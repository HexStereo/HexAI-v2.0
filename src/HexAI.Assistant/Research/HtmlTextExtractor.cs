using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace HexAI.Assistant.Research;

/// <summary>HTML sayfalarından okunabilir ana metni çıkarır (harici bağımlılık yok).</summary>
public static class HtmlTextExtractor
{
    private static readonly string[] NoiseTags =
        { "script", "style", "noscript", "svg", "iframe", "nav", "header", "footer", "aside", "form", "template", "button", "select" };

    /// <summary>Tüm etiketleri siler, entity'leri çözer, boşlukları sadeleştirir.</summary>
    public static string StripTags(string html)
    {
        string t = Regex.Replace(html, "<[^>]+>", " ");
        t = WebUtility.HtmlDecode(t);
        return Regex.Replace(t, "\\s+", " ").Trim();
    }

    /// <summary>Sayfanın başlığını ve ana metnini döndürür.</summary>
    public static (string Title, string Text) Extract(string html)
    {
        string title = "";
        var tm = Regex.Match(html, "<title[^>]*>(.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (tm.Success) title = StripTags(tm.Groups[1].Value);

        // Yorumlar ve gürültü etiketleri
        html = Regex.Replace(html, "<!--.*?-->", " ", RegexOptions.Singleline);
        foreach (var tag in NoiseTags)
            html = Regex.Replace(html, $"<{tag}\\b[^>]*>.*?</{tag}>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Varsa <article> veya <main> içine odaklan
        string body = html;
        foreach (var tag in new[] { "article", "main" })
        {
            var m = Regex.Match(html, $"<{tag}\\b[^>]*>(.*?)</{tag}>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (m.Success && m.Groups[1].Value.Length > 1500) { body = m.Groups[1].Value; break; }
        }

        // Blok etiketlerini satır sonuna çevir
        body = Regex.Replace(body, "</?(p|div|br|li|ul|ol|h[1-6]|tr|section|blockquote|pre|table)\\b[^>]*>", "\n", RegexOptions.IgnoreCase);
        body = Regex.Replace(body, "<[^>]+>", " ");
        body = WebUtility.HtmlDecode(body);

        var sb = new StringBuilder();
        var seen = new HashSet<string>();
        foreach (var raw in body.Split('\n'))
        {
            string line = Regex.Replace(raw, "[ \\t\\u00A0]+", " ").Trim();
            // Menü/etiket kırıntılarını at: kısa satırlar ve tekrarlar
            if (line.Length < 45) continue;
            if (!seen.Add(line)) continue;
            sb.AppendLine(line);
        }

        return (title, sb.ToString().Trim());
    }

    /// <summary>
    /// Uzun metinden, sorgu anahtar kelimeleriyle en çok örtüşen paragrafları
    /// (özgün sırayı koruyarak) bütçe dolana kadar seçer.
    /// </summary>
    public static string SelectRelevant(string text, string query, int maxChars)
    {
        if (text.Length <= maxChars) return text;

        var keywords = Regex.Matches(query.ToLowerInvariant(), "[\\p{L}\\p{N}]{4,}")
            .Select(m => m.Value).Distinct().ToList();

        var paras = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select((p, i) => (Index: i, Text: p, Score: Score(p, keywords, i)))
            .ToList();

        var chosen = new List<(int Index, string Text)>();
        int used = 0;
        foreach (var p in paras.OrderByDescending(p => p.Score))
        {
            if (used + p.Text.Length > maxChars) continue;
            chosen.Add((p.Index, p.Text));
            used += p.Text.Length + 1;
            if (used >= maxChars - 80) break;
        }

        return string.Join("\n", chosen.OrderBy(c => c.Index).Select(c => c.Text));
    }

    private static double Score(string para, List<string> keywords, int index)
    {
        string lower = para.ToLowerInvariant();
        double score = keywords.Count(k => lower.Contains(k)) * 3.0;
        score += Math.Min(para.Length, 400) / 400.0;   // dolgun paragraflar
        score += index < 3 ? 1.0 : 0.0;                 // girişler genelde özet içerir
        return score;
    }
}
