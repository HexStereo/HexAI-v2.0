namespace HexAI.Assistant.Knowledge;

/// <summary>
/// Belgeleri dil modeli için uygun parçalara (chunk) ayırır.
/// 
/// Desteklenen formatlar: TXT, MD, CS, PY, JS, TS, JSON, XML, HTML, CSV
/// Sliding window + overlap ile parçalar oluşturur.
/// Paragraf ve cümle sınırlarına duyarlıdır.
/// </summary>
public class DocumentChunker
{
    /// <summary>Varsayılan chunk boyutu (karakter).</summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>Chunk'lar arası örtüşme (karakter).</summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>Minimum chunk boyutu (çok kısa parçaları atla).</summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// Dosyayı okuyup chunk'lara ayırır.
    /// </summary>
    /// <param name="filePath">Dosya yolu.</param>
    /// <returns>Chunk listesi (metin + metadata).</returns>
    public List<DocumentChunk> ChunkFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");

        string content = File.ReadAllText(filePath);
        string extension = Path.GetExtension(filePath).ToLower();
        string fileName = Path.GetFileName(filePath);

        var chunks = extension switch
        {
            ".cs" or ".py" or ".js" or ".ts" or ".java" or ".cpp" or ".c" or ".go" or ".rs"
                => ChunkCode(content, fileName),
            ".md" or ".markdown"
                => ChunkMarkdown(content, fileName),
            ".json" or ".xml" or ".yaml" or ".yml"
                => ChunkStructured(content, fileName),
            _ => ChunkText(content, fileName)
        };

        return chunks;
    }

    /// <summary>
    /// Bir dizindeki tüm desteklenen dosyaları chunk'lara ayırır.
    /// </summary>
    public List<DocumentChunk> ChunkDirectory(string directoryPath, bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Dizin bulunamadı: {directoryPath}");

        var supportedExtensions = new HashSet<string>
        {
            ".txt", ".md", ".cs", ".py", ".js", ".ts", ".java", ".cpp", ".c", ".h",
            ".go", ".rs", ".json", ".xml", ".yaml", ".yml", ".html", ".css",
            ".toml", ".ini", ".cfg", ".log", ".sql", ".sh", ".bat", ".ps1"
        };

        var allChunks = new List<DocumentChunk>();
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true
        };

        foreach (var file in Directory.GetFiles(directoryPath, "*.*", options))
        {
            string ext = Path.GetExtension(file).ToLower();
            if (!supportedExtensions.Contains(ext)) continue;

            try
            {
                var fileInfo = new FileInfo(file);
                // 1MB üstü dosyaları atla
                if (fileInfo.Length > 1024 * 1024) continue;

                allChunks.AddRange(ChunkFile(file));
            }
            catch { /* Okunamayan dosyaları sessizce atla */ }
        }

        return allChunks;
    }

    /// <summary>
    /// Ham metni sliding window ile chunk'lara ayırır.
    /// Paragraf sınırlarına duyarlıdır.
    /// </summary>
    public List<DocumentChunk> ChunkText(string text, string source = "text")
    {
        var chunks = new List<DocumentChunk>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // Paragraflara ayır
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new System.Text.StringBuilder();
        int chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            string trimmed = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Mevcut chunk'a eklenirse boyut aşılır mı?
            if (currentChunk.Length + trimmed.Length > ChunkSize && currentChunk.Length > MinChunkSize)
            {
                // Mevcut chunk'ı kaydet
                chunks.Add(CreateChunk(currentChunk.ToString().Trim(), source, chunkIndex));
                chunkIndex++;

                // Overlap: son kısmı yeni chunk'a taşı
                string overlap = GetOverlapText(currentChunk.ToString());
                currentChunk.Clear();
                if (!string.IsNullOrWhiteSpace(overlap))
                    currentChunk.Append(overlap + "\n\n");
            }

            currentChunk.AppendLine(trimmed);
            currentChunk.AppendLine();
        }

        // Son chunk
        if (currentChunk.Length >= MinChunkSize)
        {
            chunks.Add(CreateChunk(currentChunk.ToString().Trim(), source, chunkIndex));
        }

        return chunks;
    }

    /// <summary>
    /// Kaynak kodu fonksiyon/sınıf sınırlarına göre chunk'lara ayırır.
    /// </summary>
    private List<DocumentChunk> ChunkCode(string code, string source)
    {
        var chunks = new List<DocumentChunk>();
        var lines = code.Split('\n');
        var currentChunk = new System.Text.StringBuilder();
        int chunkIndex = 0;
        int braceDepth = 0;

        foreach (var line in lines)
        {
            currentChunk.AppendLine(line);

            // Süslü parantez takibi (fonksiyon/sınıf sınırları)
            foreach (char c in line)
            {
                if (c == '{') braceDepth++;
                if (c == '}') braceDepth--;
            }

            // Chunk boyutuna ulaşıldıysa ve bir blok kapandıysa (braceDepth == 0 veya 1)
            if (currentChunk.Length >= ChunkSize && braceDepth <= 1)
            {
                chunks.Add(CreateChunk(currentChunk.ToString().Trim(), source, chunkIndex, "code"));
                chunkIndex++;

                string overlap = GetOverlapText(currentChunk.ToString());
                currentChunk.Clear();
                if (!string.IsNullOrWhiteSpace(overlap))
                    currentChunk.AppendLine(overlap);
            }
        }

        if (currentChunk.Length >= MinChunkSize)
        {
            chunks.Add(CreateChunk(currentChunk.ToString().Trim(), source, chunkIndex, "code"));
        }

        return chunks;
    }

    /// <summary>
    /// Markdown'u başlık sınırlarına göre chunk'lara ayırır.
    /// </summary>
    private List<DocumentChunk> ChunkMarkdown(string markdown, string source)
    {
        var chunks = new List<DocumentChunk>();
        var sections = markdown.Split(new[] { "\n# ", "\n## ", "\n### " }, StringSplitOptions.None);
        int chunkIndex = 0;

        var currentChunk = new System.Text.StringBuilder();

        foreach (var section in sections)
        {
            if (currentChunk.Length + section.Length > ChunkSize && currentChunk.Length > MinChunkSize)
            {
                chunks.Add(CreateChunk(currentChunk.ToString().Trim(), source, chunkIndex, "markdown"));
                chunkIndex++;
                currentChunk.Clear();
            }

            currentChunk.AppendLine(section);
        }

        if (currentChunk.Length >= MinChunkSize)
        {
            chunks.Add(CreateChunk(currentChunk.ToString().Trim(), source, chunkIndex, "markdown"));
        }

        return chunks;
    }

    /// <summary>
    /// Yapılandırılmış veri dosyalarını (JSON, XML vb.) chunk'lara ayırır.
    /// </summary>
    private List<DocumentChunk> ChunkStructured(string content, string source)
    {
        // Yapılandırılmış veriler genelde tek parça olarak daha anlamlıdır
        if (content.Length <= ChunkSize)
        {
            return new List<DocumentChunk>
            {
                CreateChunk(content, source, 0, "structured")
            };
        }

        // Büyük yapılandırılmış dosyalar için genel text chunking kullan
        return ChunkText(content, source);
    }

    private DocumentChunk CreateChunk(string text, string source, int index, string type = "text")
    {
        return new DocumentChunk
        {
            Id = $"{source}::chunk_{index}",
            Text = text,
            Source = source,
            ChunkIndex = index,
            ContentType = type,
            CharCount = text.Length,
            EstimatedTokens = (int)(text.Length / 3.5)
        };
    }

    private string GetOverlapText(string text)
    {
        if (text.Length <= ChunkOverlap) return text;
        return text[^ChunkOverlap..];
    }
}

/// <summary>
/// Bir belge parçası (chunk).
/// </summary>
public class DocumentChunk
{
    /// <summary>Benzersiz tanımlayıcı (source::chunk_N formatında).</summary>
    public string Id { get; set; } = "";

    /// <summary>Chunk metni.</summary>
    public string Text { get; set; } = "";

    /// <summary>Kaynak dosya adı.</summary>
    public string Source { get; set; } = "";

    /// <summary>Chunk sıra numarası.</summary>
    public int ChunkIndex { get; set; }

    /// <summary>İçerik türü (text, code, markdown, structured).</summary>
    public string ContentType { get; set; } = "text";

    /// <summary>Karakter sayısı.</summary>
    public int CharCount { get; set; }

    /// <summary>Tahmini token sayısı.</summary>
    public int EstimatedTokens { get; set; }
}
