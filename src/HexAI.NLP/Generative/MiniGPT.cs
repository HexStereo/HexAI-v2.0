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
/// Kelime tabanlı, Multi-Head Attention, Positional Encoding, Embedding ve LayerNorm içeren
/// modern bir mini Generative Pre-trained Transformer (GPT) modeli.
/// </summary>
public class MiniGPT
{
    private readonly Network _network;
    private readonly WordVocabulary _vocabulary = new();
    private int _sequenceLength;
    
    public int HiddenSize { get; }
    public double LearningRate { get; }

    public MiniGPT(int hiddenSize = 32, double learningRate = 0.05, int? seed = null)
    {
        HiddenSize = hiddenSize;
        LearningRate = learningRate;
        _network = new Network(seed);
    }

    /// <summary>
    /// Modeli verilen metin (corpus) üzerinden kelime tabanlı eğitir.
    /// </summary>
    public void Train(string corpus, int epochs = 500, Action<int, double>? onEpochComplete = null)
    {
        if (string.IsNullOrWhiteSpace(corpus))
            throw new ArgumentException("Eğitim metni boş olamaz.");

        // 1. Sözlüğü kelimelere göre oluştur
        _vocabulary.BuildFromText(corpus);

        var words = Tokenizer.Tokenize(corpus);
        if (words.Count < 3)
            throw new ArgumentException("Eğitim metni kelime düzeyi öğrenme için çok kısa (en az 3 kelime olmalı).");

        // 2. Sekans uzunluğunu belirle (örneğin 2)
        _sequenceLength = Math.Min(2, words.Count - 1);

        // 3. Modeli bir kere başlat (Modern GPT Blok Yapısı)
        if (_network.LayerCount == 0)
        {
            int heads = 4;
            if (HiddenSize % heads != 0) heads = 1;

            _network.AddLayer(new EmbeddingLayer(_sequenceLength, _vocabulary.Size, HiddenSize));
            _network.AddLayer(new PositionalEncodingLayer(_sequenceLength, HiddenSize));
            _network.AddLayer(new MultiHeadAttentionLayer(_sequenceLength, HiddenSize, heads));
            _network.AddLayer(new LayerNormLayer(_sequenceLength, HiddenSize));
            _network.AddLayer(new DenseLayer(_sequenceLength * HiddenSize, _vocabulary.Size, new Linear()));
        }

        // 4. DataSet oluşturma (Kayan Pencere / Sliding Window)
        var dataset = new DataSet();
        for (int i = 0; i <= words.Count - _sequenceLength - 1; i++)
        {
            double[] inputIds = new double[_sequenceLength];
            for (int j = 0; j < _sequenceLength; j++)
            {
                inputIds[j] = _vocabulary.GetId(words[i + j]);
            }

            string targetWord = words[i + _sequenceLength];
            double[] targetVector = new double[_vocabulary.Size];
            targetVector[_vocabulary.GetId(targetWord)] = 1.0;

            dataset.Add(inputIds, targetVector);
        }

        // 5. Eğitim
        var optimizer = new AdamOptimizer(learningRate: LearningRate);
        var lossFunction = new CrossEntropyLoss(_vocabulary.Size);
        _network.Train(dataset, epochs, optimizer, lossFunction, reportInterval: Math.Max(1, epochs / 10), onEpochComplete);
    }

    /// <summary>
    /// Başlangıç metni (seed) alarak kelime düzeyinde yeni metin üretir.
    /// </summary>
    public string Generate(string seedText, int generateLength = 10, double temperature = 0.7)
    {
        if (_vocabulary.Size <= 4)
            throw new InvalidOperationException("Model henüz eğitilmedi.");

        _network.ResetState();

        var seedWords = Tokenizer.Tokenize(seedText);
        if (seedWords.Count == 0)
        {
            seedWords.Add(_vocabulary.GetWord(4)); // Rastgele bir kelime seç
        }

        // Pencereyi dolduracak kadar kelimeyi sığdır
        var currentWindow = new List<int>();
        for (int i = Math.Max(0, seedWords.Count - _sequenceLength); i < seedWords.Count; i++)
        {
            currentWindow.Add(_vocabulary.GetId(seedWords[i]));
        }

        // Eğer pencere eksik kaldıysa başına <pad> ekle
        while (currentWindow.Count < _sequenceLength)
        {
            currentWindow.Insert(0, _vocabulary.PadId);
        }

        List<string> generatedWords = new List<string>(seedWords);

        Random rng = new Random();

        // Kelime kelime üret
        for (int i = 0; i < generateLength; i++)
        {
            double[] inputIds = currentWindow.Select(id => (double)id).ToArray();
            var prediction = _network.Predict(inputIds);

            int nextWordId = 0;

            if (temperature <= 0.01)
            {
                // ArgMax
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
                // Softmax with temperature
                double[] probs = new double[prediction.Length];
                double maxLogit = prediction.Skip(1).Max(); // avoid overflow, skip pad
                double sum = 0;
                for (int k = 1; k < prediction.Length; k++) // skip pad token at 0
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
                if (nextWordId == 0) nextWordId = 1; // fallback to unk
            }

            string nextWord = _vocabulary.GetWord(nextWordId);
            generatedWords.Add(nextWord);

            // Pencereyi kaydır
            currentWindow.RemoveAt(0);
            currentWindow.Add(nextWordId);
        }

        return string.Join(" ", generatedWords);
    }
}
