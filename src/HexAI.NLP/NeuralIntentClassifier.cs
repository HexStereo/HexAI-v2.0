using System;
using System.Collections.Generic;
using System.Linq;
using HexAI.Core.Activation;
using HexAI.Core.Data;
using HexAI.Core.Loss;
using HexAI.NeuralNetwork;
using HexAI.NeuralNetwork.Layers;
using HexAI.NeuralNetwork.Optimizers;

namespace HexAI.NLP;

/// <summary>
/// Kural tabanlı (if-else) niyet algılayıcısının yerine geçen, sinir ağı (Neural Network) tabanlı
/// niyet sınıflandırıcı (Neural Intent Classifier).
/// </summary>
public class NeuralIntentClassifier
{
    private readonly Network _network;
    private readonly WordVocabulary _vocabulary;
    private readonly Dictionary<int, IntentClassifier.Intent> _idToIntent;
    private readonly Dictionary<IntentClassifier.Intent, int> _intentToId;
    
    // Fallback için eski sınıflandırıcıyı tutuyoruz (henüz eğitilmemişse)
    private readonly IntentClassifier _fallbackClassifier;
    private bool _isTrained = false;

    public NeuralIntentClassifier()
    {
        _network = new Network(seed: 42);
        _vocabulary = new WordVocabulary();
        _fallbackClassifier = new IntentClassifier();
        
        _idToIntent = new Dictionary<int, IntentClassifier.Intent>();
        _intentToId = new Dictionary<IntentClassifier.Intent, int>();

        // Intentleri eşle
        int id = 0;
        foreach (IntentClassifier.Intent intent in Enum.GetValues(typeof(IntentClassifier.Intent)))
        {
            _idToIntent[id] = intent;
            _intentToId[intent] = id;
            id++;
        }
    }

    /// <summary>
    /// Kural tabanlı eski sınıflandırıcının eşleşmelerinden sentetik veriseti 
    /// üreterek sinir ağını eğitir.
    /// </summary>
    public void TrainSynthetic(int epochs = 100)
    {
        var dataset = GenerateSyntheticDataset();
        if (dataset.Count == 0) return;

        // Ağ mimarisini kur:
        // Girdi: Maksimum 10 kelimelik sekans ortalaması (BoW / Average Pooling)
        // Gizli Katman: 32 nöron (ReLU)
        // Çıktı: Intent sayısı kadar (Softmax / CrossEntropy)
        
        if (_network.LayerCount == 0)
        {
            _network.AddLayer(new DenseLayer(_vocabulary.Size, 32, new ReLU()));
            _network.AddLayer(new DenseLayer(32, _idToIntent.Count, new Softmax()));
        }

        var optimizer = new AdamOptimizer(learningRate: 0.05);
        var loss = new CrossEntropyLoss(_idToIntent.Count);

        _network.Train(dataset, epochs, optimizer, loss, reportInterval: Math.Max(1, epochs/5));
        _isTrained = true;
    }

    /// <summary>
    /// Verilen metnin niyetini sinir ağı ile sınıflandırır.
    /// Ağ eğitilmemişse kural tabanlı eski sınıflandırıcıya düşer (fallback).
    /// </summary>
    public IntentClassifier.IntentResult Classify(string input)
    {
        // Slash komutları hala kural tabanlıya gitsin
        if (input.Trim().StartsWith("/"))
        {
            return _fallbackClassifier.Classify(input);
        }

        if (!_isTrained)
        {
            return _fallbackClassifier.Classify(input);
        }

        var words = Tokenizer.Tokenize(input.ToLower(new System.Globalization.CultureInfo("tr-TR")));
        if (words.Count == 0)
        {
            return new IntentClassifier.IntentResult { Intent = IntentClassifier.Intent.Unknown, OriginalInput = input };
        }

        // Giriş vektörü oluştur (BoW - Bag of Words)
        double[] inputVector = new double[_vocabulary.Size];
        foreach (var word in words)
        {
            int id = _vocabulary.GetId(word);
            if (id != _vocabulary.UnkId)
            {
                inputVector[id] = 1.0; 
            }
        }

        var prediction = _network.Predict(inputVector);

        // En yüksek olasılıklı intenti bul
        double maxProb = prediction[0];
        int maxId = 0;
        for (int i = 1; i < prediction.Length; i++)
        {
            if (prediction[i] > maxProb)
            {
                maxProb = prediction[i];
                maxId = i;
            }
        }

        var intent = _idToIntent[maxId];

        var result = new IntentClassifier.IntentResult
        {
            Intent = intent,
            Confidence = maxProb,
            OriginalInput = input
        };

        // Parametreleri (dosya yolu vb) eski yöntemle çıkar
        if (intent == IntentClassifier.Intent.FileOperation || intent == IntentClassifier.Intent.RunCommand)
        {
            var fallbackResult = _fallbackClassifier.Classify(input);
            result.Parameters = fallbackResult.Parameters;
        }

        return result;
    }

    private DataSet GenerateSyntheticDataset()
    {
        var dataset = new DataSet();
        
        // Sentetik eğitim verileri
        var samples = new List<(string Text, IntentClassifier.Intent Intent)>
        {
            ("masaüstündeki dosyaları listele", IntentClassifier.Intent.FileOperation),
            ("c dizinini göster", IntentClassifier.Intent.FileOperation),
            ("dosyayı aç", IntentClassifier.Intent.FileOperation),
            ("belgeleri oku", IntentClassifier.Intent.FileOperation),
            
            ("powershell komutu çalıştır", IntentClassifier.Intent.RunCommand),
            ("git status çalıştır", IntentClassifier.Intent.RunCommand),
            ("terminalde npm install yap", IntentClassifier.Intent.RunCommand),
            
            ("saat kaç", IntentClassifier.Intent.SystemInfo),
            ("bugün günlerden ne", IntentClassifier.Intent.SystemInfo),
            ("disk alanım ne kadar", IntentClassifier.Intent.SystemInfo),
            ("ram kullanımı nedir", IntentClassifier.Intent.SystemInfo),
            ("cpu durumu", IntentClassifier.Intent.SystemInfo),
            
            ("25 çarpı 16", IntentClassifier.Intent.Math),
            ("12 artı 5 hesapla", IntentClassifier.Intent.Math),
            ("bunun sonucu kaçtır", IntentClassifier.Intent.Math),
            
            ("beni eğit", IntentClassifier.Intent.Train),
            ("yeni veri öğren", IntentClassifier.Intent.Train),
            
            ("not defteri aç", IntentClassifier.Intent.OpenApplication),
            ("chrome başlat", IntentClassifier.Intent.OpenApplication),
            ("hesap makinesini çalıştır", IntentClassifier.Intent.OpenApplication),
            
            ("merhaba", IntentClassifier.Intent.Chat),
            ("nasılsın", IntentClassifier.Intent.Chat),
            ("teşekkür ederim", IntentClassifier.Intent.Chat),
            ("bana kendini anlat", IntentClassifier.Intent.Chat)
        };

        var asciiSamples = new List<(string Text, IntentClassifier.Intent Intent)>();
        foreach (var s in samples)
        {
            string asciiText = s.Text.Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');
            if (asciiText != s.Text)
            {
                asciiSamples.Add((asciiText, s.Intent));
            }
        }
        samples.AddRange(asciiSamples);

        // Kelimeleri sözlüğe ekle
        foreach (var sample in samples)
        {
            var words = Tokenizer.Tokenize(sample.Text);
            foreach (var word in words)
            {
                _vocabulary.AddWord(word);
            }
        }

        // Vektörleri oluştur
        foreach (var sample in samples)
        {
            var words = Tokenizer.Tokenize(sample.Text);
            double[] inputVector = new double[_vocabulary.Size];
            foreach (var word in words)
            {
                inputVector[_vocabulary.GetId(word)] = 1.0;
            }

            double[] targetVector = new double[_idToIntent.Count];
            targetVector[_intentToId[sample.Intent]] = 1.0;

            dataset.Add(inputVector, targetVector);
        }

        return dataset;
    }
}

