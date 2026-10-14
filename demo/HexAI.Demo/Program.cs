using System;
using HexAI.Core.Activation;
using HexAI.Core.Data;
using HexAI.Core.Math;
using HexAI.NeuralNetwork;
using HexAI.NeuralNetwork.Layers;
using HexAI.NLP;
using HexAI.NLP.Generative;

namespace HexAI.Demo;

/// <summary>
/// HEX.AI Transformer & Core Math Odaklı Sadeleştirilmiş Demo Programı.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintBanner();

        // Demo 1: Matris İşlemleri ve SIMD Hızlandırması
        DemoMatrisIslemleri();

        // Demo 2: Aktivasyon Fonksiyonları
        DemoAktivasyonFonksiyonlari();

        // Demo 3: XOR Sinir Ağı (Feed-Forward Ağı)
        DemoXorSinirAgi();

        // Demo 4: Transformer & Mini-GPT Kelime Tabanlı Metin Üretimi
        DemoRNN();

        // Demo 5: Multi-Agent Pipeline Sistemi
        DemoMultiAgent();

        // Demo 6: LLM & Assistant CLI üzerinden test edilebilir
        // DemoVerificationPipeline();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  🎉 Tüm odaklanmış Transformer ve Core demoları başarıyla tamamlandı!");
        Console.ResetColor();
        Console.ReadLine();
    }

    static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
██╗  ██╗███████╗██╗  ██╗     █████╗ ██╗
██║  ██║██╔════╝╚██╗██╔╝    ██╔══██╗██║
███████║█████╗   ╚███╔╝     ███████║██║
██╔══██║██╔══╝   ██╔██╗     ██╔══██║██║
██║  ██║███████╗██╔╝ ██╗    ██║  ██║██║
╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝    ╚═╝  ╚═╝╚═╝");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("=================================================");
        Console.WriteLine("    Transformer & GPT Çekirdek Yapay Zeka Motoru");
        Console.WriteLine("=================================================");
        Console.ResetColor();
    }

    static void PrintMatrix(Matrix m)
    {
        for (int r = 0; r < m.Rows; r++)
        {
            Console.Write("    [ ");
            for (int c = 0; c < m.Cols; c++)
            {
                Console.Write($"{m[r, c],6:F2} ");
            }
            Console.WriteLine("]");
        }
    }

    static void DemoMatrisIslemleri()
    {
        PrintSectionHeader("🧮 Demo 1: Matris İşlemleri ve SIMD Hızlandırması");

        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var b = new Matrix(new double[,] { { 5, 6 }, { 7, 8 } });

        Console.WriteLine("  A Matrisi:");
        PrintMatrix(a);
        Console.WriteLine("  B Matrisi:");
        PrintMatrix(b);

        Console.WriteLine("  1. Matris Toplama (SIMD):");
        PrintMatrix(a + b);

        Console.WriteLine("  2. Matris Çıkarma (SIMD):");
        PrintMatrix(b - a);

        Console.WriteLine("  3. Matris Çarpımı (Parallel + SIMD):");
        PrintMatrix(a * b);

        Console.WriteLine("  4. Skaler Çarpım (A * 2.5):");
        PrintMatrix(a * 2.5);

        Console.WriteLine("  5. Hadamard Çarpımı (A ⊙ B):");
        PrintMatrix(a.HadamardProduct(b));

        PrintSectionFooter();
    }

    static void DemoAktivasyonFonksiyonlari()
    {
        PrintSectionHeader("⚡ Demo 2: Aktivasyon Fonksiyonları");

        double[] inputs = { -2.0, -0.5, 0.0, 1.5, 3.0 };

        var activations = new IActivation[]
        {
            new Sigmoid(),
            new TanhActivation(),
            new ReLU(),
            new Softmax(),
            new Linear()
        };

        foreach (var act in activations)
        {
            Console.WriteLine($"  [{act.GetType().Name}]");
            foreach (double val in inputs)
            {
                double outVal = act.Activate(val);
                Console.WriteLine($"    F({val,4:F1}) = {outVal,6:F4}");
            }
            Console.WriteLine();
        }

        PrintSectionFooter();
    }

    static void DemoXorSinirAgi()
    {
        PrintSectionHeader("🧠 Demo 3: XOR Sinir Ağı (Feed-Forward)");

        // XOR Veri seti
        var dataset = new DataSet();
        dataset.Add(new double[] { 0, 0 }, new double[] { 0 });
        dataset.Add(new double[] { 0, 1 }, new double[] { 1 });
        dataset.Add(new double[] { 1, 0 }, new double[] { 1 });
        dataset.Add(new double[] { 1, 1 }, new double[] { 0 });

        var network = new Network(seed: 42);
        network.AddLayer(new DenseLayer(2, 4, new Sigmoid(), new Random(42)));
        network.AddLayer(new DenseLayer(4, 1, new Sigmoid(), new Random(42)));

        var optimizer = new HexAI.NeuralNetwork.Optimizers.SGDOptimizer(learningRate: 0.5);

        Console.WriteLine("  XOR Ağı 1000 epoch boyunca eğitiliyor...");
        network.Train(dataset, epochs: 1000, optimizer: optimizer, reportInterval: 200);

        Console.WriteLine("\n  Tahmin Sonuçları:");
        foreach (var sample in dataset.Samples)
        {
            var pred = network.Predict(sample.Inputs);
            Console.WriteLine($"    Girdi: [{sample.Inputs[0]}, {sample.Inputs[1]}] -> Beklenen: {sample.ExpectedOutputs[0]} -> Tahmin: {pred[0]:F4}");
        }

        PrintSectionFooter();
    }

    static void DemoRNN()
    {
        PrintSectionHeader("📝 Demo 4: Transformer / Mini-GPT Kelime Tahmini");

        Console.WriteLine("  Senaryo: Kelime tabanlı, Multi-Head Attention, Embedding ve LayerNorm içeren");
        Console.WriteLine("           Mini-GPT modelimiz, verilen cümleden kelimelerin akışını tahmin etmeyi öğrenecek.");
        Console.WriteLine();

        string corpus = "yapay zeka gelecektir yapay zeka gelecektir";
        Console.WriteLine($"  Eğitim Metni (Corpus): \"{corpus}\"\n");

        var miniGpt = new MiniGPT(hiddenSize: 16, learningRate: 0.2, seed: 42);
        
        // Eğit
        miniGpt.Train(corpus, epochs: 1500);
        
        Console.WriteLine("\n  ✅ Eğitim tamamlandı! Şimdi ağın kelime düzeyinde metin üretme (Autoregressive Generation) testini yapalım:");

        // Test 1: "yapay" verelim
        string seed1 = "yapay";
        string generated1 = miniGpt.Generate(seedText: seed1, generateLength: 3);
        Console.WriteLine($"  [Üretim 1] Başlangıç: '{seed1}' -> Sonuç: {generated1}");

        // Test 2: "yapay zeka" verelim
        string seed2 = "yapay zeka";
        string generated2 = miniGpt.Generate(seedText: seed2, generateLength: 2);
        Console.WriteLine($"  [Üretim 2] Başlangıç: '{seed2}' -> Sonuç: {generated2}");

        PrintSectionFooter();
    }

    static void DemoMultiAgent()
    {
        PrintSectionHeader("🤖 Demo 5: Multi-Agent Sistemi (Pipeline)");

        Console.WriteLine("  Senaryo: Bir görev, Fikir Üretici -> Mimari Tasarımcı -> Kodlayıcı zincirinde işleniyor.");
        Console.WriteLine("  (Not: Demo amaçlı olarak bu ajanlar küçük ve eğitilmemiş bir GPT modeli ile çalışacaktır.)\n");

        // Hızlı çalışması için küçük boyutlu, rastgele bir model oluşturuyoruz.
        var model = new HexAI.NLP.Generative.LargeGPT(hiddenSize: 8, numHeads: 1, numLayers: 1, sequenceLength: 5, learningRate: 0.1, seed: 42);
        
        // Hata vermemesi için çok kısa eğitiyoruz (Demo amaçlı)
        model.Train("yapay zeka ile geleceği keşfetmek çok güzel", epochs: 1);

        // Ajanları tanımlayalım 
        var agent1 = new HexAI.Assistant.Agents.BaseAgent("Fikir Üretici", "Sen yaratıcı bir fikir üreticisisin.", model, generateLength: 3);
        var agent2 = new HexAI.Assistant.Agents.BaseAgent("Mimari Tasarımcı", "Sen bir yazılım mimarısın.", model, generateLength: 3);
        var agent3 = new HexAI.Assistant.Agents.BaseAgent("Kodlayıcı", "Sen bir yazılımcısın.", model, generateLength: 3);

        var pipeline = new HexAI.Assistant.Agents.AgentPipeline();
        pipeline.AddAgent(agent1);
        pipeline.AddAgent(agent2);
        pipeline.AddAgent(agent3);

        string baslangic = "Bir oyun yapalım.";
        Console.WriteLine($"  [Başlangıç Girdisi]: {baslangic}\n");

        var resultTask = pipeline.RunAsync(baslangic, (ajanAdi, uretilenCikti) =>
        {
            Console.WriteLine($"  [{ajanAdi}]:");
            Console.WriteLine($"  {uretilenCikti}\n");
        });

        // RunAsync task döndürdüğü için ve konsol uygulamasında olduğumuz için Wait() yapalım
        resultTask.Wait();

        Console.WriteLine($"  [Final Sonuç]:\n  {resultTask.Result}");

        PrintSectionFooter();
    }

    static void DemoVerificationPipeline()
    {
        PrintSectionHeader("🔍 Demo 6: Dış LLM Doğrulama Pipeline");
        Console.WriteLine("  HexAI v2.0 artık yerel GGUF LLM ve ReAct Ajan motorunu destekliyor.");
        Console.WriteLine("  Etkileşimli sohbet ve araştırma için 'cli/HexAI.CLI' çalıştırın.");
        PrintSectionFooter();
    }

    static void PrintSectionHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n┌────────────────────────────────────────────────────────┐");
        Console.WriteLine($"│ {title,-54} │");
        Console.WriteLine($"└────────────────────────────────────────────────────────┘");
        Console.ResetColor();
    }

    static void PrintSectionFooter()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("----------------------------------------------------------");
        Console.ResetColor();
    }
}
