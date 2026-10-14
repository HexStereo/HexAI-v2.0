using System;
using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Transformer mimarisinin kalbi olan Self-Attention (Öz-Dikkat) katmanı.
/// Girdideki sekansın (cümle vb.) elemanlarının birbirleriyle olan ilişkisini,
/// Query (Sorgu), Key (Anahtar) ve Value (Değer) matrisleriyle çözer.
/// </summary>
public class AttentionLayer : ILayer
{
    public int SequenceLength { get; }
    public int EmbedSize { get; }
    
    // ILayer arayüzü tek boyutlu dizilerle haberleştiği için Input/Output boyutumuz 
    // Sekans_Uzunluğu * Gömülü_Vektör_Boyutu şeklindedir.
    public int InputSize => SequenceLength * EmbedSize;
    public int NeuronCount => SequenceLength * EmbedSize;
    
    public IActivation Activation { get; } // Dummy, Softmax içsel olarak kullanılıyor

    public Matrix LastInput { get; private set; } = new Matrix(1, 1);
    public Matrix LastOutput { get; private set; } = new Matrix(1, 1);

    // Q, K, V Ağırlıkları (Wq, Wk, Wv)
    private Matrix _Wq;
    private Matrix _Wk;
    private Matrix _Wv;

    // Geri yayılım için ara değerler
    private Matrix _Q = new Matrix(1,1);
    private Matrix _K = new Matrix(1,1);
    private Matrix _V = new Matrix(1,1);
    private Matrix _Scores = new Matrix(1,1);
    private Matrix _Weights = new Matrix(1,1); // Softmax(Scores)
    private Matrix _InputReshaped = new Matrix(1,1);

    public AttentionLayer(int sequenceLength, int embedSize, Random? rng = null)
    {
        SequenceLength = sequenceLength;
        EmbedSize = embedSize;
        Activation = new Linear(); // Dışarıya lineer veriyoruz, asıl işlem içeride Softmax ile

        rng ??= new Random();
        _Wq = Matrix.Random(EmbedSize, EmbedSize, rng);
        _Wk = Matrix.Random(EmbedSize, EmbedSize, rng);
        _Wv = Matrix.Random(EmbedSize, EmbedSize, rng);
    }

    public Matrix Forward(Matrix input)
    {
        LastInput = input;
        
        // Gelen 1D (SequenceLength * EmbedSize) x 1 vektörü, SequenceLength x EmbedSize matrisine çevir (Reshape)
        _InputReshaped = new Matrix(SequenceLength, EmbedSize);
        for (int i = 0; i < SequenceLength; i++)
        {
            for (int j = 0; j < EmbedSize; j++)
            {
                _InputReshaped[i, j] = input[i * EmbedSize + j, 0];
            }
        }

        // Q = X * Wq
        _Q = _InputReshaped * _Wq;
        
        // K = X * Wk
        _K = _InputReshaped * _Wk;
        
        // V = X * Wv
        _V = _InputReshaped * _Wv;

        // Attention Scores = (Q * K^T) / sqrt(d_k)
        double scale = 1.0 / System.Math.Sqrt(EmbedSize);
        _Scores = (_Q * _K.Transpose()) * scale;

        // Weights = Softmax(Scores) -> Her satır için (Kelimenin diğer kelimelere olan dikkati)
        _Weights = Softmax.CalculateRowWise(_Scores);

        // Output = Weights * V
        Matrix context = _Weights * _V;

        // Çıktıyı tekrar 1D vektöre Flattening yap (SequenceLength * EmbedSize) x 1
        LastOutput = new Matrix(InputSize, 1);
        for (int i = 0; i < SequenceLength; i++)
        {
            for (int j = 0; j < EmbedSize; j++)
            {
                LastOutput[i * EmbedSize + j, 0] = context[i, j];
            }
        }

        return LastOutput;
    }

    public Matrix Backward(Matrix error, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer)
    {
        // 1. Hatayı (error) 1D'den 2D matrise (SequenceLength x EmbedSize) çevir
        Matrix dOutput = new Matrix(SequenceLength, EmbedSize);
        for (int i = 0; i < SequenceLength; i++)
        {
            for (int j = 0; j < EmbedSize; j++)
            {
                dOutput[i, j] = error[i * EmbedSize + j, 0];
            }
        }

        // dV = Weights^T * dOutput
        Matrix dV = _Weights.Transpose() * dOutput;

        // dWeights = dOutput * V^T
        Matrix dWeights = dOutput * _V.Transpose();

        // Geriye doğru Softmax türevi (Yaklaşık formül - Exact Chain Rule for row-wise softmax)
        Matrix dScores = new Matrix(_Scores.Rows, _Scores.Cols);
        for (int r = 0; r < SequenceLength; r++)
        {
            double rowDot = 0;
            for (int c = 0; c < SequenceLength; c++)
            {
                rowDot += _Weights[r, c] * dWeights[r, c];
            }
            
            for (int c = 0; c < SequenceLength; c++)
            {
                dScores[r, c] = _Weights[r, c] * (dWeights[r, c] - rowDot);
            }
        }

        // Scale factor türevi
        dScores = dScores * (1.0 / System.Math.Sqrt(EmbedSize));

        // dQ = dScores * K
        Matrix dQ = dScores * _K;

        // dK = dScores^T * Q
        Matrix dK = dScores.Transpose() * _Q;

        // Wq, Wk, Wv Gradientleri
        Matrix gradWq = _InputReshaped.Transpose() * dQ;
        Matrix gradWk = _InputReshaped.Transpose() * dK;
        Matrix gradWv = _InputReshaped.Transpose() * dV;

        // Ağırlıkları güncelle
        _Wq.SubtractInPlace(optimizer.ComputeUpdate(gradWq, _Wq));
        _Wk.SubtractInPlace(optimizer.ComputeUpdate(gradWk, _Wk));
        _Wv.SubtractInPlace(optimizer.ComputeUpdate(gradWv, _Wv));

        // Önceki katmana iletilecek hata: dInput = dQ*Wq^T + dK*Wk^T + dV*Wv^T
        Matrix dInput = (dQ * _Wq.Transpose()) + (dK * _Wk.Transpose()) + (dV * _Wv.Transpose());

        // dInput'u 1D vektöre çevir
        Matrix flattenedError = new Matrix(InputSize, 1);
        for (int i = 0; i < SequenceLength; i++)
        {
            for (int j = 0; j < EmbedSize; j++)
            {
                flattenedError[i * EmbedSize + j, 0] = dInput[i, j];
            }
        }

        return flattenedError;
    }
}

