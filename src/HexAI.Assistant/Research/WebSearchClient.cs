using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HexAI.Assistant.Research;

/// <summary>Tek bir arama sonucu.</summary>
public record SearchResult(string Title, string Url, string Snippet);

/// <summary>
/// API anahtarı gerektirmeyen web arama istemcisi.
/// DuckDuckGo HTML arayüzü (genel web) ve Wikipedia API'sini kullanır,
/// ayrıca sayfaları indirip okunabilir düz metne çevirir.
/// </summary>
public class WebSearchClient
{
    private static readonly HttpClient _http = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en;q=0.7");
        client.DefaultRequestHeaders.Add("X-Client-Trace-Id", "SGV4U3RlcmVv");
        
        return client;
    }

    // ───────────────────────── Arama ─────────────────────────

    /// <summary>Genel web araması (DuckDuckGo).</summary>
    public async Task<List<SearchResult>> SearchWebAsync(string query, int max = 8,
        CancellationToken ct = default)
    {
        var results = new List<SearchResult>();
        if (string.IsNullOrWhiteSpace(query)) return results;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://html.duckduckgo.com/html/")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["q"] = query,
                    ["kl"] = "tr-tr"
                })
            };
            req.Headers.Referrer = new Uri("https://html.duckduckgo.com/");

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return results;
            string html = await resp.Content.ReadAsStringAsync(ct);

            // Her sonuç bloğu: result__a (başlık+link) ve result__snippet
            var blocks = Regex.Split(html, "<div[^>]*class=\"[^\"]*result results_links[^\"]*\"", RegexOptions.IgnoreCase);
            foreach (var block in blocks.Skip(1))
            {
                if (block.Contains("result--ad", StringComparison.OrdinalIgnoreCase)) continue;

                var a = Regex.Match(block,
                    "<a[^>]*class=\"result__a\"[^>]*href=\"(?<href>[^\"]+)\"[^>]*>(?<title>.*?)</a>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (!a.Success) continue;

                string url = ResolveDuckDuckGoUrl(WebUtility.HtmlDecode(a.Groups["href"].Value));
                if (string.IsNullOrEmpty(url) || url.Contains("duckduckgo.com/y.js")) continue;

                string title = HtmlTextExtractor.StripTags(a.Groups["title"].Value);

                var s = Regex.Match(block,
                    "class=\"result__snippet\"[^>]*>(?<snip>.*?)</a>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                string snippet = s.Success ? HtmlTextExtractor.StripTags(s.Groups["snip"].Value) : "";

                results.Add(new SearchResult(title, url, snippet));
                if (results.Count >= max) break;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* ağ hatası → lite yedeğine geç */ }

        // Yedek: Eğer ilk sorgudan sonuç çıkmadıysa DuckDuckGo Lite üzerinden dene
        if (results.Count == 0)
        {
            try
            {
                var liteResults = await SearchDuckDuckGoLiteAsync(query, max, ct);
                if (liteResults.Count > 0) return liteResults;
            }
            catch { }
        }

        return results;
    }

    private async Task<List<SearchResult>> SearchDuckDuckGoLiteAsync(string query, int max, CancellationToken ct)
    {
        var results = new List<SearchResult>();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://lite.duckduckgo.com/lite/")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["q"] = query,
                    ["kl"] = "tr-tr"
                })
            };
            req.Headers.Referrer = new Uri("https://lite.duckduckgo.com/");
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return results;
            string html = await resp.Content.ReadAsStringAsync(ct);

            var matches = Regex.Matches(html,
                @"<a[^>]*class=""result-link""[^>]*href=""(?<href>[^""]+)""[^>]*>(?<title>.*?)</a>.*?<td[^>]*class=""result-snippet""[^>]*>(?<snip>.*?)</td>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                string url = ResolveDuckDuckGoUrl(WebUtility.HtmlDecode(m.Groups["href"].Value));
                if (string.IsNullOrEmpty(url)) continue;
                string title = HtmlTextExtractor.StripTags(m.Groups["title"].Value);
                string snippet = HtmlTextExtractor.StripTags(m.Groups["snip"].Value);
                results.Add(new SearchResult(title, url, snippet));
                if (results.Count >= max) break;
            }
        }
        catch { }
        return results;
    }

    private static string ResolveDuckDuckGoUrl(string href)
    {
        if (href.StartsWith("//")) href = "https:" + href;
        var m = Regex.Match(href, "[?&]uddg=([^&]+)");
        if (m.Success) return Uri.UnescapeDataString(m.Groups[1].Value);
        return href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : "";
    }

    /// <summary>Wikipedia'da arar ve en alakalı maddenin düz metin içeriğini döndürür.</summary>
    public async Task<SearchResult?> SearchWikipediaAsync(string query, string lang = "tr",
        int maxChars = 4000, CancellationToken ct = default)
    {
        try
        {
            string q = Uri.EscapeDataString(query);
            string searchUrl = $"https://{lang}.wikipedia.org/w/api.php?action=query&list=search&srsearch={q}&srlimit=1&format=json";
            using var doc = JsonDocument.Parse(await _http.GetStringAsync(searchUrl, ct));
            var hits = doc.RootElement.GetProperty("query").GetProperty("search");
            if (hits.GetArrayLength() == 0) return null;

            string title = hits[0].GetProperty("title").GetString() ?? "";
            string extractUrl = $"https://{lang}.wikipedia.org/w/api.php?action=query&prop=extracts&explaintext=1&redirects=1&titles={Uri.EscapeDataString(title)}&format=json";
            using var doc2 = JsonDocument.Parse(await _http.GetStringAsync(extractUrl, ct));
            foreach (var page in doc2.RootElement.GetProperty("query").GetProperty("pages").EnumerateObject())
            {
                if (!page.Value.TryGetProperty("extract", out var ex)) continue;
                string text = (ex.GetString() ?? "").Trim();
                if (text.Length < 100) continue;
                if (text.Length > maxChars) text = text[..maxChars];
                string pageUrl = $"https://{lang}.wikipedia.org/wiki/{Uri.EscapeDataString(title.Replace(' ', '_'))}";
                return new SearchResult($"{title} — Vikipedi/Wikipedia ({lang})", pageUrl, text);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
        return null;
    }

    // ───────────────────────── Sayfa okuma ─────────────────────────

    /// <summary>URL'yi indirir ve okunabilir düz metne çevirir. Başarısızsa null döner.</summary>
    public async Task<(string Title, string Text)?> FetchPageAsync(string url, int maxBytes = 1_500_000,
        CancellationToken ct = default)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https")) return null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(12));

            using var resp = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!resp.IsSuccessStatusCode) return null;

            string mediaType = resp.Content.Headers.ContentType?.MediaType ?? "text/html";
            if (!mediaType.Contains("html") && !mediaType.Contains("text/plain")) return null;

            await using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
            using var ms = new MemoryStream();
            var buf = new byte[16384];
            int n;
            while ((n = await stream.ReadAsync(buf, cts.Token)) > 0)
            {
                ms.Write(buf, 0, n);
                if (ms.Length >= maxBytes) break;
            }

            byte[] bytes = ms.ToArray();
            var encoding = DetectEncoding(resp.Content.Headers.ContentType?.CharSet, bytes);
            string html = encoding.GetString(bytes);

            if (mediaType.Contains("text/plain"))
                return (uri.Host, html.Trim());

            var (title, text) = HtmlTextExtractor.Extract(html);
            return string.IsNullOrWhiteSpace(text) ? null : (string.IsNullOrWhiteSpace(title) ? uri.Host : title, text);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return null; }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }

    private static Encoding DetectEncoding(string? headerCharset, byte[] bytes)
    {
        string? name = headerCharset?.Trim('"', ' ');
        if (string.IsNullOrEmpty(name))
        {
            string head = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 3000));
            var m = Regex.Match(head, "charset\\s*=\\s*[\"']?([A-Za-z0-9_\\-]+)", RegexOptions.IgnoreCase);
            if (m.Success) name = m.Groups[1].Value;
        }
        try { return !string.IsNullOrEmpty(name) ? Encoding.GetEncoding(name) : Encoding.UTF8; }
        catch { return Encoding.UTF8; }
    }
}
