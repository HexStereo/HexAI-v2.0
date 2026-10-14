using System.Text;
using System.Text.RegularExpressions;

namespace HexAI.Assistant.Research;

/// <summary>Araştırmada toplanan tek bir kaynak.</summary>
public class ResearchSource
{
    public int Index { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>Araştırma çıktısı: kullanılan sorgular + kaynaklar.</summary>
public class ResearchResult
{
    public string Question { get; set; } = "";
    public List<string> Queries { get; set; } = new();
    public List<ResearchSource> Sources { get; set; } = new();
    public bool HasSources => Sources.Count > 0;

    /// <summary>Kullanıcıya gösterilecek kaynak listesi.</summary>
    public string BuildSourceList()
    {
        var sb = new StringBuilder();
        sb.AppendLine("📚 Kaynaklar ve Referanslar:");
        foreach (var s in Sources)
            sb.AppendLine($"  [{s.Index}] {s.Title} — {s.Url}");
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// HexAI Yerel Web Araştırma Motoru:
/// Dışarıdan hazır model gerektirmeden, HexAI'ın kendi analitik zekasıyla:
///   1. Soru analiz edilir ve anahtar kavramlar çıkarılır.
///   2. Çoklu sorgu planı oluşturulur (Türkçe ve uluslararası kavramlar).
///   3. DuckDuckGo + Wikipedia taranır; aday sayfalar okunur.
///   4. Alakalı paragraflar derlenir ve doğrudan özetlenip sentezlenir.
/// </summary>
public class ResearchEngine
{
    private readonly WebSearchClient _web = new();

    public int MaxSources { get; set; } = 5;
    public int CharsPerSource { get; set; } = 3500;
    public int MaxQueries { get; set; } = 3;

    public ResearchEngine()
    {
    }

    public WebSearchClient Web => _web;

    public async Task<ResearchResult> GatherAsync(string question,
        Action<string>? progress = null, CancellationToken ct = default)
    {
        var result = new ResearchResult { Question = question };

        // 1) Sorgu planı (HexAI doğal dil ayrıştırıcı)
        progress?.Invoke("🧭 Arama sorguları planlanıyor...");
        result.Queries = PlanQueries(question);
        foreach (var q in result.Queries) progress?.Invoke($"   🔎 {q}");

        // 2) Aramalar (paralel) + Vikipedi
        var searchTasks = result.Queries.Select(q => _web.SearchWebAsync(q, 8, ct)).ToList();
        var wikiTask = _web.SearchWikipediaAsync(result.Queries[0], "tr", CharsPerSource, ct);
        var wikiEnTask = result.Queries.Count > 1
            ? _web.SearchWikipediaAsync(result.Queries[^1], "en", CharsPerSource, ct)
            : Task.FromResult<SearchResult?>(null);

        var perQuery = await Task.WhenAll(searchTasks);
        var wiki = await wikiTask;
        var wikiEn = await wikiEnTask;

        // Sonuçları sorgular arası dönüşümlü sırala, aynı siteyi tekrarlama
        var candidates = new List<SearchResult>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hostCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int rank = 0; rank < 8; rank++)
        {
            foreach (var list in perQuery)
            {
                if (rank >= list.Count) continue;
                var r = list[rank];
                if (!Uri.TryCreate(r.Url, UriKind.Absolute, out var uri)) continue;
                if (!seenUrls.Add(NormalizeUrl(uri))) continue;
                hostCount[uri.Host] = hostCount.GetValueOrDefault(uri.Host) + 1;
                if (hostCount[uri.Host] > 2) continue;
                candidates.Add(r);
            }
        }

        progress?.Invoke($"🌐 {candidates.Count} kaynak bulundu, içerikler inceleniyor...");

        // 3) Sayfaları paralel oku
        var gate = new SemaphoreSlim(4);
        var fetchTargets = candidates.Take(MaxSources + 4).ToList();
        var fetchTasks = fetchTargets.Select(async c =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var page = await _web.FetchPageAsync(c.Url, ct: ct);
                return (Candidate: c, Page: page);
            }
            finally { gate.Release(); }
        }).ToList();
        var fetched = await Task.WhenAll(fetchTasks);

        int index = 1;
        void Add(string title, string url, string text)
        {
            result.Sources.Add(new ResearchSource { Index = index++, Title = title, Url = url, Text = text });
            progress?.Invoke($"   📄 [{index - 1}] {Trunc(title, 70)}");
        }

        // Vikipedi güvenilir omurga
        if (wiki != null) Add(wiki.Title, wiki.Url,
            HtmlTextExtractor.SelectRelevant(wiki.Snippet, question, CharsPerSource));

        foreach (var (cand, page) in fetched)
        {
            if (result.Sources.Count >= MaxSources) break;

            if (page is { } p && p.Text.Length >= 300)
            {
                string title = !string.IsNullOrWhiteSpace(cand.Title) ? cand.Title : p.Title;
                Add(title, cand.Url, HtmlTextExtractor.SelectRelevant(p.Text, question, CharsPerSource));
            }
            else if (cand.Snippet.Length >= 80)
            {
                Add(cand.Title, cand.Url, cand.Snippet);
            }
        }

        // İngilizce Vikipedi desteği
        if (wikiEn != null && result.Sources.Count < MaxSources + 1 &&
            !result.Sources.Any(s => s.Url == wikiEn.Url))
        {
            Add(wikiEn.Title, wikiEn.Url, HtmlTextExtractor.SelectRelevant(wikiEn.Snippet, question, CharsPerSource));
        }

        return result;
    }

    /// <summary>
    /// Toplanan kaynakları analiz ederek kapsamlı, yapılandırılmış bir araştırma raporu sentezler.
    /// <summary>
    /// Toplanan kaynakları analiz ederek kapsamlı, yapılandırılmış ve akıcı bir Türkçe yanıt sentezler.
    /// Tamamen kendi iç algoritmalarımızla (saf yerel kod) üretilir.
    /// </summary>
    /// <summary>
    /// Toplanan kaynakları analiz ederek kapsamlı, yapılandırılmış, adım adım ve akıcı bir Türkçe yanıt sentezler.
    /// "Nasıl yapılır" sorularını adım adım ("ilk bunu yap sonra bunu yap") formatına çevirir.
    /// </summary>
    public string SynthesizeReport(ResearchResult research)
    {
        return KnowledgeSynthesizer.Synthesize(research);
    }

    // ───────────────────────── Kendi Doğal Dil Sorgu Planlama ─────────────────────────

    private List<string> PlanQueries(string question)
    {
        string cleaned = CleanQuestion(question);
        var queries = new List<string> { cleaned };

        // Ek alternatif sorgular üret
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 2)
        {
            // İlk 3-4 kelimelik çekirdek kavram
            string core = string.Join(" ", words.Take(4));
            if (!queries.Contains(core, StringComparer.OrdinalIgnoreCase))
                queries.Add(core);
        }

        if (words.Length >= 1)
        {
            if (question.ToLower().Contains("nasıl") || question.ToLower().Contains("tarif"))
            {
                string recipeQuery = $"{cleaned} tarifi adımları yapılışı";
                if (queries.Count < MaxQueries) queries.Add(recipeQuery);
            }
            else
            {
                string detailQuery = $"{words[0]} güncel detaylar";
                if (queries.Count < MaxQueries) queries.Add(detailQuery);
            }
        }

        return queries.Take(MaxQueries).ToList();
    }

    public static string CleanQuestion(string q)
    {
        string c = Regex.Replace(q,
            "\\b(lütfen|bana|şunu|bunu|hakkında|internette|internetten|webde|web'de|google'da|araştır(?:ır mısın|ır mısınız|)|araştırma yap|ara(?:r mısın|)|bul)\\b",
            " ", RegexOptions.IgnoreCase);
        c = Regex.Replace(c, "\\s+", " ").Trim(' ', '?', '.', '!');
        return string.IsNullOrWhiteSpace(c) ? q.Trim() : c;
    }

    /// <summary>
    /// Gelen mesajın canlı web araştırması gerektirip gerektirmediğini belirler.
    /// </summary>
    public static bool ShouldResearch(string message) => ShouldResearch(message, out _);

    /// <summary>
    /// Gelen mesajın canlı web araştırması gerektirip gerektirmediğini belirler.
    /// Soru ifadeleri, nasıl yapılır rehberleri, kavram açıklamaları ve güncel bilgiler tespit edilir.
    /// "bana makarna nasıl yapılır anlat" gibi doğal istekler doğru bir şekilde araştırma/öğrenmeye yönlendirilir.
    /// </summary>
    public static bool ShouldResearch(string message, out string reason)
    {
        string m = message.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim();
        reason = "";

        // 1. Açık araştırma istekleri — kullanıcı doğrudan web araması istiyorsa kesinlikle araştır
        string[] explicitTriggers =
        {
            "araştır", "arastir", "internette ara", "internetten ara", "webde ara", "web'de ara",
            "google'da ara", "googleda ara", "kaynak bul", "kaynak göster", "kaynaklarla", "internetten öğren",
            "internetten bilgi", "internetten getir"
        };
        if (explicitTriggers.Any(m.Contains))
        {
            reason = "açık araştırma isteği";
            return true;
        }

        // 2. Harici nesnel / kavramsal bilgi ve "Nasıl Yapılır" soruları (ÖNCELİKLİ KONTROL)
        // Kullanıcı "bana makarna nasıl yapılır anlat" veya "makarna nasıl yapılır" derse araştırılmalı
        string[] questionPatterns =
        {
            "nasıl yapılır", "nasil yapilir", "nasıl pişirilir", "nasil pisirilir", "nasıl çalışır", "nasil calisir",
            "nasıl hazırlanır", "nasıl kurulur", "nasıl çözülür", "nasıl temizlenir", "adım adım", "tarifi", "rehberi",
            "nedir", "ne demek", "kimdir", "nerededir", "nerede", "ne zaman", "neden olur", "niçin", "niye",
            "hangi", "hangisidir", "ne kadar", "hakkında bilgi", "özellikleri", "tarihi", "tarihçesi",
            "farkı ne", "farkları", "kim buldu", "kim icat etti", "ne işe yarar"
        };
        if (questionPatterns.Any(m.Contains))
        {
            reason = "bilgi ve adım adım uygulama/açıklama sorusu";
            return true;
        }

        // 3. Güncel veya dinamik harici veriler
        string[] freshness =
        {
            "son dakika", "güncel", "bugün ne", "dün ne", "şu an", "şu anda", "son haber", "haberler",
            "son durum", "en son", "2025", "2026", "bu hafta", "bu yıl", "kaç tl", "dolar kaç", "euro kaç",
            "hava durumu", "maç sonucu", "kim kazandı", "ne zaman çıkacak", "çıkış tarihi", "fiyatı ne"
        };
        if (freshness.Any(m.Contains))
        {
            reason = "güncel bilgi gerektiriyor";
            return true;
        }

        // 4. Saf Sohbet ve Kişisel Diyalog Filtresi — Asistanla doğrudan selamlama, hal hatır, şaka
        // Bilgi sorusu İÇERMEYEN saf sohbet mesajları web aramasına gönderilmez.
        string[] pureConversationalMarkers =
        {
            "nasılsın", "nasilsin", "naber", "ne haber", "iyi misin", "nasılsınız", "nasilsiniz", "keyifler",
            "sohbet", "konuşalım", "muhabbet", "dertleşelim",
            "fıkra", "şaka", "fikra", "saka", "şiir", "hikaye anlat",
            "ne yapıyorsun", "ne düşünüyorsun", "amacın ne",
            "merhaba", "selam", "günaydın", "iyi günler", "iyi akşamlar", "teşekkür", "sağol", "sagol", "hoşça kal"
        };

        if (pureConversationalMarkers.Any(cm => m.Contains(cm)))
        {
            return false;
        }

        // 5. Soru işareti içeren veya "anlat", "açıkla", "öğret" içeren en az 3 kelimelik nesnel sorular
        bool hasInquiryAction = m.Contains("anlat") || m.Contains("açıkla") || m.Contains("öğret") || m.Contains("söyle");
        if ((m.EndsWith("?") || hasInquiryAction) && m.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3)
        {
            reason = "soru ve öğrenme isteği";
            return true;
        }

        return false;
    }

    private static string NormalizeUrl(Uri u) => (u.Host + u.AbsolutePath).TrimEnd('/').ToLowerInvariant();
    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
