using System;
using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Transformer'lar için Multi-Head Attention (Çok Başlı Dikkat) Katmanı.
/// EmbedSize'ı HeadCount parçaya bölerek sekans elemanlarının farklı boyutlarındaki
/// ilişkileri paralel olarak öğrenmesini sağlar.
/// </summary>
public class MultiHeadAttentionLayer : ILayer
{
    public int SequenceLength { get; }
    public int EmbedSize { get; }
    public int HeadCount { get; }
    public int HeadDim { get; }

    public int InputSize => SequenceLength * EmbedSize;
    public int NeuronCount => SequenceLength * EmbedSize;

    public IActivation Activation { get; }

    // Wq, Wk, Wv projection weights of size EmbedSize x EmbedSize
    private Matrix _Wq;
    private Matrix _Wk;
    private Matrix _Wv;
    
    // Output projection weight Wo of size EmbedSize x EmbedSize
    private Matrix _Wo;

    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    // Keep intermediate values for backward pass
    private Matrix _InputReshaped = new Matrix(1, 1);
    private Matrix[] _Q_heads = Array.Empty<Matrix>();
    private Matrix[] _K_heads = Array.Empty<Matrix>();
    private Matrix[] _V_heads = Array.Empty<Matrix>();
    private Matrix[] _Weights_heads = Array.Empty<Matrix>();
    private Matrix[] _Scores_heads = Array.Empty<Matrix>();
    private Matrix[] _Context_heads = Array.Empty<Matrix>();
    private Matrix _ConcatContext = new Matrix(1, 1);

    public MultiHeadAttentionLayer(int sequenceLength, int embedSize, int headCount, Random? rng = null)
    {
        if (embedSize % headCount != 0)
            throw new ArgumentException("EmbedSize, HeadCount'a kalansız bölünmelidir!");

        SequenceLength = sequenceLength;
        EmbedSize = embedSize;
        HeadCount = headCount;
        HeadDim = embedSize / headCount;
        Activation = new Linear();

        rng ??= new Random();
        _Wq = Matrix.Random(EmbedSize, EmbedSize, rng);
        _Wk = Matrix.Random(EmbedSize, EmbedSize, rng);
        _Wv = Matrix.Random(EmbedSize, EmbedSize, rng);
        _Wo = Matrix.Random(EmbedSize, EmbedSize, rng);
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;

        // Reshape 1D input (SequenceLength * EmbedSize) -> 2D (SequenceLength x EmbedSize)
        _InputReshaped = new Matrix(SequenceLength, EmbedSize);
        for (int i = 0; i < SequenceLength; i++)
            for (int j = 0; j < EmbedSize; j++)
                _InputReshaped[i, j] = input[i * EmbedSize + j, 0];

        // Q, K, V projections
        Matrix Q = _InputReshaped * _Wq;
        Matrix K = _InputReshaped * _Wk;
        Matrix V = _InputReshaped * _Wv;

        // Prepare head structures
        _Q_heads = new Matrix[HeadCount];
        _K_heads = new Matrix[HeadCount];
        _V_heads = new Matrix[HeadCount];
        _Scores_heads = new Matrix[HeadCount];
        _Weights_heads = new Matrix[HeadCount];
        _Context_heads = new Matrix[HeadCount];

        double scale = 1.0 / Math.Sqrt(HeadDim);

        for (int h = 0; h < HeadCount; h++)
        {
            // Split Q, K, V for this head: (SequenceLength x HeadDim)
            _Q_heads[h] = new Matrix(SequenceLength, HeadDim);
            _K_heads[h] = new Matrix(SequenceLength, HeadDim);
            _V_heads[h] = new Matrix(SequenceLength, HeadDim);

            for (int i = 0; i < SequenceLength; i++)
            {
                for (int j = 0; j < HeadDim; j++)
                {
                    _Q_heads[h][i, j] = Q[i, h * HeadDim + j];
                    _K_heads[h][i, j] = K[i, h * HeadDim + j];
                    _V_heads[h][i, j] = V[i, h * HeadDim + j];
                }
            }

            // Scores = (Q_head * K_head^T) * scale
            _Scores_heads[h] = (_Q_heads[h] * _K_heads[h].Transpose()) * scale;

            // Weights = Softmax(Scores)
            _Weights_heads[h] = Softmax.CalculateRowWise(_Scores_heads[h]);

            // Context = Weights * V_head
            _Context_heads[h] = _Weights_heads[h] * _V_heads[h];
        }

        // Concat context heads back to (SequenceLength x EmbedSize)
        _ConcatContext = new Matrix(SequenceLength, EmbedSize);
        for (int h = 0; h < HeadCount; h++)
        {
            for (int i = 0; i < SequenceLength; i++)
            {
                for (int j = 0; j < HeadDim; j++)
                {
                    _ConcatContext[i, h * HeadDim + j] = _Context_heads[h][i, j];
                }
            }
        }

        // Project output: ConcatContext * Wo
        Matrix projected = _ConcatContext * _Wo;

        // Flatten to 1D
        LastOutput = new Matrix(InputSize, 1);
        for (int i = 0; i < SequenceLength; i++)
            for (int j = 0; j < EmbedSize; j++)
                LastOutput[i * EmbedSize + j, 0] = projected[i, j];

        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // 1. Hatayı (error) 1D'den 2D matrise (SequenceLength x EmbedSize) çevir
        Matrix dProjected = new Matrix(SequenceLength, EmbedSize);
        for (int i = 0; i < SequenceLength; i++)
            for (int j = 0; j < EmbedSize; j++)
                dProjected[i, j] = error[i * EmbedSize + j, 0];

        // dWo = ConcatContext^T * dProjected
        Matrix gradWo = _ConcatContext.Transpose() * dProjected;

        // dConcatContext = dProjected * Wo^T
        Matrix dConcatContext = dProjected * _Wo.Transpose();

        // Heads gradients accumulation
        Matrix dQ = new Matrix(SequenceLength, EmbedSize);
        Matrix dK = new Matrix(SequenceLength, EmbedSize);
        Matrix dV = new Matrix(SequenceLength, EmbedSize);

        double scale = 1.0 / Math.Sqrt(HeadDim);

        for (int h = 0; h < HeadCount; h++)
        {
            // Split dConcatContext to head context gradient (SequenceLength x HeadDim)
            Matrix dContext_h = new Matrix(SequenceLength, HeadDim);
            for (int i = 0; i < SequenceLength; i++)
                for (int j = 0; j < HeadDim; j++)
                    dContext_h[i, j] = dConcatContext[i, h * HeadDim + j];

            // dV_head = Weights^T * dContext_h
            Matrix dV_h = _Weights_heads[h].Transpose() * dContext_h;

            // dWeights = dContext_h * V_head^T
            Matrix dWeights_h = dContext_h * _V_heads[h].Transpose();

            // Softmax derivative
            Matrix dScores_h = new Matrix(SequenceLength, SequenceLength);
            for (int r = 0; r < SequenceLength; r++)
            {
                double rowDot = 0;
                for (int c = 0; c < SequenceLength; c++)
                    rowDot += _Weights_heads[h][r, c] * dWeights_h[r, c];

                for (int c = 0; c < SequenceLength; c++)
                    dScores_h[r, c] = _Weights_heads[h][r, c] * (dWeights_h[r, c] - rowDot);
            }

            dScores_h = dScores_h * scale;

            // dQ_head = dScores * K_head
            Matrix dQ_h = dScores_h * _K_heads[h];

            // dK_head = dScores^T * Q_head
            Matrix dK_h = dScores_h.Transpose() * _Q_heads[h];

            // Assemble into full matrices
            for (int i = 0; i < SequenceLength; i++)
            {
                for (int j = 0; j < HeadDim; j++)
                {
                    dQ[i, h * HeadDim + j] = dQ_h[i, j];
                    dK[i, h * HeadDim + j] = dK_h[i, j];
                    dV[i, h * HeadDim + j] = dV_h[i, j];
                }
            }
        }

        // Wq, Wk, Wv Gradientleri
        Matrix gradWq = _InputReshaped.Transpose() * dQ;
        Matrix gradWk = _InputReshaped.Transpose() * dK;
        Matrix gradWv = _InputReshaped.Transpose() * dV;

        // Weights updates
        _Wq.SubtractInPlace(optimizer.ComputeUpdate(gradWq, _Wq));
        _Wk.SubtractInPlace(optimizer.ComputeUpdate(gradWk, _Wk));
        _Wv.SubtractInPlace(optimizer.ComputeUpdate(gradWv, _Wv));
        _Wo.SubtractInPlace(optimizer.ComputeUpdate(gradWo, _Wo));

        // Gradient back to input
        Matrix dInput = (dQ * _Wq.Transpose()) + (dK * _Wk.Transpose()) + (dV * _Wv.Transpose());

        // Flatten to 1D
        Matrix flattenedError = new Matrix(InputSize, 1);
        for (int i = 0; i < SequenceLength; i++)
            for (int j = 0; j < EmbedSize; j++)
                flattenedError[i * EmbedSize + j, 0] = dInput[i, j];

        return flattenedError;
    }
}

