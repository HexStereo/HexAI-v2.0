namespace HexAI.Assistant.Knowledge;

/// <summary>
/// Çeşitli dosya formatlarından eğitim corpus'u yükleyen sınıf.
/// .txt, .md, .json dosyalarını destekler.
/// </summary>
public class CorpusLoader
{
    /// <summary>
    /// Tek bir dosyadan metin yükler.
    /// </summary>
    public static string LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");

        string ext = Path.GetExtension(filePath).ToLower();
        string content = File.ReadAllText(filePath);

        return ext switch
        {
            ".json" => ExtractTextFromJson(content),
            ".md" => CleanMarkdown(content),
            _ => content // .txt ve diğer formatlar doğrudan okunur
        };
    }

    /// <summary>
    /// Bir klasördeki tüm metin dosyalarından corpus oluşturur.
    /// </summary>
    public static string LoadFromDirectory(string directoryPath, string searchPattern = "*.txt")
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Klasör bulunamadı: {directoryPath}");

        var allText = new List<string>();
        string[] patterns = { "*.txt", "*.md" };

        foreach (var pattern in patterns)
        {
            var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    string content = LoadFromFile(file);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        allText.Add(content);
                    }
                }
                catch { /* Okunamayan dosyalar atlanır */ }
            }
        }

        if (allText.Count == 0)
            throw new InvalidOperationException("Klasörde okunabilir metin dosyası bulunamadı.");

        return string.Join("\n", allText);
    }

    /// <summary>
    /// Yüklenen corpus hakkında bilgi döndürür.
    /// </summary>
    public static CorpusInfo GetInfo(string corpus)
    {
        var words = corpus.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var uniqueWords = new HashSet<string>(words.Select(w => w.ToLower(new System.Globalization.CultureInfo("tr-TR"))));
        var sentences = corpus.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        return new CorpusInfo
        {
            TotalCharacters = corpus.Length,
            TotalWords = words.Length,
            UniqueWords = uniqueWords.Count,
            TotalSentences = sentences.Length
        };
    }

    /// <summary>
    /// JSON dosyasından metin çıkarır.
    /// Desteklenen formatlar:
    /// - String dizisi: ["metin1", "metin2"]
    /// - Obje dizisi: [{"text": "metin"}]
    /// - Tek string: "metin"
    /// </summary>
    private static string ExtractTextFromJson(string json)
    {
        json = json.Trim();

        // Basit string dizisi
        if (json.StartsWith("["))
        {
            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                if (items != null)
                    return string.Join("\n", items);
            }
            catch { /* String dizisi değilse diğer formatları dene */ }

            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
                if (items != null)
                {
                    var texts = new List<string>();
                    foreach (var item in items)
                    {
                        // "text", "content", "metin" anahtarlarını ara
                        foreach (var key in new[] { "text", "content", "metin", "sentence", "cümle" })
                        {
                            if (item.ContainsKey(key))
                            {
                                texts.Add(item[key]);
                                break;
                            }
                        }
                    }
                    if (texts.Count > 0)
                        return string.Join("\n", texts);
                }
            }
            catch { }
        }

        // Tek string
        if (json.StartsWith("\""))
        {
            return json.Trim('"');
        }

        return json;
    }

    /// <summary>
    /// Markdown formatındaki metni temizler.
    /// </summary>
    private static string CleanMarkdown(string md)
    {
        // Başlık işaretlerini kaldır
        md = System.Text.RegularExpressions.Regex.Replace(md, @"#{1,6}\s*", "");
        // Bold/italic işaretlerini kaldır
        md = md.Replace("**", "").Replace("__", "").Replace("*", "").Replace("_", "");
        // Kod bloklarını kaldır
        md = System.Text.RegularExpressions.Regex.Replace(md, @"```[\s\S]*?```", "");
        md = System.Text.RegularExpressions.Regex.Replace(md, @"`[^`]*`", "");
        // Link formatını temizle
        md = System.Text.RegularExpressions.Regex.Replace(md, @"\[([^\]]*)\]\([^\)]*\)", "$1");
        // HTML etiketlerini kaldır
        md = System.Text.RegularExpressions.Regex.Replace(md, @"<[^>]*>", "");

        return md.Trim();
    }
}

/// <summary>
/// Corpus hakkında istatistik bilgisi.
/// </summary>
public class CorpusInfo
{
    public int TotalCharacters { get; set; }
    public int TotalWords { get; set; }
    public int UniqueWords { get; set; }
    public int TotalSentences { get; set; }

    public override string ToString() =>
        $"📊 Corpus Bilgisi:\n" +
        $"  Karakter  : {TotalCharacters:N0}\n" +
        $"  Kelime    : {TotalWords:N0}\n" +
        $"  Benzersiz : {UniqueWords:N0}\n" +
        $"  Cümle     : {TotalSentences:N0}";
}

