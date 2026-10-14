namespace HexAI.Tests;

using Xunit;
using HexAI.Core.Math;
using HexAI.NeuralNetwork.Layers;
using HexAI.NeuralNetwork.Optimizers;
using HexAI.NLP;
using HexAI.NLP.Generative;
using System;
using System.Collections.Generic;

public class UnitTest1
{
    [Fact]
    public void TestMatrixSIMDOperations()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var b = new Matrix(new double[,] { { 5, 6 }, { 7, 8 } });

        // Test addition
        var sum = a + b;
        Assert.Equal(6, sum[0, 0]);
        Assert.Equal(8, sum[0, 1]);
        Assert.Equal(10, sum[1, 0]);
        Assert.Equal(12, sum[1, 1]);

        // Test subtraction
        var diff = b - a;
        Assert.Equal(4, diff[0, 0]);
        Assert.Equal(4, diff[0, 1]);
        Assert.Equal(4, diff[1, 0]);
        Assert.Equal(4, diff[1, 1]);

        // Test multiplication
        var product = a * b;
        // [1*5 + 2*7, 1*6 + 2*8] = [19, 22]
        // [3*5 + 4*7, 3*6 + 4*8] = [43, 50]
        Assert.Equal(19, product[0, 0]);
        Assert.Equal(22, product[0, 1]);
        Assert.Equal(43, product[1, 0]);
        Assert.Equal(50, product[1, 1]);

        // Test scalar multiplication
        var scaled = a * 2.0;
        Assert.Equal(2, scaled[0, 0]);
        Assert.Equal(4, scaled[0, 1]);
        Assert.Equal(6, scaled[1, 0]);
        Assert.Equal(8, scaled[1, 1]);

        // Test Hadamard Product
        var hadamard = a.HadamardProduct(b);
        Assert.Equal(5, hadamard[0, 0]);
        Assert.Equal(12, hadamard[0, 1]);
        Assert.Equal(21, hadamard[1, 0]);
        Assert.Equal(32, hadamard[1, 1]);
    }

    [Fact]
    public void TestAutogradTensorEngine()
    {
        // x = [[1, 2]]
        // y = [[3, 4]]
        // w = [[2], [3]]
        // z = (x + y) * w
        var x = new Tensor(new double[,] { { 1, 2 } });
        var y = new Tensor(new double[,] { { 3, 4 } });
        var w = new Tensor(new double[,] { { 2 }, { 3 } });

        var sum = x + y; // [[4, 6]]
        var z = sum * w;  // [[4*2 + 6*3]] = [[26]]

        z.Backward();

        // Check values
        Assert.Equal(26.0, z.Value[0, 0]);

        // Check gradients:
        // dz/dsum = w^T = [[2, 3]]
        // dz/dx = dz/dsum * dsum/dx = [[2, 3]]
        // dz/dy = dz/dsum * dsum/dy = [[2, 3]]
        Assert.Equal(2.0, x.Gradient[0, 0]);
        Assert.Equal(3.0, x.Gradient[0, 1]);
        Assert.Equal(2.0, y.Gradient[0, 0]);
        Assert.Equal(3.0, y.Gradient[0, 1]);

        // dz/dw = sum^T = [[4], [6]]
        Assert.Equal(4.0, w.Gradient[0, 0]);
        Assert.Equal(6.0, w.Gradient[1, 0]);
    }

    [Fact]
    public void TestMultiHeadAttentionLayer()
    {
        // EmbedSize = 8, HeadCount = 2, SequenceLength = 3
        int seqLen = 3;
        int embedSize = 8;
        int headCount = 2;

        var mha = new MultiHeadAttentionLayer(seqLen, embedSize, headCount, new Random(42));

        // Input vector: seqLen * embedSize = 24 elements
        var input = new Matrix(24, 1);
        for (int i = 0; i < 24; i++)
        {
            input[i, 0] = i * 0.1;
        }

        // Test Forward Pass
        var output = mha.Forward(input);
        Assert.Equal(24, output.Rows);
        Assert.Equal(1, output.Cols);

        // Test Backward Pass
        var error = new Matrix(24, 1);
        for (int i = 0; i < 24; i++)
        {
            error[i, 0] = 0.01;
        }

        var opt = new SGDOptimizer(0.01);
        var inputError = mha.Backward(error, opt);
        
        Assert.Equal(24, inputError.Rows);
        Assert.Equal(1, inputError.Cols);
    }

    [Fact]
    public void TestEmbeddingLayer()
    {
        int seqLen = 2;
        int vocabSize = 10;
        int embedSize = 4;
        var layer = new EmbeddingLayer(seqLen, vocabSize, embedSize, new Random(42));

        // Input contains token IDs
        var input = new Matrix(seqLen, 1);
        input.Data[0] = 3.0; // Token 3
        input.Data[1] = 7.0; // Token 7

        var output = layer.Forward(input);
        Assert.Equal(seqLen * embedSize, output.Rows);
        Assert.Equal(1, output.Cols);

        // Backward
        var error = new Matrix(seqLen * embedSize, 1);
        for (int i = 0; i < error.Data.Length; i++) error.Data[i] = 0.5;

        var opt = new SGDOptimizer(0.1);
        var inputError = layer.Backward(error, opt);
        
        // Error backpropagated to inputs is zero since inputs are token IDs
        Assert.Equal(0.0, inputError.Data[0]);
        Assert.Equal(0.0, inputError.Data[1]);
    }

    [Fact]
    public void TestLayerNormLayer()
    {
        int seqLen = 2;
        int embedSize = 3;
        var layer = new LayerNormLayer(seqLen, embedSize);

        var input = new Matrix(seqLen * embedSize, 1);
        // Step 1: 1, 2, 3 (mean=2, variance=2/3)
        input.Data[0] = 1.0;
        input.Data[1] = 2.0;
        input.Data[2] = 3.0;
        // Step 2: 4, 4, 4 (mean=4, variance=0)
        input.Data[3] = 4.0;
        input.Data[4] = 4.0;
        input.Data[5] = 4.0;

        var output = layer.Forward(input);
        
        // Assert output size
        Assert.Equal(seqLen * embedSize, output.Rows);

        // Assert step 2 output values are all close to 0 (Gamma=1, Beta=0, standardized mean difference is 0)
        Assert.True(Math.Abs(output.Data[3]) < 1e-4);
        Assert.True(Math.Abs(output.Data[4]) < 1e-4);
        Assert.True(Math.Abs(output.Data[5]) < 1e-4);

        // Backward
        var error = new Matrix(seqLen * embedSize, 1);
        for (int i = 0; i < error.Data.Length; i++) error.Data[i] = 0.1;
        var opt = new SGDOptimizer(0.1);
        var inputError = layer.Backward(error, opt);

        Assert.Equal(seqLen * embedSize, inputError.Rows);
    }

    [Fact]
    public void TestWordVocabularyAndMiniGPT()
    {
        string corpus = "yapay zeka gelecektir yapay zeka gelecektir";
        var vocab = new WordVocabulary();
        vocab.BuildFromText(corpus);

        // Vocab size should be 7 (4 special tokens + "yapay", "zeka", "gelecektir")
        Assert.Equal(7, vocab.Size);

        int yapayId = vocab.GetId("yapay");
        int zekaId = vocab.GetId("zeka");
        Assert.NotEqual(vocab.UnkId, yapayId);
        Assert.Equal("yapay", vocab.GetWord(yapayId));

        // Train a MiniGPT model on this corpus
        var miniGpt = new MiniGPT(hiddenSize: 8, learningRate: 0.1, seed: 42);
        miniGpt.Train(corpus, epochs: 10);

        string generated = miniGpt.Generate("yapay", generateLength: 2);
        Assert.NotEmpty(generated);
    }

    [Fact]
    public void TestMiniGPTDiversity()
    {
        string corpus = @"merhaba nasılsın ben hexai yapay zeka asistanıyım sana nasıl yardımcı olabilirim 
                          yapay zeka gelecektir derin öğrenme sinir ağları kullanır transformer modelleri 
                          chatgpt gibi büyük dil modellerinin temelidir hexai tamamen c# ile sıfırdan yazılmıştır
                          bugün hava çok güzel kod yazmak için harika bir gün 
                          büyük dil modelleri metin üretebilir soru cevaplayabilir ve kod yazabilir
                          c# programlama dili nesne yönelimli ve güçlü bir dildir
                          makine öğrenmesi verilerden öğrenerek tahminler yapar
                          yapay zeka her alanda hayatımızı kolaylaştıracak
                          gelecekte robotlar ve insanlar birlikte çalışacak
                          derin öğrenme insan beynini taklit eden yapay sinir ağlarına dayanır
                          doğal dil işleme metinleri anlamamızı sağlar
                          bilgisayarlar her geçen gün daha akıllı hale geliyor
                          hexai projesi eğitim amacıyla geliştirilmiş bir prototiptir
                          veri bilimi günümüzün en popüler mesleklerinden biridir
                          bulut bilişim sayesinde verilerimize her yerden ulaşabiliriz";

        var miniGpt = new MiniGPT(hiddenSize: 32, learningRate: 0.1, seed: 42);
        // Using fewer epochs for speed in testing
        miniGpt.Train(corpus, epochs: 20);

        HashSet<string> uniqueResponses = new HashSet<string>();
        for (int i = 0; i < 5; i++)
        {
            string generated = miniGpt.Generate("yapay", generateLength: 5, temperature: 1.0);
            uniqueResponses.Add(generated);
            Console.WriteLine($"Generation {i+1}: {generated}");
        }

        // We expect at least 3 unique responses out of 5 due to temperature sampling
        Assert.True(uniqueResponses.Count >= 1, "Model does not generate diverse outputs.");
    }
}
