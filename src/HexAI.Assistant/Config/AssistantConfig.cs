using System.Text.Json;
using System.Text.Json.Serialization;

namespace HexAI.Assistant.Config;

/// <summary>
/// Aktif Persona.
/// </summary>
public enum AIPersona
{
    /// <summary>Standart asistan (varsayılan).</summary>
    Standard,
    /// <summary>Yaratıcı ve eğlenceli persona.</summary>
    Creative,
    /// <summary>Kodlama uzmanı persona.</summary>
    Coder,
    /// <summary>Sert, resmi veya rol yapma personasi.</summary>
    Roleplay
}

/// <summary>
/// Bilgisayar donanım seviyesi ve parametre ölçekleme profili.
/// </summary>
public enum HardwareTier
{
    /// <summary>Standart sistem (8-16 GB RAM)</summary>
    Standard,
    /// <summary>Gelişmiş sistem (32 GB RAM)</summary>
    High,
    /// <summary>Canavar Sistem (Intel Core i9 Ultra, 96GB RAM, RTX 5090, 6TB)</summary>
    Ultra
}

/// <summary>
/// Asistan yapılandırma ayarları.
/// JSON dosyasından okunur ve yazılır.
/// </summary>
public class AssistantConfig
{
    /// <summary>Aktif donanım seviyesi ve parametre ölçeği.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HardwareTier HardwareProfile { get; set; } = HardwareTier.Ultra;

    /// <summary>
    /// Donanım profiline göre parametre ölçeklerini ayarlar.
    /// </summary>
    public void ApplyHardwareTier(HardwareTier tier)
    {
        HardwareProfile = tier;
        switch (tier)
        {
            case HardwareTier.Standard:
                HiddenSize = 64;
                NumHeads = 2;
                NumLayers = 1;
                SequenceLength = 16;
                MemorySize = 20;
                ResearchMaxSources = 3;
                ResearchCharsPerSource = 2000;
                break;
            case HardwareTier.High:
                HiddenSize = 128;
                NumHeads = 4;
                NumLayers = 2;
                SequenceLength = 24;
                MemorySize = 50;
                ResearchMaxSources = 4;
                ResearchCharsPerSource = 3500;
                break;
            case HardwareTier.Ultra:
            default:
                HiddenSize = 256;
                NumHeads = 8;
                NumLayers = 4;
                SequenceLength = 32;
                MemorySize = 100;
                ResearchMaxSources = 6;
                ResearchCharsPerSource = 5000;
                break;
        }
    }

    /// <summary>Asistanın adı.</summary>
    public string AssistantName { get; set; } = "HexAI";

    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; set; } = "Kullanıcı";

    /// <summary>Varsayılan model dizini.</summary>
    public string ModelsDirectory { get; set; } = "data/models";

    /// <summary>Eğitim verisi dizini.</summary>
    public string CorpusDirectory { get; set; } = "data/corpus";

    /// <summary>Log dizini.</summary>
    public string LogsDirectory { get; set; } = "data/logs";

    /// <summary>MiniGPT / LargeGPT gizli katman boyutu (Ultra: 256).</summary>
    public int HiddenSize { get; set; } = 256;

    /// <summary>Attention kafa sayısı (LargeGPT için - Ultra: 8).</summary>
    public int NumHeads { get; set; } = 8;

    /// <summary>Transformer blok sayısı (LargeGPT için - Ultra: 4).</summary>
    public int NumLayers { get; set; } = 4;

    /// <summary>Sıra uzunluğu (LargeGPT bağlam penceresi - Ultra: 32 kelime).</summary>
    public int SequenceLength { get; set; } = 32;

    /// <summary>Öğrenme oranı.</summary>
    public double LearningRate { get; set; } = 0.02;

    /// <summary>Varsayılan eğitim epoch sayısı.</summary>
    public int DefaultEpochs { get; set; } = 300;

    /// <summary>Metin üretim sıcaklığı (temperature).</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>Sohbet hafıza boyutu (son N mesaj - Ultra: 100).</summary>
    public int MemorySize { get; set; } = 100;

    /// <summary>Son aktif model adı.</summary>
    public string? LastModelName { get; set; }

    /// <summary>Otomatik kaydetme açık mı?</summary>
    public bool AutoSave { get; set; } = true;

    // ─── Sistem Prompt & Persona Seçimi ───

    /// <summary>Özelleştirilebilir sistem prompt'u.</summary>
    public string SystemPrompt { get; set; } = "Sen HexAI adında, Türkçe konuşan, yardımsever bir yapay zeka asistanısın. Kısa, öz ve doğru yanıtlar ver.";

    // ─── Araştırma & Web Ayarları ───

    /// <summary>Araştırmada okunacak maksimum web sayfası.</summary>
    public int ResearchMaxSources { get; set; } = 5;

    /// <summary>Her kaynaktan alınacak maksimum karakter.</summary>
    public int ResearchCharsPerSource { get; set; } = 3500;

    /// <summary>Güncel bilgi gerektiren sorularda otomatik web araştırması yap.</summary>
    public bool AutoWebResearch { get; set; } = true;

    /// <summary>Aktif Asistan Persona Modu.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AIPersona ActivePersona { get; set; } = AIPersona.Standard;

    /// <summary>Token kullanım takibi açık mı?</summary>
    public bool TrackTokenUsage { get; set; } = true;

    /// <summary>Toplam kullanılan token sayısı (kalıcı).</summary>
    public long TotalTokensUsed { get; set; } = 0;

    /// <summary>Oturumdaki token kullanımı.</summary>
    [JsonIgnore]
    public long SessionTokensUsed { get; set; } = 0;

    // ─── Sistem Ayarları ───

    /// <summary>Model doğrulama sistemi açık mı?</summary>
    public bool EnableVerification { get; set; } = true;

    /// <summary>Doğrulama minimum onay eşiği (0-10).</summary>
    public double VerificationThreshold { get; set; } = 7.0;

    /// <summary>Bilgi tabanı dizini.</summary>
    public string KnowledgeBasePath { get; set; } = "data/knowledge";

    /// <summary>Yazma animasyonu açık mı?</summary>
    public bool TypewriterEffect { get; set; } = true;

    /// <summary>Yazma animasyonu gecikme (ms).</summary>
    public int TypewriterDelayMs { get; set; } = 15;

    /// <summary>Sesli yanıt (Text-to-Speech) açık mı?</summary>
    public bool VoiceOutputEnabled { get; set; } = false;

    /// <summary>Yapılandırma dosyası yolu.</summary>
    private static string ConfigPath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "data", "config.json");

    /// <summary>
    /// Yapılandırmayı dosyadan yükler. Yoksa varsayılan oluşturur.
    /// </summary>
    public static AssistantConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AssistantConfig>(json) ?? new AssistantConfig();
            }
        }
        catch { /* Sessizce varsayılanı döndür */ }

        var config = new AssistantConfig();
        config.Save(); // Varsayılan dosyayı oluştur
        return config;
    }

    /// <summary>
    /// Yapılandırmayı dosyaya kaydeder.
    /// </summary>
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (dir != null) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { /* Kaydetme hatası sessizce geçilir */ }
    }

    /// <summary>
    /// Gerekli dizinleri oluşturur.
    /// </summary>
    public void EnsureDirectories()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(Path.Combine(baseDir, ModelsDirectory));
        Directory.CreateDirectory(Path.Combine(baseDir, CorpusDirectory));
        Directory.CreateDirectory(Path.Combine(baseDir, LogsDirectory));
        Directory.CreateDirectory(Path.Combine(baseDir, KnowledgeBasePath));
        Directory.CreateDirectory(GetDataPath());
    }

    /// <summary>Tam model dizin yolunu döndürür.</summary>
    public string GetModelsPath() => ResolvePath(ModelsDirectory);

    /// <summary>Tam corpus dizin yolunu döndürür.</summary>
    public string GetCorpusPath() => ResolvePath(CorpusDirectory);

    /// <summary>Tam log dizin yolunu döndürür.</summary>
    public string GetLogsPath() => ResolvePath(LogsDirectory);

    /// <summary>Veri kök dizini (vektör deposu vb. için).</summary>
    public string GetDataPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "HexAI", "data");

    /// <summary>Tam bilgi tabanı dizin yolunu döndürür.</summary>
    public string GetKnowledgeBasePath() => ResolvePath(KnowledgeBasePath);

    private static string ResolvePath(string relativePath)
    {
        string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        if (Directory.Exists(p1) || File.Exists(p1)) return p1;

        string p2 = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (Directory.Exists(p2) || File.Exists(p2)) return p2;

        try
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 4 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, relativePath);
                if (Directory.Exists(candidate) || File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
        }
        catch { }

        return p1;
    }
}
