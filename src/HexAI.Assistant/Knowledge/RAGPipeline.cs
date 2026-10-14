using System.Text;

namespace HexAI.Assistant.Knowledge;

/// <summary>
/// HexAI Yerel RAG (Retrieval Augmented Generation) Pipeline.
/// Harici model gerektirmeden, HexAI'ın kendi n-gram & sözcük tabanlı vektör projeksiyonuyla:
///   1. Belgeleri semantik chunk'lara ayırır.
///   2. 128 boyutlu yerel embedding vektörlerini hesaplar.
///   3. VectorStore içinde Cosine Similarity ile arama yapar.
///   4. İlgili bilgileri getirir ve raporlar.
/// </summary>
public class RAGPipeline
{
    private readonly VectorStore _vectorStore;
    private readonly DocumentChunker _chunker;
    private const int EmbeddingDimension = 128;

    public int TopK { get; set; } = 5;
    public float MinSimilarity { get; set; } = 0.25f;
    public int IndexedFileCount { get; private set; }
    public int TotalChunkCount => _vectorStore.Count;

    public RAGPipeline(VectorStore vectorStore, DocumentChunker? chunker = null)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _chunker = chunker ?? new DocumentChunker();
    }

    /// <summary>
    /// HexAI'ın kendi matematiksel metin vektörleştiricisi (128 boyutlu normalize vektör).
    /// </summary>
    public static float[] ComputeEmbedding(string text)
    {
        float[] vector = new float[EmbeddingDimension];
        if (string.IsNullOrWhiteSpace(text)) return vector;

        string normalized = text.ToLowerInvariant();
        var words = normalized.Split(new[] { ' ', '\t', '\r', '\n', '.', ',', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            // Kelime hash'i ile boyut projeksiyonu
            uint hash = 2166136261;
            foreach (char c in word)
                hash = (hash ^ c) * 16777619;

            int index = (int)(hash % EmbeddingDimension);
            vector[index] += 1.0f;

            // Karakter 3-gram'ları da ekle (morfolojik benzerlik yakalamak için)
            if (word.Length >= 3)
            {
                for (int i = 0; i <= word.Length - 3; i++)
                {
                    uint gHash = (uint)(word[i] * 31 * 31 + word[i + 1] * 31 + word[i + 2]);
                    int gIndex = (int)(gHash % EmbeddingDimension);
                    vector[gIndex] += 0.5f;
                }
            }
        }

        // L2 Normalize
        double sumSq = 0;
        for (int i = 0; i < EmbeddingDimension; i++) sumSq += vector[i] * vector[i];
        if (sumSq > 0)
        {
            float norm = (float)Math.Sqrt(sumSq);
            for (int i = 0; i < EmbeddingDimension; i++) vector[i] /= norm;
        }

        return vector;
    }

    public async Task IndexFileAsync(string filePath, Action<int, int>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var chunks = _chunker.ChunkFile(filePath);
        if (chunks.Count == 0) return;

        _vectorStore.RemoveBySource(Path.GetFileName(filePath));

        for (int i = 0; i < chunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunk = chunks[i];
            float[] embedding = ComputeEmbedding(chunk.Text);

            _vectorStore.Add(chunk.Id, chunk.Text, embedding, new Dictionary<string, string>
            {
                ["source"] = chunk.Source,
                ["chunk_index"] = chunk.ChunkIndex.ToString(),
                ["content_type"] = chunk.ContentType
            });

            onProgress?.Invoke(i + 1, chunks.Count);
        }

        IndexedFileCount++;
        _vectorStore.Save();
        await Task.CompletedTask;
    }

    public async Task IndexDirectoryAsync(string directoryPath, bool recursive = true,
        Action<string, int, int>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var chunks = _chunker.ChunkDirectory(directoryPath, recursive);
        if (chunks.Count == 0) return;

        for (int i = 0; i < chunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunk = chunks[i];
            float[] embedding = ComputeEmbedding(chunk.Text);

            _vectorStore.Add(chunk.Id, chunk.Text, embedding, new Dictionary<string, string>
            {
                ["source"] = chunk.Source,
                ["chunk_index"] = chunk.ChunkIndex.ToString(),
                ["content_type"] = chunk.ContentType
            });

            onProgress?.Invoke(chunk.Source, i + 1, chunks.Count);
        }

        IndexedFileCount++;
        _vectorStore.Save();
        await Task.CompletedTask;
    }

    public Task<RAGResult> RetrieveAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_vectorStore.Count == 0)
        {
            return Task.FromResult(new RAGResult
            {
                Query = query,
                Context = "",
                Sources = new List<string>(),
                ChunkCount = 0,
                HasResults = false
            });
        }

        float[] queryEmbedding = ComputeEmbedding(query);
        var results = _vectorStore.Search(queryEmbedding, TopK, MinSimilarity);

        if (results.Count == 0)
        {
            return Task.FromResult(new RAGResult
            {
                Query = query,
                Context = "",
                Sources = new List<string>(),
                ChunkCount = 0,
                HasResults = false
            });
        }

        var contextBuilder = new StringBuilder();
        var sources = new HashSet<string>();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            string source = result.Metadata.GetValueOrDefault("source", "bilinmeyen");
            sources.Add(source);

            contextBuilder.AppendLine($"[Kaynak: {source} | Skor: {result.Similarity:F2}]");
            contextBuilder.AppendLine(result.Text);
            contextBuilder.AppendLine();
        }

        return Task.FromResult(new RAGResult
        {
            Query = query,
            Context = contextBuilder.ToString().Trim(),
            Sources = sources.ToList(),
            ChunkCount = results.Count,
            HasResults = true,
            TopSimilarity = results[0].Similarity,
            SearchResults = results
        });
    }

    public string GetStats()
    {
        return $"📚 Yerel RAG Pipeline:\n" +
               $"  İndexlenmiş Dosya: {IndexedFileCount}\n" +
               $"  Toplam Vektör    : {TotalChunkCount}\n" +
               $"  {_vectorStore.GetStats()}";
    }
}

public class RAGResult
{
    public string Query { get; set; } = "";
    public string Context { get; set; } = "";
    public List<string> Sources { get; set; } = new();
    public int ChunkCount { get; set; }
    public bool HasResults { get; set; }
    public float TopSimilarity { get; set; }
    public List<SearchResult> SearchResults { get; set; } = new();
}
