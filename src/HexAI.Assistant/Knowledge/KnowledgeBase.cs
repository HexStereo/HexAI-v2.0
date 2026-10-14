using System.Text.Json;
using System.Text.Json.Serialization;

namespace HexAI.Assistant.Knowledge;

/// <summary>
/// Doğrulanmış bilgi girişi (soru-cevap çifti + metadata).
/// </summary>
public class KnowledgeEntry
{
    /// <summary>Kullanıcının sorduğu soru veya konu.</summary>
    [JsonPropertyName("question")]
    public string Question { get; set; } = "";

    /// <summary>Doğrulanmış cevap.</summary>
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = "";

    /// <summary>Güven skoru (0.0 - 10.0).</summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    /// <summary>Cevabın kaynağı (ör: "Claude+ChatGPT", "HexAI", "Kullanıcı").</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    /// <summary>Kayıt tarihi.</summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Doğrulama detayları (her LLM'den gelen puanlar).</summary>
    [JsonPropertyName("verificationDetails")]
    public Dictionary<string, double> VerificationDetails { get; set; } = new();

    /// <summary>Arama için normalleştirilmiş anahtar kelimeler.</summary>
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// Doğrulanmış soru-cevap çiftlerini kalıcı olarak saklayan ve arayan bilgi tabanı.
/// HexAI'ın "hafızası" görevi görür. Dış LLM'lerden doğrulanmış bilgiler burada birikir.
/// </summary>
public class KnowledgeBase
{
    private readonly string _storagePath;
    private List<KnowledgeEntry> _entries = new();
    private readonly object _lock = new();

    /// <summary>Toplam bilgi girişi sayısı.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Yeni bir KnowledgeBase oluşturur.
    /// </summary>
    /// <param name="storagePath">Bilgi tabanının saklanacağı dizin yolu.</param>
    public KnowledgeBase(string storagePath)
    {
        _storagePath = storagePath;
        Directory.CreateDirectory(_storagePath);
        Load();
    }

    /// <summary>
    /// Yeni bir doğrulanmış bilgi girişi ekler.
    /// </summary>
    public void Save(string question, string answer, double confidence, string source,
                     Dictionary<string, double>? verificationDetails = null)
    {
        var entry = new KnowledgeEntry
        {
            Question = question,
            Answer = answer,
            Confidence = confidence,
            Source = source,
            Timestamp = DateTime.Now,
            VerificationDetails = verificationDetails ?? new(),
            Keywords = ExtractKeywords(question)
        };

        lock (_lock)
        {
            // Aynı soru varsa güncelle, yoksa ekle
            var existing = FindExact(question);
            if (existing != null)
            {
                // Daha yüksek güven skorlu cevabı tut
                if (confidence > existing.Confidence)
                {
                    _entries.Remove(existing);
                    _entries.Add(entry);
                }
            }
            else
            {
                _entries.Add(entry);
            }

            Persist();
        }
    }

    /// <summary>
    /// Verilen soruya en benzer bilgi girişini arar.
    /// Basit anahtar kelime benzerlik eşleştirmesi kullanır.
    /// </summary>
    public KnowledgeEntry? Search(string question, double minConfidence = 5.0)
    {
        if (_entries.Count == 0) return null;

        var queryKeywords = ExtractKeywords(question);
        if (queryKeywords.Count == 0) return null;

        KnowledgeEntry? bestMatch = null;
        double bestScore = 0;

        foreach (var entry in _entries)
        {
            if (entry.Confidence < minConfidence) continue;

            // Jaccard benzerliği hesapla
            var intersection = queryKeywords.Intersect(entry.Keywords).Count();
            var union = queryKeywords.Union(entry.Keywords).Count();

            if (union == 0) continue;

            double similarity = (double)intersection / union;

            if (similarity > bestScore && similarity >= 0.4) // En az %40 benzerlik
            {
                bestScore = similarity;
                bestMatch = entry;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Tüm bilgi girişlerini corpus formatında dışa aktarır.
    /// LargeGPT eğitimi için kullanılabilir.
    /// </summary>
    public string ExportAsCorpus()
    {
        var lines = _entries
            .Where(e => e.Confidence >= 7.0)
            .OrderByDescending(e => e.Confidence)
            .Select(e => $"{e.Question} {e.Answer}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Corpus'u dosyaya yazar.
    /// </summary>
    public void ExportAsCorpusToFile(string outputPath)
    {
        var corpus = ExportAsCorpus();
        if (!string.IsNullOrWhiteSpace(corpus))
        {
            File.WriteAllText(outputPath, corpus);
        }
    }

    /// <summary>
    /// Tüm bilgi girişlerini döndürür (salt okunur).
    /// </summary>
    public IReadOnlyList<KnowledgeEntry> GetAllEntries() => _entries.AsReadOnly();

    /// <summary>
    /// İstatistikleri döndürür.
    /// </summary>
    public string GetStats()
    {
        if (_entries.Count == 0) return "📚 Bilgi tabanı henüz boş.";

        double avgConfidence = _entries.Average(e => e.Confidence);
        var sources = _entries.GroupBy(e => e.Source).Select(g => $"{g.Key}: {g.Count()}");

        return $"📚 Bilgi Tabanı İstatistikleri:\n" +
               $"  Toplam Giriş  : {_entries.Count}\n" +
               $"  Ort. Güven    : {avgConfidence:F1}/10\n" +
               $"  Kaynaklar     : {string.Join(", ", sources)}\n" +
               $"  Son Güncelleme: {_entries.Max(e => e.Timestamp):dd/MM/yyyy HH:mm}";
    }

    #region Private Helpers

    private KnowledgeEntry? FindExact(string question)
    {
        string normalized = question.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim();
        return _entries.FirstOrDefault(e =>
            e.Question.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim() == normalized);
    }

    private static List<string> ExtractKeywords(string text)
    {
        // Basit anahtar kelime çıkarma: küçük harf, noktalama temizle, 2+ karakter
        var rawWords = text.ToLower(new System.Globalization.CultureInfo("tr-TR"))
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ':', ';', '"', '\'', '(', ')', '/', '-', '_' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2)
            .Distinct()
            .ToList();

        // Yaygın Türkçe durdurma kelimelerini çıkar
        var stopWords = new HashSet<string>
        {
            "bir", "bu", "şu", "ve", "ile", "için", "de", "da", "mi", "mı",
            "ne", "mu", "mü", "ama", "fakat", "ya", "veya", "hem", "ki",
            "ben", "sen", "biz", "siz", "the", "is", "are", "was", "and"
        };

        var result = new HashSet<string>();
        foreach (var w in rawWords)
        {
            if (stopWords.Contains(w)) continue;
            result.Add(w);
            string stem = NormalizeTurkishStem(w);
            if (stem.Length >= 2 && !stopWords.Contains(stem))
            {
                result.Add(stem);
            }
        }

        return result.ToList();
    }

    private static string NormalizeTurkishStem(string word)
    {
        string w = word;
        string[] suffixes = { "leri", "ları", "lerin", "ların", "lerden", "lardan", "lerde", "larda",
                              "lere", "lara", "deki", "daki", "nin", "nın", "nun", "nün",
                              "den", "dan", "ten", "tan", "de", "da", "te", "ta",
                              "ye", "ya", "yi", "yı", "yu", "yü",
                              "ler", "lar", "dir", "dır", "dur", "dür", "tir", "tır", "tur", "tür",
                              "in", "ın", "un", "ün", "i", "ı", "u", "ü", "e", "a" };
        foreach (var s in suffixes)
        {
            if (w.Length > s.Length + 2 && w.EndsWith(s))
            {
                return w[..^s.Length];
            }
        }
        return w;
    }

    private void Load()
    {
        string filePath = Path.Combine(_storagePath, "knowledge.json");
        if (!File.Exists(filePath)) return;

        try
        {
            var json = File.ReadAllText(filePath);
            _entries = JsonSerializer.Deserialize<List<KnowledgeEntry>>(json) ?? new();
        }
        catch
        {
            _entries = new();
        }
    }

    private void Persist()
    {
        try
        {
            string filePath = Path.Combine(_storagePath, "knowledge.json");
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch { /* Kaydetme hatası sessizce geçilir */ }
    }

    #endregion
}

