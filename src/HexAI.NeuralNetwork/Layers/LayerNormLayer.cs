using System;
using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Transformer ağlarının kararlı eğitilmesi için Katman Normalizasyonu (Layer Normalization) katmanı.
/// Her özellik vektörünü kendi ortalaması ve standart sapması ile normalize eder.
/// </summary>
public class LayerNormLayer : ILayer
{
    public int SequenceLength { get; }
    public int EmbedSize { get; }

    public int InputSize => SequenceLength * EmbedSize;
    public int NeuronCount => SequenceLength * EmbedSize;

    public IActivation Activation { get; }

    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    // Trainable parameters
    private double[] _gamma;
    private double[] _beta;

    // Intermediate values for backprop
    private double[] _means = Array.Empty<double>();
    private double[] _vars = Array.Empty<double>();
    private double[] _xHat = Array.Empty<double>();

    private const double Epsilon = 1e-5;

    public LayerNormLayer(int sequenceLength, int embedSize)
    {
        SequenceLength = sequenceLength;
        EmbedSize = embedSize;
        Activation = new Linear();

        _gamma = new double[EmbedSize];
        _beta = new double[EmbedSize];

        // Gamma'ları 1.0, Beta'ları 0.0 olarak başlat (Identity transformation başlangıçta)
        for (int i = 0; i < EmbedSize; i++)
        {
            _gamma[i] = 1.0;
            _beta[i] = 0.0;
        }
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        LastOutput = new Matrix(InputSize, 1);

        _means = new double[SequenceLength];
        _vars = new double[SequenceLength];
        _xHat = new double[InputSize];

        for (int i = 0; i < SequenceLength; i++)
        {
            int offset = i * EmbedSize;

            // 1. Ortalama hesapla
            double sum = 0;
            for (int j = 0; j < EmbedSize; j++)
            {
                sum += input.Data[offset + j];
            }
            double mean = sum / EmbedSize;
            _means[i] = mean;

            // 2. Varyans hesapla
            double varSum = 0;
            for (int j = 0; j < EmbedSize; j++)
            {
                double diff = input.Data[offset + j] - mean;
                varSum += diff * diff;
            }
            double variance = varSum / EmbedSize;
            _vars[i] = variance;

            // 3. Normalize et, ölçekle ve kaydır (Scale and shift)
            double stdDev = Math.Sqrt(variance + Epsilon);
            for (int j = 0; j < EmbedSize; j++)
            {
                double xHatVal = (input.Data[offset + j] - mean) / stdDev;
                _xHat[offset + j] = xHatVal;
                LastOutput.Data[offset + j] = xHatVal * _gamma[j] + _beta[j];
            }
        }

        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        Matrix dInput = new Matrix(InputSize, 1);

        double[] gradGamma = new double[EmbedSize];
        double[] gradBeta = new double[EmbedSize];

        // 1. Gamma ve Beta gradyanlarını hesapla
        for (int i = 0; i < SequenceLength; i++)
        {
            int offset = i * EmbedSize;
            for (int j = 0; j < EmbedSize; j++)
            {
                gradGamma[j] += error.Data[offset + j] * _xHat[offset + j];
                gradBeta[j] += error.Data[offset + j];
            }
        }

        // 2. Ağırlıkları güncelle (Gamma/Beta)
        // Gamma ve Beta vektörlerini matris olarak temsil edip optimizer'a yollayalım
        Matrix gammaMat = new Matrix(1, EmbedSize);
        Matrix betaMat = new Matrix(1, EmbedSize);
        Matrix gradGammaMat = new Matrix(1, EmbedSize);
        Matrix gradBetaMat = new Matrix(1, EmbedSize);

        for (int j = 0; j < EmbedSize; j++)
        {
            gammaMat.Data[j] = _gamma[j];
            betaMat.Data[j] = _beta[j];
            gradGammaMat.Data[j] = gradGamma[j];
            gradBetaMat.Data[j] = gradBeta[j];
        }

        var updateGamma = optimizer.ComputeUpdate(gradGammaMat, gammaMat);
        var updateBeta = optimizer.ComputeUpdate(gradBetaMat, betaMat);

        for (int j = 0; j < EmbedSize; j++)
        {
            _gamma[j] -= updateGamma.Data[j];
            _beta[j] -= updateBeta.Data[j];
        }

        // 3. Girişe iletilecek gradyanları hesapla (LayerNorm derivative)
        for (int i = 0; i < SequenceLength; i++)
        {
            int offset = i * EmbedSize;
            double mean = _means[i];
            double variance = _vars[i];
            double stdDev = Math.Sqrt(variance + Epsilon);

            // d_xHat hesaplama
            double[] dxHat = new double[EmbedSize];
            for (int j = 0; j < EmbedSize; j++)
            {
                dxHat[j] = error.Data[offset + j] * _gamma[j];
            }

            // d_variance hesaplama
            double dVar = 0;
            for (int j = 0; j < EmbedSize; j++)
            {
                dVar += dxHat[j] * (LastInput.Data[offset + j] - mean) * -0.5 * Math.Pow(variance + Epsilon, -1.5);
            }

            // d_mean hesaplama
            double dMean = 0;
            double sumDxHat = 0;
            for (int j = 0; j < EmbedSize; j++)
            {
                sumDxHat += dxHat[j];
            }
            dMean = sumDxHat * -1.0 / stdDev;

            // d_input_i hesaplama
            for (int j = 0; j < EmbedSize; j++)
            {
                dInput.Data[offset + j] = (dxHat[j] / stdDev) + (dVar * 2.0 * (LastInput.Data[offset + j] - mean) / EmbedSize) + (dMean / EmbedSize);
            }
        }

        return dInput;
    }
}
