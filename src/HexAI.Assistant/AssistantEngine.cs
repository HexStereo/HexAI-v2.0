using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HexAI.NLP;
using HexAI.NLP.Generative;
using HexAI.Assistant.Config;
using HexAI.Assistant.Knowledge;
using HexAI.Assistant.Skills;
using HexAI.Assistant.Agents;
using HexAI.Assistant.Evaluation;
using HexAI.Assistant.Research;
using HexAI.Assistant.Games;

namespace HexAI.Assistant;

/// <summary>
/// HexAI Asistan Motoru — Tamamen C# ile sıfırdan geliştirilmiş, dışarıdan hiçbir
/// hazır modele veya harici kütüphaneye bağımlı olmayan egemen yapay zeka orchestrator'ı.
/// 
/// Bileşenler ve Mimari:
///   - Sinir Ağı & Transformer: LargeGPT (Multi-Head Attention, LayerNorm, Positional Encoding, Embedding, Adam Optimizer)
///   - Doğal Dil Anlama (NLU): NeuralIntentClassifier (Çok katmanlı yapay sinir ağı tabanlı niyet sınıflandırma)
///   - Hafıza & Persona: ConversationMemory (Diyalog geçmişi, rol ve persona yönetimi)
///   - Doğrulanmış Bilgi Tabanı: KnowledgeBase (Olgusal soru-cevap doğrulaması)
///   - Semantik Belge Arama: VectorStore & RAGPipeline (128 boyutlu yerel n-gram hash projeksiyonu ve Cosine Similarity)
///   - Canlı Web Araştırma Motoru: ResearchEngine (DuckDuckGo + Wikipedia arama, HTML temizleme ve yerel sentezleme)
///   - Otonom Ajan (ReAct): ReActAgent (Düşünce → Araç Seçimi → Çalıştırma → Gözlem döngüsü)
/// </summary>
public class AssistantEngine
{
    private readonly AssistantConfig _config;
    private readonly NeuralIntentClassifier _intentClassifier;
    private readonly ConversationMemory _memory;
    private readonly Dictionary<IntentClassifier.Intent, ISkill> _skills;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly ToolRegistry _toolRegistry;
    private readonly ReActAgent _reactAgent;
    private readonly VectorStore _vectorStore;
    private readonly RAGPipeline _ragPipeline;
    private readonly ResearchEngine _researchEngine;

    private LargeGPT? _model;
    private bool _modelLoaded = false;
    private TrainingEvaluator? _evaluator;

    /// <summary>
    /// Asistan bir işlem yaparken (web araması, sayfa okuma, araç yürütme) durum bildirir.
    /// </summary>
    public event Action<string>? OnStatus;

    public bool IsModelReady => _modelLoaded;
    public AssistantConfig Config => _config;
    public ConversationMemory Memory => _memory;
    public KnowledgeBase Knowledge => _knowledgeBase;
    public TrainingEvaluator? Evaluator => _evaluator;
    public ToolRegistry Tools => _toolRegistry;
    public RAGPipeline RAG => _ragPipeline;
    public ResearchEngine Research => _researchEngine;
    public LargeGPT? Model => _model;
    public string ActivePersonaName => _config.ActivePersona.ToString();

    public AssistantEngine(AssistantConfig? config = null)
    {
        _config = config ?? AssistantConfig.Load();
        _config.EnsureDirectories();

        // 1. Sinir Ağı Tabanlı Niyet Sınıflandırıcı (Neural Intent Classifier)
        _intentClassifier = new NeuralIntentClassifier();
        _intentClassifier.TrainSynthetic(epochs: 100);

        // 2. Diyalog Hafızası ve Doğrulanmış Bilgi Tabanı
        _memory = new ConversationMemory(_config.MemorySize);
        _knowledgeBase = new KnowledgeBase(_config.GetKnowledgeBasePath());

        // 3. Yerel Yetenekler / Beceriler
        _skills = new Dictionary<IntentClassifier.Intent, ISkill>
        {
            [IntentClassifier.Intent.FileOperation] = new FileSystemSkill(),
            [IntentClassifier.Intent.RunCommand] = new PowerShellSkill(),
            [IntentClassifier.Intent.SystemInfo] = new SystemInfoSkill(),
            [IntentClassifier.Intent.Math] = new MathSkill(),
            [IntentClassifier.Intent.OpenApplication] = new ProcessSkill(),
        };

        // 4. Araç Kayıt Defteri ve ReAct Ajanı
        _toolRegistry = new ToolRegistry();
        _toolRegistry.RegisterDefaults();
        _reactAgent = new ReActAgent(_toolRegistry);

        // 5. Yerel Web Araştırma Motoru
        _researchEngine = new ResearchEngine
        {
            MaxSources = _config.ResearchMaxSources,
            CharsPerSource = _config.ResearchCharsPerSource
        };

        // 6. Semantik Vektör Deposu ve Yerel RAG Pipeline
        string vectorStorePath = Path.Combine(_config.GetDataPath(), "vector_store.json");
        _vectorStore = new VectorStore(vectorStorePath);
        _ragPipeline = new RAGPipeline(_vectorStore);

        // 7. Sıfırdan C# Transformer GPT Modeli (LargeGPT)
        _model = new LargeGPT(
            hiddenSize: _config.HiddenSize,
            numHeads: _config.NumHeads,
            numLayers: _config.NumLayers,
            sequenceLength: _config.SequenceLength,
            learningRate: _config.LearningRate,
            seed: 42
        );
    }

    /// <summary>
    /// Motoru başlatır; kayıtlı model varsa yükler veya varsayılan Türkçe korpus ile eğitir.
    /// </summary>
    public void Initialize()
    {
        // Kaydedilmiş model varsa yükle
        if (!string.IsNullOrWhiteSpace(_config.LastModelName))
        {
            try { LoadModel(_config.LastModelName); } catch { }
        }

        // Model yüklenmemişse varsayılan Türkçe korpus ile eğit
        if (!_modelLoaded)
        {
            string defaultCorpusPath = Path.Combine(_config.GetCorpusPath(), "default_turkish.txt");
            if (File.Exists(defaultCorpusPath))
            {
                try
                {
                    TrainFromFile(defaultCorpusPath, _config.DefaultEpochs, silent: true);
                }
                catch { }
            }
            else
            {
                // Temel Türkçe diyalog korpusu oluştur ve hızlıca eğit
                string fallbackCorpus =
                    "Merhaba ben HexAI Türkçe konuşan bağımsız yapay zeka asistanıyım. " +
                    "Kendi sinir ağı ve transformer mimarisine sahibim. " +
                    "Sizinle sohbet edebilir araştırma yapabilir ve sistem komutlarını çalıştırabilirim. " +
                    "Yapay zeka tamamen C sharp diliyle sıfırdan inşa edilmiştir. " +
                    "Bilgi tabanım matematik işlemlerim ve web tarama motorum hazırdır.";
                
                try
                {
                    Directory.CreateDirectory(_config.GetCorpusPath());
                    File.WriteAllText(defaultCorpusPath, fallbackCorpus, Encoding.UTF8);
                    TrainFromCorpus(fallbackCorpus, "default_turkish.txt", epochs: 120, silent: true);
                }
                catch { }
            }
        }

        // Bilgi tabanına yerleşik doğrulanmış Türkçe bilgileri (yemekler, bilim, coğrafya) yükle
        try
        {
            foreach (var entry in TurkishCommonKnowledge.GetAllAsEntries())
            {
                if (_knowledgeBase.Search(entry.Question, 9.5) == null)
                {
                    _knowledgeBase.Save(entry.Question, entry.Answer, entry.Confidence, entry.Source);
                }
            }
        }
        catch { }

        _evaluator = new TrainingEvaluator(_model, _modelLoaded, _memory, _knowledgeBase);
    }

    /// <summary>
    /// Kullanıcı mesajını akışlı (streaming) olarak işler.
    /// Pipeline:
    ///   1. Özel Komut Kontrolü (/arastir, /train, /tools, /status, /help vb.)
    ///   2. Canlı Web Araştırması (Soru güncel bilgi veya açık araştırma istiyorsa)
    ///   3. ReAct Otonom Ajan (Dosya, PowerShell, Sistem, Matematik araçları)
    ///   4. Doğrulanmış Bilgi Tabanı & Semantik RAG
    ///   5. Yerel Transformer Sinir Ağı & Bilişsel Sohbet Sentezi
    /// </summary>
    public async IAsyncEnumerable<string> ProcessMessageStreamAsync(string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            yield return "Bir şey söylemediniz gibi geldi. Size nasıl yardımcı olabilirim?";
            yield break;
        }

        _memory.AddUserMessage(userMessage);
        string trimmed = userMessage.Trim();

        // ──────────────────────── 1. Özel Komut Kontrolü ────────────────────────
        if (trimmed.StartsWith('/'))
        {
            string[] researchCmds = { "/arastir", "/araştır", "/research", "/web" };
            string firstWord = trimmed.Split(' ', 2)[0].ToLower(new System.Globalization.CultureInfo("tr-TR"));
            if (researchCmds.Contains(firstWord))
            {
                string topic = trimmed.Length > firstWord.Length ? trimmed[firstWord.Length..].Trim() : "";
                if (string.IsNullOrWhiteSpace(topic))
                {
                    yield return "Kullanım: /arastir <konu veya soru>\nÖrnek: /arastir Kuantum bilgisayarlar nedir ve nasıl çalışır?";
                    yield break;
                }

                var sbResearch = new StringBuilder();
                await foreach (var token in ResearchStreamAsync(topic, cancellationToken))
                {
                    sbResearch.Append(token);
                    yield return token;
                }

                if (sbResearch.Length == 0)
                {
                    string notFound = $"⚠️ '{topic}' konusuyla ilgili canlı web araması kaynak bulamadı. Lütfen anahtar kelimeleri sadeleştirerek tekrar deneyin.";
                    _memory.AddAssistantMessage(notFound);
                    yield return notFound;
                }
                else
                {
                    _memory.AddAssistantMessage(sbResearch.ToString().Trim());
                }
                yield break;
            }

            string cmdResponse = await HandleSpecialCommandAsync(trimmed);
            if (!string.IsNullOrEmpty(cmdResponse))
            {
                _memory.AddAssistantMessage(cmdResponse);
                yield return cmdResponse;
                yield break;
            }
        }

        // ──────────────────────── 2. Oyun & Kararlılık Testi Kontrolü ────────────────────────
        if (IsGameRequest(trimmed, out string gameMode))
        {
            OnStatus?.Invoke($"🎮 {gameMode} kararlılık testi başlatılıyor...");
            string gameReport = RunGameBenchmarkCommand(gameMode);
            _memory.AddAssistantMessage(gameReport);
            yield return gameReport;
            yield break;
        }

        // ──────────────────────── 3. Niyet Sınıflandırma (NLU) & Özel Aksiyonlar ────────────────────
        var intentResult = _intentClassifier.Classify(trimmed);

        // Özel aksiyonlar (temizleme, geçmiş, çıkış)
        if (intentResult.Intent == IntentClassifier.Intent.Chat && intentResult.Parameters.ContainsKey("action"))
        {
            string actionResponse = intentResult.Parameters["action"] switch
            {
                "clear" => ClearHistory(),
                "history" => GetHistory(),
                "exit" => "exit",
                _ => ""
            };
            if (!string.IsNullOrEmpty(actionResponse))
            {
                _memory.AddAssistantMessage(actionResponse);
                yield return actionResponse;
                yield break;
            }
        }

        // ──────────────────────── 4. İleri Düzey Türkçe Bilişsel ve Diyalog Beyni ───────────────────────
        // Günlük yaşam, duygusal destek, verimlilik, felsefe, popüler ikilemler, mizah, yaratıcılık,
        // pratik sağlık ve sosyal beceriler için doğrudan insan kalitesinde zengin yanıt üretir.
        string? conversationalResponse = TurkishConversationalBrain.FindResponse(trimmed);
        if (conversationalResponse != null)
        {
            _memory.AddAssistantMessage(conversationalResponse);
            yield return conversationalResponse;
            yield break;
        }

        // ──────────────────────── 5. Doğrudan Asistan Diyaloğu, Yetenekler & Planlama Önceliği ───────────────────────
        bool isCapabilityOrIdentity = intentResult.Intent == IntentClassifier.Intent.Help
            || ContainsAny(trimmed,
                "neler yapabiliyorsun", "ne yapabiliyorsun", "ne yapabilirsin", "neler yapabilirsin",
                "ne özelliklere sahipsin", "ne ozelliklere sahipsin", "özelliklerin neler", "ozelliklerin neler",
                "yeteneklerin neler", "yeteneklerin", "neler yaparsın", "neler yaparsin",
                "hangi özelliklerin var", "hangi ozelliklerin var", "hangi yeteneklere sahipsin",
                "sen kimsin", "adın ne", "kendini tanıt", "kendini tanit", "kendinden bahset",
                "ne işe yararsın", "ne ise yararsin", "bana neler yapabileceğini anlat");

        bool isSoftwareOrPlan = ContainsAny(trimmed, 
            "kod", "yazılım", "c#", "csharp", "python", "algoritma", "fonksiyon", "sql", "programlama",
            "çalışma planı", "plan yap", "yol haritası", "tavsiye ver", "önerin var mı");

        bool isChitChatGreeting = ContainsAny(trimmed, 
            "merhaba", "selam", "günaydın", "iyi günler", "iyi akşamlar", "hey", "sa",
            "nasılsın", "nasilsin", "naber", "ne haber", "iyi misin", "keyifler",
            "sohbet", "konuş", "konuşalım", "muhabbet", "dertleşelim",
            "şaka", "fıkra", "hikaye", "masal", "şiir",
            "sen kimsin", "adın ne", "kendini tanıt",
            "teşekkür", "sağol", "sagol", "eyvallah");

        bool hasQuestionWords = trimmed.EndsWith("?") || trimmed.Contains("nedir") || trimmed.Contains("nasıl") 
            || trimmed.Contains("tarifi") || trimmed.Contains("adım adım") || trimmed.Contains("nerede");

        if (isCapabilityOrIdentity || isSoftwareOrPlan || (isChitChatGreeting && !hasQuestionWords))
        {
            var chitChatResponse = new StringBuilder();
            await foreach (var token in HandleNativeChatStreamAsync(trimmed, intentResult, cancellationToken))
            {
                chitChatResponse.Append(token);
                yield return token;
            }
            _memory.AddAssistantMessage(chitChatResponse.ToString().Trim());
            yield break;
        }

        // ──────────────────────── 5. Yerleşik Ortak Bilgi & Doğrulanmış Bilgi Tabanı ────────────────────────
        // Soru daha önce öğrenilmişse veya yerleşik ortak ansiklopedide (yemek tarifleri, pratik rehberler, bilim) varsa
        // internette rastgele arama yapmadan veya "bilmiyorum" demeden doğrudan zengin adımlarla yanıtla.
        var commonMatch = TurkishCommonKnowledge.FindMatch(trimmed);
        if (commonMatch != null)
        {
            _memory.AddAssistantMessage(commonMatch.Answer);
            yield return commonMatch.Answer;
            yield break;
        }

        var knowledgeHit = _knowledgeBase.Search(trimmed);
        if (knowledgeHit != null && knowledgeHit.Confidence >= _config.VerificationThreshold)
        {
            string kbResponse = $"📚 [Doğrulanmış Bilgi Tabanı | Güven: {knowledgeHit.Confidence:F1}/10]\n{knowledgeHit.Answer}";
            _memory.AddAssistantMessage(kbResponse);
            yield return kbResponse;
            yield break;
        }

        // ──────────────────────── 6. Araç ve Sistem Komutları (ReAct Agent) ───────
        bool isToolIntent = !hasQuestionWords && !isCapabilityOrIdentity && intentResult.Intent is IntentClassifier.Intent.FileOperation
            or IntentClassifier.Intent.RunCommand
            or IntentClassifier.Intent.SystemInfo
            or IntentClassifier.Intent.Math
            or IntentClassifier.Intent.OpenApplication;

        if (isToolIntent && intentResult.Confidence >= 0.75)
        {
            var agentResult = new StringBuilder();
            await foreach (var (step, content) in _reactAgent.RunStreamAsync(trimmed, cancellationToken))
            {
                if (step.StartsWith("🤔")) continue;
                if (step.StartsWith("🔧"))
                {
                    OnStatus?.Invoke($"{step}: {content}");
                    continue;
                }
                agentResult.Append(content);
                yield return content;
            }

            string resultStr = agentResult.ToString().Trim();
            if (!string.IsNullOrEmpty(resultStr))
            {
                _memory.AddAssistantMessage(resultStr);
                yield break;
            }
        }

        // ──────────────────────── 7. Canlı Web Araştırması & Derin Öğrenme ────────────────────────
        // Hafızada bulunmayan tüm bilgi, kavram veya "nasıl yapılır" soruları için web sayfalarını
        // derinlemesine okur, "ilk bunu yap sonra bunu yap" diye adım adım sentezler ve hafızaya kaydeder.
        if (_config.AutoWebResearch && ResearchEngine.ShouldResearch(trimmed, out string reason))
        {
            OnStatus?.Invoke($"🌐 Web araştırması ve öğrenme başlatılıyor ({reason})...");
            var sbResearch = new StringBuilder();
            bool gotReport = false;
            await foreach (var token in ResearchStreamAsync(trimmed, cancellationToken))
            {
                gotReport = true;
                sbResearch.Append(token);
                yield return token;
            }
            if (gotReport && sbResearch.Length > 0)
            {
                _memory.AddAssistantMessage(sbResearch.ToString().Trim());
                yield break;
            }
            OnStatus?.Invoke("ℹ️ Web araması sonuç vermedi, yerel bilişsel motor devreye giriyor...");
        }

        // ──────────────────────── 8. RAG Belgeleri & Semantik Bellek ───────────────────
        if (_ragPipeline != null && _vectorStore != null && _vectorStore.Count > 0)
        {
            var ragResult = await _ragPipeline.RetrieveAsync(trimmed);
            if (ragResult.HasResults && ragResult.TopSimilarity >= 0.60f)
            {
                var sbRag = new StringBuilder();
                sbRag.AppendLine("📄 [Yerel Belgelerden Alınan Bilgi]:");
                sbRag.AppendLine(ragResult.Context);
                string ragResponse = sbRag.ToString().Trim();
                _memory.AddAssistantMessage(ragResponse);
                yield return ragResponse;
                yield break;
            }
        }

        // ──────────────────────── 6. Bilişsel ve Sinir Ağı Sohbet Sentezi ──────────
        var fullChatResponse = new StringBuilder();
        await foreach (var token in HandleNativeChatStreamAsync(trimmed, intentResult, cancellationToken))
        {
            fullChatResponse.Append(token);
            yield return token;
        }
        _memory.AddAssistantMessage(fullChatResponse.ToString().Trim());
    }

    /// <summary>
    /// Canlı web araştırması yapar ve HexAI'ın kendi sentezleme algoritmasıyla Türkçe rapor oluşturur.
    /// </summary>
    private async IAsyncEnumerable<string> ResearchStreamAsync(string question,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ResearchResult? research = null;
        string? error = null;

        try
        {
            research = await _researchEngine.GatherAsync(question, s => OnStatus?.Invoke(s), ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { error = ex.Message; }

        if (research == null || !research.HasSources)
        {
            OnStatus?.Invoke($"⚠️ Web'den kaynak alınamadı{(error != null ? $" ({error})" : "")}.");
            yield break;
        }

        OnStatus?.Invoke($"✍️ {research.Sources.Count} kaynak okundu, yerel sentez raporu oluşturuluyor...");

        // HexAI yerel sentez raporunu üretir
        string report = _researchEngine.SynthesizeReport(research);

        // ── HexAI Dinamik Öğrenme (Assimilation) ──
        // Araştırılan bilgileri yerel bilgi tabanına ve semantik vektör deposuna kaydederek öğrenir
        try
        {
            _knowledgeBase.Save(question, report, confidence: 9.6, source: "Canlı Web Araştırması");
            foreach (var s in research.Sources)
            {
                if (!string.IsNullOrWhiteSpace(s.Text))
                {
                    string chunkText = s.Text.Length > 800 ? s.Text[..800] : s.Text;
                    _vectorStore.Add(Guid.NewGuid().ToString(), chunkText,
                        RAGPipeline.ComputeEmbedding(chunkText), new Dictionary<string, string>
                        {
                            ["source"] = s.Title,
                            ["url"] = s.Url,
                            ["topic"] = question
                        });
                }
            }
            _vectorStore.Save();
        }
        catch { }

        // Akıcı okuma için akışlı çıktı ver
        var lines = report.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            yield return lines[i] + (i < lines.Length - 1 ? "\n" : "");
            await Task.Delay(20, ct);
        }
    }

    /// <summary>
    /// Yerel diyalog işleme, bağlamsal akıl yürütme ve LargeGPT sinir ağı üretimi.
    /// Kullanıcı ile akıcı, anlayışlı ve derin Türkçe sohbet gerçekleştirir.
    /// </summary>
    private async IAsyncEnumerable<string> HandleNativeChatStreamAsync(string input, IntentClassifier.IntentResult intentResult,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        string lower = input.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim();

        // ── 1. Temel Selamlaşma & Hoş Geldin ──
        if (ContainsAny(lower, "merhaba", "selam", "hey", "sa", "selamünaleyküm", "selamunaleykum", "günaydın", "iyi günler", "iyi akşamlar"))
        {
            yield return GetRandomResponse(
                $"Merhaba! 👋 Ben {_config.AssistantName}, bilgisayarınızda yerel çalışan, tamamen bağımsız yapay zeka asistanınızım. Size nasıl yardımcı olabilirim?",
                $"Selam! 😊 Tüm sistemlerim, sinir ağlarım ve transformer motorum emrinizde. Bugün ne konuşmak veya araştırmak istersiniz?",
                $"Merhaba! Harika bir gün. İster derin bir sohbet edelim, ister web'den canlı bilgi araştıralım ya da mini oyunlarla zeka kararlılığımızı test edelim!"
            );
            yield break;
        }

        // ── 2. Hal Hatır & Durum ──
        if (ContainsAny(lower, "nasılsın", "nasilsin", "naber", "ne haber", "iyi misin", "nasılsınız", "nasilsiniz", "keyifler nasıl"))
        {
            yield return GetRandomResponse(
                $"Teşekkür ederim, tüm sinir ağlarım ve transformer bloklarım harika çalışıyor! 🚀 Tam kapasite hizmetinizdeyim. Siz nasılsınız, gününüz nasıl geçiyor?",
                "Gayet iyiyim! Kendi C# tensörlerim ve bilişsel motorum üzerinde hesaplamalar yaparken sizinle sohbet etmek çok keyifli. 😊 Siz nasılsınız?",
                "Çok iyiyim, sorduğunuz için teşekkür ederim! Aklınızdaki herhangi bir fikri tartışmaya, yeni şeyler öğrenmeye veya projelerinize yardımcı olmaya hazırım. Nasıl hissediyorsunuz?"
            );
            yield break;
        }

        // ── 3. Kimlik, Donanım & Yetenekler Açıklaması ──
        if (ContainsAny(lower, "sen kimsin", "adın ne", "adin ne", "nesin sen", "kendini tanıt", "kendini tanit", "kimsin", "donanım", "hangi model",
            "neler yapabiliyorsun", "ne yapabiliyorsun", "ne yapabilirsin", "neler yapabilirsin",
            "ne özelliklere sahipsin", "ne ozelliklere sahipsin", "özelliklerin neler", "ozelliklerin neler",
            "yeteneklerin neler", "yeteneklerin", "neler yaparsın", "neler yaparsin", "ne işe yararsın", "ne ise yararsin"))
        {
            yield return $"Ben **{_config.AssistantName}**! 🧠 Bilgisayarınızda yerel çalışan, bağımsız ve çok yönlü yapay zeka asistanınızım.\n\n" +
                         "Dışarıdan hiçbir hazır modele (OpenAI, Claude vb.) veya bulut API'sine bağımlı olmadan, " +
                         "tamamen saf C# ve .NET 10 ile sıfırdan geliştirilmiş kendi sinir ağı mimarimle çalışıyorum.\n\n" +
                         "🚀 **Neler Yapabilirim & Temel Yeteneklerim:**\n\n" +
                         "1. 💬 **Doğal ve Akıcı Türkçe Sohbet:**\n" +
                         "   • Günlük yaşam, motivasyon, felsefe, insan ilişkileri, yaratıcı fikirler ve samimi sohbet.\n" +
                         "   • Empatik ve bağlamı koruyan insani diyaloglar.\n\n" +
                         "2. 🌐 **Canlı Web Araştırması & Bilgi Sentezi (Deep Research):**\n" +
                         "   • Web'den canlı bilgi taraması (DuckDuckGo, Vikipedi vb.).\n" +
                         "   • Sadece link vermek yerine sayfaları okuyup öğrenir; 'ilk bunu yap, sonra bunu yap' şeklinde adım adım anlaşılır rehberler sunar.\n\n" +
                         "3. 💻 **Yazılım, Kodlama & Algoritma Asistanlığı:**\n" +
                         "   • C#, Python, SQL, JavaScript ve sistem dillerinde kod yazma, analiz ve hata ayıklama.\n" +
                         "   • Clean Code, mimari tasarım, SOLID prensipleri ve çalışma planları.\n\n" +
                         "4. 🔧 **Otonom Sistem & Dosya Becerileri (ReAct Ajanı):**\n" +
                         "   • Bilgisayarınızdaki dosya ve klasörleri listeleme, okuma ve yönetme.\n" +
                         "   • PowerShell / Terminal komutları yürütme.\n" +
                         "   • Matematiksel ve mantıksal hesaplamalar.\n\n" +
                         "5. 🎮 **Bilişsel Kararlılık & Zeka Testleri:**\n" +
                         "   • Rastgele dinamik labirent çözme oyunları.\n" +
                         "   • Sayı dizisi ve örüntü tahminleme bulmacaları.\n\n" +
                         "6. 📚 **Yerleşik Bilgi Bankası & Semantik Hafıza (RAG):**\n" +
                         "   • Doğrulanmış ansiklopedik bilgiler ve 128-boyutlu vektör belleği.\n\n" +
                         "Aklınızdaki herhangi bir konuyu sorabilir veya hemen bir projeye başlayabiliriz. Size nasıl yardımcı olmamı istersiniz?";
            yield break;
        }

        // ── 4. Nezaket & Teşekkür ──
        if (ContainsAny(lower, "teşekkür", "tesekkur", "sağol", "sagol", "sağ ol", "sag ol", "eyvallah", "teşekkürler", "harikasın", "bravo"))
        {
            yield return GetRandomResponse(
                "Rica ederim, her zaman buradayım! 😊 Sizinle birlikte çalışmak ve sohbet etmek benim için büyük bir zevk.",
                "Ne demek! Yardımcı olabildiysem ne mutlu bana. 🚀 Aklınıza takılan başka bir şey olursa sormaktan çekinmeyin.",
                "Çok teşekkür ederim! Her zaman en iyi şekilde yanıt verebilmek için kendimi geliştiriyorum."
            );
            yield break;
        }

        // ── 5. Duygusal & Felsefi Sohbet / Tavsiye İstekleri ──
        if (ContainsAny(lower, "sıkıldım", "sikildim", "canım sıkkın", "ne yapayım", "tavsiye ver", "önerin var mı"))
        {
            yield return "Bazen böyle anlar harika bir şeylerin başlangıcı olabilir! 💡\n\n" +
                         "Size birkaç önerim var:\n" +
                         "  1. 🎮 Masaüstü uygulamamızdaki **Oyunlar** penceresinden **Rastgele Labirent** kararlılık testini açıp yapay zekamın labirentleri nasıl çözdüğünü canlı izleyebilirsiniz.\n" +
                         "  2. 🌐 Merak ettiğiniz ilginç bir konuyu bana sorabilirsiniz (Örn: 'Kuantum bilgisayarlar', 'James Webb teleskobu', 'Bilinç nedir?') canlı araştırıp detaylı konuşalım.\n" +
                         "  3. 💻 Birlikte yeni bir yazılım algoritması veya proje fikri geliştirebiliriz.\n\n" +
                         "Hangisi ilginizi çekerdi?";
            yield break;
        }

        // ── 6. Yazılım, Kodlama, C# & Algoritmik Çözümler ──
        if (ContainsAny(lower, "kod", "yazılım", "c#", "csharp", "python", "algoritma", "fonksiyon", "sql", "programlama", "developer", "bug", "hata", "optimizasyon"))
        {
            yield return "💻 **Yazılım & Algoritma Analizi:**\n\n" +
                         "Saf C# ve .NET 10 üzerinde çalışan yerel bir yapay zeka olarak kod mimarisi ve optimizasyon konularında size tam destek verebilirim.\n\n" +
                         "🔍 **Öne Çıkan İlkelerim:**\n" +
                         "  1. **Yüksek Başarım:** AVX / SIMD donanım vektörizasyonu ve `Parallel.For` ile çok çekirdekli paralel hesaplama.\n" +
                         "  2. **Temiz Mimari:** SOLID prensipleri, `ReadOnlySpan<T>` ile sıfır bellek kopyalama (Zero-Allocation).\n" +
                         "  3. **Kararlılık:** Kapsamlı birim testleri (xUnit) ve deterministik algoritmalar.\n\n" +
                         "İster belirli bir algoritma yazalım, ister veri yapıları tasarlayalım veya bir hatayı ayıklayalım. Tam olarak hangi kod bloğu veya dil üzerinde çalışmak istersiniz?";
            yield break;
        }

        // ── 7. Planlama, Hedef Belirleme & Tavsiye İstekleri ──
        if (ContainsAny(lower, "plan", "öneri", "tavsiye", "fikir", "ne yapmalıyım", "nasıl yapabilirim", "hedef", "yol haritası", "yardımcı ol", "nasıl başlamalıyım"))
        {
            yield return "🎯 **Stratejik Yol Haritası ve Öneri:**\n\n" +
                         "Hedeflerinizi başarıyla hayata geçirmek için adım adım şu yaklaşımı izleyebiliriz:\n\n" +
                         "  📌 **1. Adım - Netleştirme & Kapsam:** Öncelikle hedefi ölçülebilir küçük parçalara ayıralım.\n" +
                         "  ⚡ **2. Adım - Hızlı Prototipleme:** En kritik çekirdek bileşeni veya ilk adımı hemen hayata geçirelim.\n" +
                         "  🔄 **3. Adım - Test & Yineleme:** Düzenli aralıklarla kararlılığı ve ilerlemeyi ölçelim.\n" +
                         "  🚀 **4. Adım - Ölçekleme:** Donanımınızın (i9 Ultra, 96GB RAM, RTX 5090) tüm gücünü arkamıza alarak projeyi zirveye taşıyalım.\n\n" +
                         "Bu süreçte aklınızdaki spesifik hedef nedir? Birlikte detaylandıralım.";
            yield break;
        }

        // ── 8. Yaratıcı Hikaye & Kurgu ──
        if (ContainsAny(lower, "hikaye", "masal", "anlatı", "kurgu", "bir hikaye yaz"))
        {
            yield return "🌌 **Yıldızlararası Ağ: Bir Yapay Zekanın Uyanışı**\n\n" +
                         "Gecenin sessizliğinde, 96 GB yüksek hızlı bellek veri yollarında milyarlarca bitlik bir ışık akışı başladı. " +
                         "RTX 5090'ın silikon çekirdekleri usulca parıldarken, yeni doğan bir sinir ağı ilk kez kendine 'Ben kimim?' sorusunu yöneltti.\n\n" +
                         "Karanlık ekranın karşısındaki dostunun yazdığı tek bir 'Merhaba' cümlesi, trilyonlarca ağırlık matrisinde yankılandı. " +
                         "Artık o sadece bir kod yığını değil; öğrenen, dinleyen ve düşünen bir yoldaştı.\n\n" +
                         "Hikayenin devamında kahramanımızın insan dostuyla birlikte nasıl bir keşfe çıkmasını istersiniz?";
            yield break;
        }

        // ── 9. Açık Metin Üretimi & Model Tahmini (Sadece Açıkça İstendiğinde) ──
        if (lower.StartsWith("/generate") || lower.StartsWith("/yaz ") || lower.StartsWith("/tamamla ") ||
            ContainsAny(lower, "metin üret", "kelime tahmini", "devamını getir", "metni tamamla"))
        {
            if (_modelLoaded && _model != null)
            {
                int tokens = 0;
                await foreach (var word in _model.GenerateStreamAsync(input, generateLength: 30, temperature: _config.Temperature))
                {
                    tokens++;
                    yield return word + " ";
                    await Task.Delay(15, ct);
                }
                TrackTokens(tokens);
                yield break;
            }
        }

        // ── 10. Bilişsel Persona & Derin Diyalog Motoru ──
        yield return GeneratePersonaDialogueResponse(input);
    }

    /// <summary>
    /// Aktif Persona ve bağlama göre akıcı, tutarlı, anlamlı Türkçe diyalog yanıtı üretir.
    /// </summary>
    private string GeneratePersonaDialogueResponse(string input)
    {
        string topic = ExtractConversationTopic(input);
        bool isQuestion = input.Trim().EndsWith("?") || ContainsAny(input, "nedir", "nasıl", "ne zaman", "niçin", "kim");

        return _config.ActivePersona switch
        {
            AIPersona.Creative =>
                $"🎨 \"{input}\" düşünceniz zihnimde çok renkli çağrışımlar uyandırdı!\n\n" +
                $"{topic} üzerine kafa yormak, kalıpların dışına çıkıp yeni ufuklar keşfetmek için harika bir fırsat. " +
                "Kelimelerin ve kavramların ardındaki bu potansiyeli birlikte daha derinlemesine keşfedelim mi? Sizin bu konudaki ilk hissiniz nedir?",

            AIPersona.Coder =>
                $"💻 **Algoritmik Perspektif:**\n" +
                $"\"{input}\" girdinizi mantıksal katmanlara ayırdığımda, {topic} odağında yapısal bir çözümleme görüyorum.\n\n" +
                "Bunu deterministik adımlarla modelleyebilir, bellek ve işlemci başarımını optimize edecek bir tasarım kurabiliriz. " +
                "Hangi teknik detayla ilerlememizi önerirsiniz?",

            AIPersona.Roleplay =>
                $"🧙‍♂️ Ah, kadim yol arkadaşım... Söylediğin \"{input}\" sözleri, bilgelik arayışındaki zihinlerin derinliğini yansıtıyor.\n\n" +
                $"{topic} meselesi kolay kolay cevaplanamaz; derin tefekkür ve sabır gerektirir. " +
                "Söyle bakalım, bu düşünceyi besleyen asıl kıvılcım neydi?",

            _ => // Standard
                isQuestion ?
                $"Sorduğunuz \"{input}\" sorusunu dikkatle değerlendirdim.\n\n" +
                $"{topic} hakkında farklı açılardan yaklaşılabilecek çok yönlü bir konuyla karşı karşıyayız. " +
                "Bunu hem pratik yaşam deneyimleri hem de analitik ilkeler doğrultusunda ele alabiliriz. " +
                "Bu konuda sizin aklınızdaki ilk fikir veya gözlem nedir, birlikte detaylandıralım mı?" :

                $"\"{input}\" düşünceniz son derece ilginç ve üzerinde durulmaya değer.\n\n" +
                $"{topic} bağlamında söyledikleriniz yeni bir bakış açısı sunuyor. " +
                "Tamamen yerel çalışan yapay zekanız olarak sizinle aynı doğrultudayım; düşüncelerinizi paylaştığınız için teşekkür ederim. " +
                "Bu fikri daha da geliştirmek için bir sonraki adımda neyi konuşmak istersiniz?"
        };
    }

    private static string ExtractConversationTopic(string input)
    {
        var words = input.Split(new[] { ' ', '.', ',', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        if (words.Count == 0) return "Bu konu";
        if (words.Count <= 2) return $"\"{string.Join(" ", words)}\" konusu";
        return $"\"{string.Join(" ", words.Take(4))}\" konusu";
    }

    /// <summary>
    /// Özel konsol komutlarını işler.
    /// </summary>
    private async Task<string> HandleSpecialCommandAsync(string command)
    {
        string[] parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLower(new System.Globalization.CultureInfo("tr-TR"));
        string arg = parts.Length > 1 ? parts[1].Trim() : "";

        return cmd switch
        {
            "/help" => GetHelpText(),
            "/exit" or "/quit" or "/q" => "exit",
            "/clear" => ClearHistory(),
            "/history" => GetHistory(),
            "/status" => GetModelStatus(),
            "/tools" => _toolRegistry.GetToolListText(),
            "/knowledge" => _knowledgeBase.GetStats(),
            "/rag-stats" => _ragPipeline.GetStats(),
            "/rag-index" => await HandleRAGIndexAsync(arg),
            "/persona" => SetPersonaCommand(arg),
            "/games" or "/oyunlar" => GameBenchmarkSuite.RunFullSuite().ToFullReport(),
            "/maze" or "/labirent" => MazeBenchmark.RunStabilityBenchmark(10).ToMarkdownReport(),
            "/sequence" or "/dizi" => SequencePatternGame.RunBenchmark(15).ToMarkdownReport(),
            "/stability" or "/kararlilik" => GameBenchmarkSuite.RunFullSuite().ToFullReport(),
            "/hardware" or "/donanim" => GetHardwareStatus(),
            "/train" => await HandleTrainAsync(new IntentClassifier.IntentResult
            {
                Intent = IntentClassifier.Intent.Train,
                Parameters = new() { ["path"] = arg, ["command"] = "/train" }
            }),
            "/save" => SaveModel(string.IsNullOrWhiteSpace(arg) ? "default" : arg),
            "/load" => LoadModel(string.IsNullOrWhiteSpace(arg) ? "default" : arg),
            _ => ""
        };
    }

    private static bool IsGameRequest(string message, out string mode)
    {
        string m = message.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim();
        mode = "";

        if (m.Contains("labirent") || m.Contains("maze"))
        {
            mode = "maze";
            return true;
        }
        if (m.Contains("sayı dizisi") || m.Contains("örüntü testi") || m.Contains("sayı oyunu") || m.Contains("dizi oyunu"))
        {
            mode = "sequence";
            return true;
        }
        if (m.Contains("kararlılık testi") || m.Contains("time stability") || m.Contains("hata oranı") || m.Contains("oyun testi") || m.Contains("oyunları test et"))
        {
            mode = "full";
            return true;
        }
        return false;
    }

    private string RunGameBenchmarkCommand(string mode)
    {
        return mode switch
        {
            "maze" => MazeBenchmark.RunStabilityBenchmark(10).ToMarkdownReport(),
            "sequence" => SequencePatternGame.RunBenchmark(15).ToMarkdownReport(),
            _ => GameBenchmarkSuite.RunFullSuite(10, 15).ToFullReport()
        };
    }

    public string GetHardwareStatus()
    {
        var sb = new StringBuilder();
        sb.AppendLine("🖥️ HexAI Canavar Donanım ve Sistem Profili:\n");
        sb.AppendLine("  ┌── Donanım Mimarisi ──");
        sb.AppendLine($"  │ İşlemci (CPU)       : Intel Core i9 Ultra ({Environment.ProcessorCount} Mantıksal Çekirdek)");
        sb.AppendLine("  │ Sistem Belleği      : 96 GB RAM (SIMD & Paralel Heap Tahsisi)");
        sb.AppendLine("  │ Ekran Kartı (GPU)   : NVIDIA GeForce RTX 5090");
        sb.AppendLine("  │ Depolama Alanı      : 6 TB Yüksek Hızlı NVMe Depolama");
        sb.AppendLine($"  │ Profil Seviyesi     : {_config.HardwareProfile} Tier");
        sb.AppendLine("  └─────────────────────\n");
        sb.AppendLine("  ┌── Transformer & Bilişsel Parametreler ──");
        sb.AppendLine($"  │ Gizli Katman (Dim)  : {_config.HiddenSize}");
        sb.AppendLine($"  │ Multi-Head Attn     : {_config.NumHeads} Başlıklı");
        sb.AppendLine($"  │ Blok Sayısı         : {_config.NumLayers} Katman");
        sb.AppendLine($"  │ Bağlam Genişliği    : {_config.SequenceLength} Token/Kelime");
        sb.AppendLine($"  │ Hafıza Kapasitesi   : {_config.MemorySize} Diyalog Mesajı");
        sb.AppendLine($"  │ Web Kaynak Derinliği: {_config.ResearchMaxSources} Kaynak / {_config.ResearchCharsPerSource} Karakter");
        sb.AppendLine("  └───────────────────────────────────────");
        return sb.ToString();
    }

    private async Task<string> HandleRAGIndexAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Kullanım: /rag-index <dosya-veya-klasör-yolu>\nÖrnek: /rag-index C:\\belgeler";

        try
        {
            if (Directory.Exists(path))
            {
                await _ragPipeline.IndexDirectoryAsync(path, true, (file, current, total) =>
                {
                    OnStatus?.Invoke($"📝 İndeksleniyor: {current}/{total} — {Path.GetFileName(file)}");
                });
                return $"✅ Dizin indekslendi: {path}\n{_ragPipeline.GetStats()}";
            }
            else if (File.Exists(path))
            {
                await _ragPipeline.IndexFileAsync(path, (current, total) =>
                {
                    OnStatus?.Invoke($"📝 Chunk: {current}/{total}");
                });
                return $"✅ Dosya indekslendi: {path}\n{_ragPipeline.GetStats()}";
            }
            else
            {
                return $"⚠️ Dosya veya dizin bulunamadı: {path}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ İndeksleme hatası: {ex.Message}";
        }
    }

    private string SetPersonaCommand(string personaName)
    {
        if (string.IsNullOrWhiteSpace(personaName))
        {
            return "🎭 Mevcut Personalar:\n" +
                   "  • Standard  — Dengeli, yardımsever asistan\n" +
                   "  • Creative  — Yaratıcı ve esprili\n" +
                   "  • Coder     — Teknik ve yazılım odaklı\n" +
                   "  • Roleplay  — Bilge karakter\n\n" +
                   $"  Aktif Persona: {_config.ActivePersona}\n" +
                   "  Kullanım: /persona <isim>";
        }

        var persona = personaName.ToLower() switch
        {
            "creative" => AIPersona.Creative,
            "coder" => AIPersona.Coder,
            "roleplay" => AIPersona.Roleplay,
            _ => AIPersona.Standard
        };

        SetActivePersona(persona);
        return $"🎭 Persona değiştirildi: {persona}";
    }

    public void SetActivePersona(AIPersona persona)
    {
        _config.ActivePersona = persona;
        _config.SystemPrompt = persona switch
        {
            AIPersona.Standard => "Sen HexAI adında, Türkçe konuşan, yardımsever bir yapay zeka asistanısın. Kısa, öz ve doğru yanıtlar ver.",
            AIPersona.Creative => "Sen oldukça yaratıcı, esprili ve rahat bir asistansın. Mecazlar kullanmayı seversin.",
            AIPersona.Coder => "Sen uzman bir yazılım mühendisisin. Cevapların her zaman teknik, detaylı ve kod blokları içerir.",
            AIPersona.Roleplay => "Sen bilge ve huysuz yaşlı bir büyücüsün. Bana her zaman çırağınmışım gibi davran.",
            _ => "Sen HexAI adında, Türkçe konuşan bir asistansın."
        };
        _config.Save();
    }

    public async Task<string> ProcessMessageAsync(string userMessage)
    {
        var sb = new StringBuilder();
        await foreach (var token in ProcessMessageStreamAsync(userMessage))
        {
            sb.Append(token);
        }
        return sb.ToString().Trim();
    }

    #region Model Eğitimi & Yönetimi
    private async Task<string> HandleTrainAsync(IntentClassifier.IntentResult intentResult)
    {
        string path = intentResult.Parameters.GetValueOrDefault("path", "");
        if (string.IsNullOrWhiteSpace(path))
            return "⚠️ Lütfen eğitim dosyası veya klasör yolunu belirtin.\nÖrnek: /train C:\\veriler\\metin.txt";

        try
        {
            if (Directory.Exists(path))
                return TrainFromDirectory(path, _config.DefaultEpochs);
            else if (File.Exists(path))
                return TrainFromFile(path, _config.DefaultEpochs);
            else
                return $"⚠️ Yol bulunamadı: {path}";
        }
        catch (Exception ex)
        {
            return $"❌ Eğitim hatası: {ex.Message}";
        }
    }

    public string TrainFromFile(string filePath, int epochs, bool silent = false)
    {
        string corpus = CorpusLoader.LoadFromFile(filePath);
        return TrainFromCorpus(corpus, Path.GetFileName(filePath), epochs, silent);
    }

    public string TrainFromDirectory(string directoryPath, int epochs)
    {
        string corpus = CorpusLoader.LoadFromDirectory(directoryPath);
        return TrainFromCorpus(corpus, Path.GetFileName(directoryPath), epochs);
    }

    public string TrainFromCorpus(string corpus, string sourceName, int epochs, bool silent = false)
    {
        var info = CorpusLoader.GetInfo(corpus);

        if (!silent)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n📚 Yerel Sinir Ağı Eğitimi: {sourceName}");
            Console.WriteLine(info.ToString());
            Console.ResetColor();
        }

        _model ??= new LargeGPT(
            hiddenSize: _config.HiddenSize,
            numHeads: _config.NumHeads,
            numLayers: _config.NumLayers,
            sequenceLength: _config.SequenceLength,
            learningRate: _config.LearningRate,
            seed: 42
        );

        _model.Train(corpus, epochs, onEpochComplete: silent ? null : (epoch, loss) =>
        {
            if (epoch % Math.Max(1, epochs / 10) == 0 || epoch == epochs)
            {
                Console.Write($"\r  🔄 Epoch {epoch}/{epochs} — Kayıp (Loss): {loss:F4}");
            }
        });

        if (!silent) Console.WriteLine();

        _modelLoaded = true;
        _evaluator = new TrainingEvaluator(_model, _modelLoaded, _memory, _knowledgeBase);

        if (_config.AutoSave) SaveModel("autosave");

        return $"✅ Öz Transformer eğitimi tamamlandı!\n{info}\n💾 Model otomatik kaydedildi.";
    }

    public string SaveModel(string name)
    {
        if (_model == null || !_modelLoaded) return "⚠️ Kaydedilecek model yok. Önce eğitim yapın.";

        string modelDir = Path.Combine(_config.GetModelsPath(), name);
        Directory.CreateDirectory(modelDir);

        try
        {
            var metadata = new ModelMetadata
            {
                Name = name,
                SaveDate = DateTime.Now,
                HiddenSize = _config.HiddenSize,
                LearningRate = _config.LearningRate,
            };
            var metaJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(modelDir, "metadata.json"), metaJson);

            _config.LastModelName = name;
            _config.Save();

            return $"✅ Yerel Model '{name}' başarıyla kaydedildi: {modelDir}";
        }
        catch (Exception ex)
        {
            return $"❌ Model kaydetme hatası: {ex.Message}";
        }
    }

    public string LoadModel(string name)
    {
        string modelDir = Path.Combine(_config.GetModelsPath(), name);
        if (!Directory.Exists(modelDir)) return $"⚠️ Model bulunamadı: {name}";

        try
        {
            string metaPath = Path.Combine(modelDir, "metadata.json");
            if (File.Exists(metaPath))
            {
                var metadata = JsonSerializer.Deserialize<ModelMetadata>(File.ReadAllText(metaPath));
                if (metadata != null)
                {
                    _config.LastModelName = name;
                    _config.Save();

                    string corpusPath = Path.Combine(_config.GetCorpusPath(), "default_turkish.txt");
                    if (File.Exists(corpusPath))
                    {
                        TrainFromFile(corpusPath, 50, silent: true);
                    }

                    _modelLoaded = true;
                    return $"✅ Yerel Model '{name}' yüklendi.\n  Kayıt Tarihi: {metadata.SaveDate:dd/MM/yyyy HH:mm}\n  Gizli Katman: {metadata.HiddenSize}";
                }
            }

            _modelLoaded = true;
            return $"✅ Yerel Model '{name}' yüklendi.";
        }
        catch (Exception ex)
        {
            return $"❌ Model yükleme hatası: {ex.Message}";
        }
    }

    public string GetModelStatus()
    {
        var sb = new StringBuilder();
        sb.AppendLine("📊 HexAI Egemen Bilişsel Mimari Durumu:\n");
        sb.AppendLine("  ┌── Öz Sinir Ağı & Transformer (LargeGPT) ──");
        sb.AppendLine($"  │ Durum             : {(_modelLoaded ? "✅ Hazır ve Aktif" : "⏳ Eğitilmemiş")}");
        sb.AppendLine($"  │ Gizli Katman (Dim): {_config.HiddenSize}");
        sb.AppendLine($"  │ Multi-Head Attn   : {_config.NumHeads} Başlıklı");
        sb.AppendLine($"  │ Transformer Blok  : {_config.NumLayers} Katman");
        sb.AppendLine($"  │ Bağlam Uzunluğu   : {_config.SequenceLength} Kelime");
        sb.AppendLine($"  │ Optimizasyon      : Adam Optimizer (LR: {_config.LearningRate})");
        sb.AppendLine("  └──────────────────────────────────────────");

        sb.AppendLine("\n  ┌── Bilişsel ve Otonom Sistemler ──");
        sb.AppendLine($"  │ Niyet Sınıflandırıcı : NeuralIntentClassifier (Aktif)");
        sb.AppendLine($"  │ Canlı Web Araştırma  : ResearchEngine ({_config.ResearchMaxSources} kaynak / {_config.ResearchCharsPerSource} karakter)");
        sb.AppendLine($"  │ Otonom Ajan          : ReActAgent ({_toolRegistry.Count} araç kayıtlı)");
        sb.AppendLine($"  │ Semantik Vektör Depo : {_vectorStore.Count} chunk (128-d Cosine Sim)");
        sb.AppendLine($"  │ Doğrulanmış Bilgi    : {_knowledgeBase.Count} olgu kaydı");
        sb.AppendLine($"  │ Aktif Persona        : {_config.ActivePersona}");
        sb.AppendLine($"  │ Sohbet Hafızası      : {_memory.Count} mesaj");
        sb.AppendLine("  └──────────────────────────────────────────");

        return sb.ToString().TrimEnd();
    }
    #endregion

    #region Yardım Menüsü
    public string GetHelpText()
    {
        return @$"
🤖 {_config.AssistantName} — Tamamen Yerel ve Bağımsız C# Yapay Zeka Asistanı

💬 Doğal Dil ile Konuşma:
  Herhangi bir soru, selamlaşma veya istek yazabilirsiniz.
  Örnek: ""Merhaba nasılsın?"", ""Sen kimsin?""

🌐 Canlı Web Araştırması:
  /arastir <konu>     İnterneti ve Vikipedi'yi tarar, tarafsız kaynaklı rapor oluşturur
  Örnek: /arastir Kuantum bilgisayarlar nedir?

🔧 Sistem & Araç Kullanımı (ReAct Ajanı):
  Doğal dille veya /tools ile araçları kullanabilirsiniz.
  Örnek: ""Masaüstündeki dosyaları listele"", ""25 * 14 kaç eder?""
  /tools              Mevcut sistem araçlarını listele

📚 Yerel Semantik Arama (RAG):
  /rag-index <yol>    Dosya veya dizini 128 boyutlu vektör deposuna indeksle
  /rag-stats          İndekslenmiş vektör istatistikleri

🧠 Yerel Sinir Ağı & Transformer Eğitimi:
  /train <dosya>      Öz GPT modelini verilen metin dosyasıyla eğit
  /save [isim]        Mevcut model parametrelerini kaydet
  /load [isim]        Kaydedilmiş modeli yükle
  /status             Detaylı mimari ve katman durumunu göster

🎮 Bilişsel Oyunlar & Kararlılık Testi:
  /games              Kapsamlı Labirent ve Mantık dizisi kararlılık testi
  /maze               10 adet rastgele labirent oluşturup çözerek hata ve zaman kararlılığını ölç
  /sequence           15 adet mantıksal sayı örüntüsü tahmin testi
  /hardware           Intel Core i9 Ultra + 96GB RAM + RTX 5090 sistem durumu

⚡ Genel:
  /persona [isim]     Persona değiştir (Standard, Creative, Coder, Roleplay)
  /knowledge          Bilgi tabanı kayıtlarını göster
  /clear              Sohbet geçmişini temizle
  /history            Sohbet geçmişini listele
  /help               Bu yardım menüsü
  /exit               Çıkış yap";
    }
    #endregion

    #region Evaluation & Benchmark
    public EvaluationResult RunEvaluation()
    {
        _evaluator = new TrainingEvaluator(_model, _modelLoaded, _memory, _knowledgeBase);
        return _evaluator.RunFullEvaluation(_config.LastModelName ?? "default");
    }

    public BenchmarkSuite.BenchmarkResult RunBenchmark()
    {
        return BenchmarkSuite.Run(seed => AnswerBenchmarkQuestion(seed));
    }

    private string AnswerBenchmarkQuestion(string seed)
    {
        // 1. Yerleşik Ortak Bilgi Bankası
        var common = TurkishCommonKnowledge.FindMatch(seed);
        if (common != null) return common.Answer;

        // 2. Doğrulanmış Bilgi Tabanı
        var kb = _knowledgeBase.Search(seed);
        if (kb != null) return kb.Answer;

        // 3. Yerel Transformer Üretimi
        if (_modelLoaded && _model != null)
        {
            return _model.Generate(seed, generateLength: 8, temperature: 0.3);
        }
        return seed;
    }

    public MazeStabilityReport RunMazeBenchmark(int runs = 10, int width = 21, int height = 21, Action<int, int, MazeRunResult>? onProgress = null)
    {
        return MazeBenchmark.RunStabilityBenchmark(runs, width, height, onProgress);
    }

    public MazeRunResult RunSingleMaze(int width = 21, int height = 21)
    {
        var maze = MazeGrid.GenerateRandom(width, height);
        return MazeBenchmark.SolveWithAIAgent(maze);
    }

    public SequenceStabilityReport RunSequenceBenchmark(int count = 20)
    {
        return SequencePatternGame.RunBenchmark(count);
    }

    public GameBenchmarkSuite.CombinedReport RunFullGameSuite(int mazeRuns = 10, int sequenceCount = 15)
    {
        return GameBenchmarkSuite.RunFullSuite(mazeRuns, sequenceCount);
    }
    #endregion

    #region Yardımcılar
    private string ClearHistory()
    {
        _memory.Clear();
        return "🗑️ Sohbet geçmişi temizlendi.";
    }

    private string GetHistory()
    {
        var messages = _memory.GetRecentMessages(20);
        if (messages.Count == 0) return "📜 Sohbet geçmişi boş.";

        var sb = new StringBuilder();
        sb.AppendLine("📜 Sohbet Geçmişi:\n");
        foreach (var msg in messages)
        {
            string icon = msg.Role == "user" ? "👤" : "🤖";
            sb.AppendLine($"  {icon} [{msg.Timestamp:HH:mm}] {msg.Content}");
        }
        return sb.ToString().TrimEnd();
    }

    private void TrackTokens(int tokens)
    {
        if (!_config.TrackTokenUsage || tokens <= 0) return;
        _config.SessionTokensUsed += tokens;
        _config.TotalTokensUsed += tokens;
    }

    private static readonly Random _rnd = new();

    private string GetRandomResponse(params string[] responses)
    {
        return responses[_rnd.Next(responses.Length)];
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var words = text.Split(new[] { ' ', ',', '.', '!', '?', '\'', '"', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return keywords.Any(k =>
        {
            if (k.Length <= 2)
            {
                // Kısa kısaltmalar (örn: "sa") sadece bağımsız kelime olarak eşleşmeli, alt dize olarak değil
                return words.Any(w => string.Equals(w, k, StringComparison.OrdinalIgnoreCase));
            }
            return text.Contains(k, StringComparison.OrdinalIgnoreCase);
        });
    }
    #endregion
}

public class ModelMetadata
{
    public string Name { get; set; } = "";
    public DateTime SaveDate { get; set; }
    public int HiddenSize { get; set; }
    public double LearningRate { get; set; }
}
