using System;
using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Token ID'leri (tamsayıları) yoğun vektörlere (dense embeddings) dönüştüren katman.
/// Eğitim sırasında bu kelime vektörlerini optimize eder.
/// </summary>
public class EmbeddingLayer : ILayer
{
    public int SequenceLength { get; }
    public int VocabSize { get; }
    public int EmbedSize { get; }

    public int InputSize => SequenceLength;
    public int NeuronCount => SequenceLength * EmbedSize;

    public IActivation Activation { get; }

    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    private Matrix _embeddings;

    public EmbeddingLayer(int sequenceLength, int vocabSize, int embedSize, Random? rng = null)
    {
        SequenceLength = sequenceLength;
        VocabSize = vocabSize;
        EmbedSize = embedSize;
        Activation = new Linear();

        rng ??= new Random();
        // Xavier/Glorot init benzeri rastgele embedding tablosu
        _embeddings = Matrix.Random(VocabSize, EmbedSize, rng) * 0.1;
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        LastOutput = new Matrix(NeuronCount, 1);

        for (int i = 0; i < SequenceLength; i++)
        {
            int tokenId = (int)input[i, 0];
            if (tokenId < 0 || tokenId >= VocabSize)
            {
                tokenId = 1; // <unk> fallback
            }

            int embedOffset = i * EmbedSize;
            int vocabOffset = tokenId * EmbedSize;

            for (int j = 0; j < EmbedSize; j++)
            {
                LastOutput[embedOffset + j, 0] = _embeddings.Data[vocabOffset + j];
            }
        }

        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        Matrix gradEmbeddings = new Matrix(VocabSize, EmbedSize);

        for (int i = 0; i < SequenceLength; i++)
        {
            int tokenId = (int)LastInput[i, 0];
            if (tokenId < 0 || tokenId >= VocabSize)
            {
                tokenId = 1;
            }

            int errorOffset = i * EmbedSize;
            int vocabOffset = tokenId * EmbedSize;

            for (int j = 0; j < EmbedSize; j++)
            {
                gradEmbeddings.Data[vocabOffset + j] += error.Data[errorOffset + j];
            }
        }

        // Ağırlıkları güncelle
        _embeddings.SubtractInPlace(optimizer.ComputeUpdate(gradEmbeddings, _embeddings));

        // Girişe iletilecek hata sıfırdır çünkü giriş sayısal token ID'leridir
        return new Matrix(SequenceLength, 1);
    }
}

