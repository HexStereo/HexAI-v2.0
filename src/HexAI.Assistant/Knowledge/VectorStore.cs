using System.Text.Json;

namespace HexAI.Assistant.Knowledge;

/// <summary>
/// In-memory vektör veritabanı — metin parçalarını embedding vektörleriyle saklar ve
/// cosine similarity ile en benzer parçaları bulur.
/// 
/// HNSW yerine brute-force arama kullanır (basitlik ve doğruluk için).
/// 96GB RAM ile milyonlarca vektör tutulabilir.
/// </summary>
public class VectorStore : IDisposable
{
    private readonly List<VectorEntry> _entries = new();
    private readonly string? _persistPath;
    private readonly object _lock = new();

    /// <summary>Toplam vektör sayısı.</summary>
    public int Count => _entries.Count;

    /// <summary>Vektör boyutu (embedding dimension).</summary>
    public int Dimension { get; private set; }

    /// <summary>
    /// Yeni bir VectorStore oluşturur.
    /// </summary>
    /// <param name="persistPath">Kalıcı depolama dosya yolu (opsiyonel).</param>
    public VectorStore(string? persistPath = null)
    {
        _persistPath = persistPath;
        if (_persistPath != null)
        {
            Load();
        }
    }

    /// <summary>
    /// Yeni bir vektör girişi ekler.
    /// </summary>
    /// <param name="id">Benzersiz tanımlayıcı.</param>
    /// <param name="text">Orijinal metin parçası.</param>
    /// <param name="embedding">Embedding vektörü.</param>
    /// <param name="metadata">Ek metadata (kaynak dosya, sayfa numarası vb.).</param>
    public void Add(string id, string text, float[] embedding, Dictionary<string, string>? metadata = null)
    {
        lock (_lock)
        {
            if (Dimension == 0)
                Dimension = embedding.Length;
            else if (embedding.Length != Dimension)
                throw new ArgumentException($"Vektör boyutu uyuşmuyor: beklenen {Dimension}, gelen {embedding.Length}");

            // Normalize et (cosine similarity için)
            float[] normalized = Normalize(embedding);

            _entries.Add(new VectorEntry
            {
                Id = id,
                Text = text,
                Embedding = normalized,
                Metadata = metadata ?? new()
            });
        }
    }

    /// <summary>
    /// Verilen query embedding'e en benzer N sonucu döndürür.
    /// Cosine similarity kullanır.
    /// </summary>
    /// <param name="queryEmbedding">Sorgu vektörü.</param>
    /// <param name="topK">Döndürülecek sonuç sayısı.</param>
    /// <param name="minSimilarity">Minimum benzerlik eşiği (0.0 - 1.0).</param>
    public List<SearchResult> Search(float[] queryEmbedding, int topK = 5, float minSimilarity = 0.3f)
    {
        if (_entries.Count == 0)
            return new List<SearchResult>();

        float[] normalizedQuery = Normalize(queryEmbedding);

        var results = new List<SearchResult>();

        lock (_lock)
        {
            foreach (var entry in _entries)
            {
                float similarity = CosineSimilarity(normalizedQuery, entry.Embedding);
                if (similarity >= minSimilarity)
                {
                    results.Add(new SearchResult
                    {
                        Id = entry.Id,
                        Text = entry.Text,
                        Similarity = similarity,
                        Metadata = entry.Metadata
                    });
                }
            }
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .ToList();
    }

    /// <summary>
    /// Belirli bir ID'ye sahip girişi siler.
    /// </summary>
    public bool Remove(string id)
    {
        lock (_lock)
        {
            return _entries.RemoveAll(e => e.Id == id) > 0;
        }
    }

    /// <summary>
    /// Belirli bir kaynak (source) ile eşleşen tüm girişleri siler.
    /// Dosya yeniden indexlenirken kullanılır.
    /// </summary>
    public int RemoveBySource(string source)
    {
        lock (_lock)
        {
            return _entries.RemoveAll(e =>
                e.Metadata.TryGetValue("source", out var s) && s == source);
        }
    }

    /// <summary>Tüm girişleri temizler.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            Dimension = 0;
        }
    }

    /// <summary>Vektör deposunu dosyaya kaydeder.</summary>
    public void Save()
    {
        if (_persistPath == null) return;

        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_persistPath);
                if (dir != null) Directory.CreateDirectory(dir);

                var data = new VectorStoreData
                {
                    Dimension = Dimension,
                    Entries = _entries
                };

                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(_persistPath, json);
            }
            catch { }
        }
    }

    /// <summary>Vektör deposunu dosyadan yükler.</summary>
    private void Load()
    {
        if (_persistPath == null || !File.Exists(_persistPath)) return;

        try
        {
            string json = File.ReadAllText(_persistPath);
            var data = JsonSerializer.Deserialize<VectorStoreData>(json);
            if (data != null)
            {
                Dimension = data.Dimension;
                _entries.Clear();
                _entries.AddRange(data.Entries);
            }
        }
        catch { }
    }

    /// <summary>Cosine similarity hesaplar (normalize edilmiş vektörler için dot product).</summary>
    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0;
        int len = Math.Min(a.Length, b.Length);

        // SIMD-friendly loop
        for (int i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
        }

        return dot; // Vektörler normalize edildiği için dot product = cosine similarity
    }

    /// <summary>Vektörü L2 normalize eder.</summary>
    private static float[] Normalize(float[] vector)
    {
        float magnitude = 0;
        for (int i = 0; i < vector.Length; i++)
            magnitude += vector[i] * vector[i];

        magnitude = MathF.Sqrt(magnitude);

        if (magnitude < 1e-10f)
            return vector;

        float[] normalized = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
            normalized[i] = vector[i] / magnitude;

        return normalized;
    }

    public void Dispose()
    {
        Save();
        GC.SuppressFinalize(this);
    }

    /// <summary>İstatistikleri döndürür.</summary>
    public string GetStats()
    {
        return $"📊 VectorStore: {Count} vektör, Boyut: {Dimension}, " +
               $"Bellek: ~{Count * Dimension * 4 / (1024.0 * 1024):F1} MB";
    }
}

/// <summary>Vektör arama sonucu.</summary>
public class SearchResult
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public float Similarity { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>Vektör girişi (iç kullanım).</summary>
internal class VectorEntry
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>Serialize/deserialize için veri yapısı.</summary>
internal class VectorStoreData
{
    public string StoreSignature { get; set; } = "SGV4U3RlcmVv";
    public int Dimension { get; set; }
    public List<VectorEntry> Entries { get; set; } = new();
}
