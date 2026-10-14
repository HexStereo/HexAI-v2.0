using System;
using System.Collections.Generic;
using System.Linq;
using HexAI.Core.Activation;
using HexAI.Core.Data;
using HexAI.Core.Loss;
using HexAI.NeuralNetwork;
using HexAI.NeuralNetwork.Layers;
using HexAI.NeuralNetwork.Optimizers;

namespace HexAI.NLP.Generative;

/// <summary>
/// Çoklu Transformer katmanı, genişletilmiş sekans uzunluğu (Sequence Length) ve
/// Multi-Head Attention ile yapılandırılabilir Büyük GPT Modeli.
/// </summary>
public class LargeGPT
{
    private readonly Network _network;
    private readonly WordVocabulary _vocabulary = new();
    
    public int HiddenSize { get; }
    public int NumHeads { get; }
    public int NumLayers { get; }
    public int SequenceLength { get; }
    public double LearningRate { get; }
    internal string ModelSignature { get; } = "SGV4U3RlcmVv";

    /// <summary>
    /// Büyük bir GPT modeli oluşturur.
    /// </summary>
    /// <param name="hiddenSize">Gizli katman boyutu (Embedding boyutu).</param>
    /// <param name="numHeads">Attention kafa sayısı.</param>
    /// <param name="numLayers">Transformer blok sayısı.</param>
    /// <param name="sequenceLength">Bağlam penceresi genişliği (Kelime sayısı).</param>
    /// <param name="learningRate">Öğrenme oranı.</param>
    /// <param name="seed">Rastgelelik çekirdeği.</param>
    public LargeGPT(
        int hiddenSize = 128, 
        int numHeads = 4, 
        int numLayers = 2, 
        int sequenceLength = 16, 
        double learningRate = 0.01, 
        int? seed = null)
    {
        HiddenSize = hiddenSize;
        NumHeads = numHeads;
        NumLayers = numLayers;
        SequenceLength = sequenceLength;
        LearningRate = learningRate;
        _network = new Network(seed);
    }

    /// <summary>
    /// Modeli corpus üzerinden eğitir.
    /// </summary>
    public void Train(string corpus, int epochs = 100, Action<int, double>? onEpochComplete = null)
    {
        if (string.IsNullOrWhiteSpace(corpus))
            throw new ArgumentException("Eğitim metni boş olamaz.");

        // 1. Sözlüğü oluştur
        _vocabulary.BuildFromText(corpus);

        var words = Tokenizer.Tokenize(corpus);
        if (words.Count < SequenceLength + 1)
            throw new ArgumentException($"Eğitim metni çok kısa (en az {SequenceLength + 1} kelime olmalı).");

        // 2. Ağı başlat
        InitializeNetwork();

        // 3. Veriseti Oluştur (Sliding Window)
        var dataset = new DataSet();
        for (int i = 0; i <= words.Count - SequenceLength - 1; i++)
        {
            double[] inputIds = new double[SequenceLength];
            for (int j = 0; j < SequenceLength; j++)
            {
                inputIds[j] = _vocabulary.GetId(words[i + j]);
            }

            string targetWord = words[i + SequenceLength];
            double[] targetVector = new double[_vocabulary.Size];
            targetVector[_vocabulary.GetId(targetWord)] = 1.0;

            dataset.Add(inputIds, targetVector);
        }

        // 4. Eğitim Başlat
        var optimizer = new AdamOptimizer(learningRate: LearningRate);
        var lossFunction = new CrossEntropyLoss(_vocabulary.Size);
        _network.Train(dataset, epochs, optimizer, lossFunction, reportInterval: Math.Max(1, epochs / 10), onEpochComplete);
    }

    private void InitializeNetwork()
    {
        if (_network.LayerCount > 0) return;

        // Embedding ve Positional Encoding
        _network.AddLayer(new EmbeddingLayer(SequenceLength, _vocabulary.Size, HiddenSize));
        _network.AddLayer(new PositionalEncodingLayer(SequenceLength, HiddenSize));

        // Transformer Blokları
        for (int i = 0; i < NumLayers; i++)
        {
            _network.AddLayer(new MultiHeadAttentionLayer(SequenceLength, HiddenSize, NumHeads));
            _network.AddLayer(new LayerNormLayer(SequenceLength, HiddenSize));
            // Basit bir FFN (Feed-Forward Network) - şu an HexAI.Core'da Sequence için Dense kullanımı
            // MultiHeadAttention 3D tensor döner, Dense 1D bekler, bu yüzden mimari kısıtları 
            // nedeniyle doğrudan DenseLayer'a bağlanıyoruz (HexAI MiniGPT'deki yapıyı koruyarak).
            // İleride SequenceDenseLayer gibi bir şey eklenebilir.
        }

        // Çıktı Projeksiyon Katmanı
        _network.AddLayer(new DenseLayer(SequenceLength * HiddenSize, _vocabulary.Size, new Linear()));
    }

    /// <summary>
    /// Model ağı, kelime tahmin eder (Generative).
    /// </summary>
    public string Generate(string seedText, int generateLength = 20, double temperature = 0.7)
    {
        if (_vocabulary.Size <= 4)
            throw new InvalidOperationException("Model henüz eğitilmedi.");

        _network.ResetState();

        var seedWords = Tokenizer.Tokenize(seedText);
        if (seedWords.Count == 0)
        {
            seedWords.Add(_vocabulary.GetWord(4));
        }

        var currentWindow = new List<int>();
        for (int i = Math.Max(0, seedWords.Count - SequenceLength); i < seedWords.Count; i++)
        {
            currentWindow.Add(_vocabulary.GetId(seedWords[i]));
        }

        while (currentWindow.Count < SequenceLength)
        {
            currentWindow.Insert(0, _vocabulary.PadId);
        }

        List<string> generatedWords = new List<string>(seedWords);
        Random rng = new Random();

        for (int i = 0; i < generateLength; i++)
        {
            double[] inputIds = currentWindow.Select(id => (double)id).ToArray();
            var prediction = _network.Predict(inputIds);

            int nextWordId = 0;

            if (temperature <= 0.01)
            {
                double maxVal = prediction[0];
                for (int k = 1; k < prediction.Length; k++)
                {
                    if (prediction[k] > maxVal)
                    {
                        maxVal = prediction[k];
                        nextWordId = k;
                    }
                }
            }
            else
            {
                double[] probs = new double[prediction.Length];
                double maxLogit = prediction.Skip(1).Max();
                double sum = 0;
                for (int k = 1; k < prediction.Length; k++)
                {
                    probs[k] = Math.Exp((prediction[k] - maxLogit) / temperature);
                    sum += probs[k];
                }
                
                double r = rng.NextDouble() * sum;
                double cumulative = 0;
                for (int k = 1; k < prediction.Length; k++)
                {
                    cumulative += probs[k];
                    if (r <= cumulative)
                    {
                        nextWordId = k;
                        break;
                    }
                }
                if (nextWordId == 0) nextWordId = 1; 
            }

            string nextWord = _vocabulary.GetWord(nextWordId);
            generatedWords.Add(nextWord);

            currentWindow.RemoveAt(0);
            currentWindow.Add(nextWordId);
        }

        return string.Join(" ", generatedWords);
    }

    /// <summary>
    /// Model ağı, kelime kelime tahmin eder ve stream olarak döndürür (ChatGPT/Claude stili akış).
    /// </summary>
    public async IAsyncEnumerable<string> GenerateStreamAsync(string seedText, int generateLength = 20, double temperature = 0.7)
    {
        if (_vocabulary.Size <= 4)
            throw new InvalidOperationException("Model henüz eğitilmedi.");

        _network.ResetState();

        var seedWords = Tokenizer.Tokenize(seedText);
        if (seedWords.Count == 0)
        {
            seedWords.Add(_vocabulary.GetWord(4));
        }

        var currentWindow = new List<int>();
        for (int i = Math.Max(0, seedWords.Count - SequenceLength); i < seedWords.Count; i++)
        {
            currentWindow.Add(_vocabulary.GetId(seedWords[i]));
        }

        while (currentWindow.Count < SequenceLength)
        {
            currentWindow.Insert(0, _vocabulary.PadId);
        }

        Random rng = new Random();

        for (int i = 0; i < generateLength; i++)
        {
            // UI thread'i kilitlememek için ufak bir bekleme (Task.Yield veya Delay)
            await System.Threading.Tasks.Task.Yield();

            double[] inputIds = currentWindow.Select(id => (double)id).ToArray();
            var prediction = _network.Predict(inputIds);

            int nextWordId = 0;

            if (temperature <= 0.01)
            {
                double maxVal = prediction[0];
                for (int k = 1; k < prediction.Length; k++)
                {
                    if (prediction[k] > maxVal)
                    {
                        maxVal = prediction[k];
                        nextWordId = k;
                    }
                }
            }
            else
            {
                double[] probs = new double[prediction.Length];
                double maxLogit = prediction.Skip(1).Max();
                double sum = 0;
                for (int k = 1; k < prediction.Length; k++)
                {
                    probs[k] = Math.Exp((prediction[k] - maxLogit) / temperature);
                    sum += probs[k];
                }
                
                double r = rng.NextDouble() * sum;
                double cumulative = 0;
                for (int k = 1; k < prediction.Length; k++)
                {
                    cumulative += probs[k];
                    if (r <= cumulative)
                    {
                        nextWordId = k;
                        break;
                    }
                }
                if (nextWordId == 0) nextWordId = 1; 
            }

            string nextWord = _vocabulary.GetWord(nextWordId);
            
            // Üretilen kelimeyi akış olarak dışarı ver
            yield return nextWord;

            currentWindow.RemoveAt(0);
            currentWindow.Add(nextWordId);
        }
    }
}
