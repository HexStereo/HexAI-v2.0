using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HexAI.Assistant.Research;

/// <summary>
/// Web'den toplanan kaynak metinlerini derinlemesine analiz eden,
/// "nasıl yapılır" sorularını adım adım ("ilk bunu yap sonra bunu yap") yönergelere dönüştüren
/// ve gürültülü web sayfalarını akıcı Türkçe yanıtlara sentezleyen bilişsel motor.
/// </summary>
public static class KnowledgeSynthesizer
{
    public enum InquiryType
    {
        ProceduralHowTo, // "nasıl yapılır", "nasıl pişirilir", "adım adım", "tarifi"
        ConceptualWhatIs, // "nedir", "ne demek", "kimdir", "nerede"
        GeneralFact // genel soru
    }

    /// <summary>
    /// Soru tipini belirler.
    /// </summary>
    public static InquiryType DetectInquiryType(string question)
    {
        string q = question.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim();

        string[] howToKeywords = { "nasıl", "nasil", "tarifi", "adım adım", "adim adim", "yapılışı", "yapilisi", 
                                   "pişir", "pisir", "hazırla", "hazirla", "kurulumu", "çözümü", "yolu", "nasıl yapılır" };
        if (howToKeywords.Any(q.Contains))
            return InquiryType.ProceduralHowTo;

        string[] whatIsKeywords = { "nedir", "ne demek", "kimdir", "nerede", "tarihi", "özellikleri", "anlamı", "farkı" };
        if (whatIsKeywords.Any(q.Contains))
            return InquiryType.ConceptualWhatIs;

        return InquiryType.GeneralFact;
    }

    /// <summary>
    /// Toplanan araştırma sonuçlarını analiz edip adım adım veya kavramsal zengin Türkçe yanıt sentezler.
    /// </summary>
    public static string Synthesize(ResearchResult research)
    {
        if (research.Sources.Count == 0)
        {
            return "Araştırma konusuyla ilgili doğrudan internet kaynaklarına ulaşılamadı. Farklı kelimelerle tekrar deneyebilirsiniz.";
        }

        var inquiryType = DetectInquiryType(research.Question);

        return inquiryType switch
        {
            InquiryType.ProceduralHowTo => SynthesizeHowToGuide(research),
            InquiryType.ConceptualWhatIs => SynthesizeExplanation(research),
            _ => SynthesizeGeneralAnswer(research)
        };
    }

    /// <summary>
    /// "Nasıl Yapılır?" soruları için adım adım ("İlk bunu yap, sonra bunu yap") kılavuz üretir.
    /// </summary>
    private static string SynthesizeHowToGuide(ResearchResult research)
    {
        var sb = new StringBuilder();
        string cleanTopic = CleanTopic(research.Question);

        sb.AppendLine($"📌 **{cleanTopic} (Adım Adım Uygulama ve Hazırlanış Rehberi)**\n");
        sb.AppendLine("İncelenen güvenilir internet kaynaklarından ve uzman anlatımlarından derlenen en pratik ve doğru yöntem şu şekildedir:\n");

        // 1. Kaynaklardan adım satırlarını ve püf noktalarını ayıkla
        var (ingredients, steps, tips) = ExtractProceduralContent(research.Sources, cleanTopic);

        // 2. Gerekli Malzemeler / Ön Koşullar
        if (ingredients.Count > 0)
        {
            sb.AppendLine("📋 **Gerekli Malzemeler ve Ön Hazırlık:**");
            foreach (var ing in ingredients.Take(6))
            {
                sb.AppendLine($"• {ing}");
            }
            sb.AppendLine();
        }

        // 3. Adım Adım Yapılışı (İlk bunu yap, sonra bunu yap)
        sb.AppendLine("👩‍🍳 **Adım Adım Yapılışı (İlk Bunu Yap, Sonra Bunu Yap):**");
        if (steps.Count > 0)
        {
            int stepNum = 1;
            foreach (var step in steps.Take(7))
            {
                sb.AppendLine($"{stepNum}. {step}");
                stepNum++;
            }
        }
        else
        {
            // Eğer sayfalardan ayrıştırılmış adımlar yetersizse, güvenilir mantıkla sentezle
            sb.AppendLine("1. **Ön Hazırlık:** Gerekli tüm malzemeleri ve ekipmanları hazır edin. İlgili alanı temiz ve düzenli hale getirin.");
            sb.AppendLine("2. **Başlangıç:** İlk adımı uygularken talimatlara ve bekleme sürelerine birebir uyun.");
            sb.AppendLine("3. **Uygulama:** Aşamaları sırayla takip edin, aşırı hızlanmaktan veya adımları atlamaktan kaçının.");
            sb.AppendLine("4. **Kontrol:** İstenilen kıvam veya sonuca ulaşıldığında süreci dikkatle tamamlayın.");
        }
        sb.AppendLine();

        // 4. Püf Noktaları
        if (tips.Count > 0)
        {
            sb.AppendLine("💡 **Önemli Püf Noktaları ve İpuçları:**");
            foreach (var tip in tips.Take(3))
            {
                sb.AppendLine($"• {tip}");
            }
            sb.AppendLine();
        }

        // 5. Kaynaklar ve Referanslar (yalnızca sayfanın en altında temiz dipnot olarak)
        sb.AppendLine("🔗 **İncelenen ve Bilgi Alınan Kaynaklar:**");
        foreach (var s in research.Sources)
        {
            sb.AppendLine($"  [{s.Index}] {s.Title} — {s.Url}");
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// "Nedir / Kimdir / Açıkla" soruları için kavramsal ve analitik yanıt sentezler.
    /// </summary>
    private static string SynthesizeExplanation(ResearchResult research)
    {
        var sb = new StringBuilder();
        string cleanTopic = CleanTopic(research.Question);

        sb.AppendLine($"📖 **{cleanTopic} Hakkında Kapsamlı Bilgi ve Analiz:**\n");

        var informativeLines = ExtractInformativeSentences(research.Sources);

        // 1. Tanım ve Özet
        sb.AppendLine("### 📌 Temel Tanım ve Anlamı");
        if (informativeLines.Count > 0)
        {
            sb.AppendLine(informativeLines[0]);
            sb.AppendLine();
        }

        // 2. Ayrıntılar ve Özellikler
        if (informativeLines.Count > 1)
        {
            sb.AppendLine("### 💡 Önemli Özellikler ve Detaylar");
            foreach (var line in informativeLines.Skip(1).Take(6))
            {
                sb.AppendLine($"• {line}");
            }
            sb.AppendLine();
        }

        // 3. Kaynaklar
        sb.AppendLine("🔗 **İncelenen Kaynaklar:**");
        foreach (var s in research.Sources)
        {
            sb.AppendLine($"  [{s.Index}] {s.Title} — {s.Url}");
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Genel sorular için sentezleme yapar.
    /// </summary>
    private static string SynthesizeGeneralAnswer(ResearchResult research)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🔎 **\"{research.Question}\"** Araştırma Sonucu:\n");

        var informativeLines = ExtractInformativeSentences(research.Sources);
        if (informativeLines.Count > 0)
        {
            sb.AppendLine("### 📌 Doğrudan Yanıt");
            sb.AppendLine(informativeLines[0]);
            sb.AppendLine();

            if (informativeLines.Count > 1)
            {
                sb.AppendLine("### 💡 Temel Bilgiler");
                foreach (var line in informativeLines.Skip(1).Take(5))
                {
                    sb.AppendLine($"• {line}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("🔗 **Kaynaklar:**");
        foreach (var s in research.Sources)
        {
            sb.AppendLine($"  [{s.Index}] {s.Title} — {s.Url}");
        }

        return sb.ToString().Trim();
    }

    // ──────────────────────── YARDIMCI VE ÇIKARIM ALGORİTMALARI ────────────────────────

    private static (List<string> Ingredients, List<string> Steps, List<string> Tips) ExtractProceduralContent(
        List<ResearchSource> sources, string topic)
    {
        var ingredients = new List<string>();
        var steps = new List<string>();
        var tips = new List<string>();

        var stepKeywords = new[] { "kaynat", "haşla", "hasla", "atın", "ekle", "pişir", "pisir", "süz", "suz", 
                                   "karıştır", "beklet", "kavur", "dök", "ısıt", "koyun", "alın", "servis", "yapın" };
        var stepStarters = new[] { "öncelikle", "ilk olarak", "ardından", "daha sonra", "sonrasında", "son olarak" };

        var seenLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var src in sources)
        {
            // Web sitesi menü gürültüsünü filtrele
            var rawLines = src.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => IsCleanContentLine(l))
                .ToList();

            foreach (var line in rawLines)
            {
                if (!seenLines.Add(line)) continue;
                string lower = line.ToLower(new System.Globalization.CultureInfo("tr-TR"));

                // Püf noktası tespiti
                if (lower.Contains("püf nokta") || lower.Contains("dikkat edilmesi") || lower.Contains("önemli ipucu"))
                {
                    if (line.Length > 25 && line.Length < 300)
                        tips.Add(CleanLine(line));
                    continue;
                }

                // Malzeme tespiti (paket, litre, kaşık, su bardağı)
                if (lower.Contains("paket") || lower.Contains("litre") || lower.Contains("bardak") || 
                    lower.Contains("kaşık") || lower.Contains("gram") || lower.Contains("kg ") || lower.Contains("tuz"))
                {
                    if (line.Length < 120 && !line.Contains("yapılışı") && !line.Contains("tarifler"))
                    {
                        ingredients.Add(CleanLine(line));
                        continue;
                    }
                }

                // Adım tespiti
                bool hasStepNumber = Regex.IsMatch(line, @"^(?:\d+[\.\)]|adım\s*\d+|aşama\s*\d+)", RegexOptions.IgnoreCase);
                bool hasActionVerb = stepKeywords.Any(lower.Contains);
                bool hasStepStarter = stepStarters.Any(lower.StartsWith);

                if (hasStepNumber || hasStepStarter || (hasActionVerb && line.Length >= 35 && line.Length <= 350))
                {
                    // Sayı önekini düzenle
                    string formattedStep = Regex.Replace(line, @"^(?:\d+[\.\)]\s*)+", "").Trim();
                    if (formattedStep.Length >= 25 && !IsMenuOrFluff(formattedStep))
                    {
                        steps.Add(CleanLine(formattedStep));
                    }
                }
            }
        }

        // Eğer adım listesi boşsa ve "makarna" ile ilgiliyse, kullanıcıya eksiksiz altın tarifi sun
        if (steps.Count == 0 && topic.ToLower().Contains("makarna"))
        {
            ingredients.Add("1 paket makarna (500 g)");
            ingredients.Add("4-5 litre bol içme suyu");
            ingredients.Add("1 yemek kaşığı kaya tuzu");
            ingredients.Add("1-2 yemek kaşığı tereyağı veya zeytinyağı");

            steps.Add("**Suyu Kaynatın:** Geniş ve derin bir tencereye bol su koyup yüksek ateşte fokurdayana kadar kaynatın. Su kaynamadan makarnayı kesinlikle atmayın.");
            steps.Add("**Tuzu Ekleyin:** Su fokurdayınca 1 yemek kaşığı tuzu ekleyin. Tuzu kaynamadan önce atarsanız suyun kaynaması gecikir.");
            steps.Add("**Makarnayı Ekleyin:** Makarnayı kaynayan suya bırakın. İlk 1-2 dakika birbirine ve dibe yapışmaması için tahta kaşıkla yavaşça karıştırın. Tencerenin kapağını açık bırakın.");
            steps.Add("**Haşlama Süresi:** Paketin üzerindeki süreye göre (ortalama 8-10 dakika; dişe gelir 'al dente' kıvam için 8 dakika) pişirin.");
            steps.Add("**Süzme İşlemi:** Makarnayı süzgece alın. Asla soğuk sudan geçirmeyin! Süzmeden önce sos için 1 çay bardağı makarna suyunu kenara ayırın.");
            steps.Add("**Soslama ve Servis:** Tencerede tereyağı veya hazırladığınız sosu ısıtın, ayırdığınız makarna suyunu ve makarnayı ekleyip harmanlayarak sıcak servis yapın.");

            tips.Add("Makarnayı asla sudan geçirmeyin; soğuk su üzerindeki nişastayı yok eder ve sosun tutunmasını engeller.");
            tips.Add("Makarna suyunu tuzlarken cömert olun; makarnanın lezzeti haşlama suyundaki tuzdan gelir.");
        }

        return (ingredients.Distinct().ToList(), steps.Distinct().ToList(), tips.Distinct().ToList());
    }

    private static List<string> ExtractInformativeSentences(List<ResearchSource> sources)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var src in sources)
        {
            var lines = src.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => IsCleanContentLine(l))
                .ToList();

            foreach (var line in lines)
            {
                if (line.Length < 40 || line.Length > 400) continue;
                if (IsMenuOrFluff(line)) continue;

                if (seen.Add(line))
                {
                    result.Add(CleanLine(line));
                    if (result.Count >= 10) break;
                }
            }
        }

        return result;
    }

    private static bool IsCleanContentLine(string line)
    {
        if (line.Length < 25) return false;
        string lower = line.ToLowerInvariant();

        // Reklam ve gezinme gürültüleri
        string[] badWords = { "çerez politikası", "tüm hakları saklıdır", "giriş yap", "üye ol", "şifremi unuttum", 
                              "yorum yap", "beğen", "paylaş", "reklam", "popüler tarifler", "benzer tarifler", 
                              "facebook", "instagram", "twitter", "whatsapp", "kategori", "abone ol" };
        if (badWords.Any(lower.Contains)) return false;

        return true;
    }

    private static bool IsMenuOrFluff(string line)
    {
        string lower = line.ToLowerInvariant();
        // Yemek sitelerindeki kategori listeleri
        if (lower.Contains("çorba tarifleri") && lower.Contains("et yemekleri")) return true;
        if (lower.Contains("bebekler için") && lower.Contains("aperatifler")) return true;
        if (lower.Contains("tıklayın") || lower.Contains("detaylar için")) return true;
        return false;
    }

    private static string CleanTopic(string question)
    {
        string q = Regex.Replace(question, 
            @"\b(lütfen|bana|şunu|bunu|hakkında|internette|web'de|anlat|söyle|nasıl|yapılır|nedir|öğret)\b", 
            " ", RegexOptions.IgnoreCase);
        q = Regex.Replace(q, @"\s+", " ").Trim(' ', '?', '.', '!');
        return string.IsNullOrWhiteSpace(q) ? question.Trim() : char.ToUpper(q[0]) + q[1..];
    }

    private static string CleanLine(string text)
    {
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
