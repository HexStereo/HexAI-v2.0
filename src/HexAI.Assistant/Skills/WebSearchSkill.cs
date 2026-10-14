using System.Text;
using HexAI.Assistant.Research;

namespace HexAI.Assistant.Skills;

/// <summary>
/// İnternet arama yeteneği.
/// DuckDuckGo (genel web) + Vikipedi ile arama yapar; başlık, özet ve bağlantıları döndürür.
/// Ayrıntı için <see cref="WebReadSkill"/> ile sayfa okunabilir.
/// </summary>
public class WebSearchSkill : ISkill
{
    private static readonly WebSearchClient _client = new();

    public string Name => "Web Arama";
    public string Description => "İnternette arama yapar: başlık, özet ve bağlantı listesi döndürür. Parametre: query";
    public string[] TriggerKeywords => new[]
    {
        "ara", "internet", "web", "google", "arama", "search", "güncel", "haber", "wiki"
    };

    public async Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string query = parameters.GetValueOrDefault("query") ??
                       parameters.GetValueOrDefault("q") ?? userInput;
        query = ResearchEngine.CleanQuestion(query);

        if (string.IsNullOrWhiteSpace(query))
            return SkillResult.Fail("Arama sorgusu belirtilmedi.");

        try
        {
            var web = await _client.SearchWebAsync(query, 6);
            var wiki = await _client.SearchWikipediaAsync(query, "tr", 600);

            if (web.Count == 0 && wiki == null)
                return SkillResult.Ok($"🔍 '{query}' için sonuç bulunamadı (internet bağlantısını kontrol edin).");

            var sb = new StringBuilder();
            sb.AppendLine($"🔍 Arama: \"{query}\"");
            sb.AppendLine();

            if (wiki != null)
            {
                sb.AppendLine($"📖 {wiki.Title}");
                sb.AppendLine($"   {wiki.Snippet.Split('\n')[0]}");
                sb.AppendLine($"   🔗 {wiki.Url}");
                sb.AppendLine();
            }

            int i = 1;
            foreach (var r in web)
            {
                sb.AppendLine($"{i++}. {r.Title}");
                if (!string.IsNullOrWhiteSpace(r.Snippet)) sb.AppendLine($"   {r.Snippet}");
                sb.AppendLine($"   🔗 {r.Url}");
            }

            string text = sb.ToString().Trim();
            return SkillResult.Ok(text, text);
        }
        catch (Exception ex)
        {
            return SkillResult.Fail($"Arama hatası: {ex.Message}");
        }
    }
}

/// <summary>Verilen web sayfasını indirip okunabilir metnini döndürür.</summary>
public class WebReadSkill : ISkill
{
    private static readonly WebSearchClient _client = new();

    public string Name => "Web Sayfası Oku";
    public string Description => "Bir web sayfasının (URL) metin içeriğini okur. Parametre: url";
    public string[] TriggerKeywords => new[] { "oku", "sayfa", "url", "link", "http" };

    public async Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string url = (parameters.GetValueOrDefault("url") ?? userInput).Trim();
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Fail("Geçerli bir http(s) URL'si gerekli.");

        var page = await _client.FetchPageAsync(url);
        if (page is not { } p)
            return SkillResult.Fail("Sayfa okunamadı (erişim engeli, zaman aşımı veya desteklenmeyen içerik).");

        string text = p.Text.Length > 6000 ? p.Text[..6000] + "\n…(kısaltıldı)" : p.Text;
        return SkillResult.Ok($"📄 {p.Title}\n🔗 {url}\n\n{text}", text);
    }
}
