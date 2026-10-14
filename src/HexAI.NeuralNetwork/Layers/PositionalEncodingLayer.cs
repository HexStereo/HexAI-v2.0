using System;
using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Sekanstaki kelimelerin sırasını ve konumunu modele bildirmek için sinusoidal konumsal kodlama katmanı.
/// </summary>
public class PositionalEncodingLayer : ILayer
{
    public int SequenceLength { get; }
    public int EmbedSize { get; }

    public int InputSize => SequenceLength * EmbedSize;
    public int NeuronCount => SequenceLength * EmbedSize;

    public IActivation Activation { get; }

    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    private Matrix _pe;

    public PositionalEncodingLayer(int sequenceLength, int embedSize)
    {
        SequenceLength = sequenceLength;
        EmbedSize = embedSize;
        Activation = new Linear();

        _pe = new Matrix(SequenceLength * EmbedSize, 1);
        
        // Sinusoidal Positional Encoding oluştur
        for (int pos = 0; pos < SequenceLength; pos++)
        {
            int offset = pos * EmbedSize;
            for (int i = 0; i < EmbedSize; i++)
            {
                double angle = pos / Math.Pow(10000.0, (2.0 * (i / 2)) / EmbedSize);
                if (i % 2 == 0)
                {
                    _pe.Data[offset + i] = Math.Sin(angle);
                }
                else
                {
                    _pe.Data[offset + i] = Math.Cos(angle);
                }
            }
        }
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        LastOutput = new Matrix(InputSize, 1);

        // Input vektörüne konumsal kodlama değerini ekle: output = input + PE
        for (int i = 0; i < InputSize; i++)
        {
            LastOutput.Data[i] = input.Data[i] + _pe.Data[i];
        }

        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // Konumsal kodlama parametresiz (statik) olduğu için gradyanı aynen geriye aktarırız
        return error;
    }
}
