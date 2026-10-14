using System;
using HexAI.Core.Math;
using HexAI.Core.Activation;

namespace HexAI.Core.Loss;

/// <summary>
/// Cross-Entropy Loss (Çapraz Entropi Kaybı).
/// Özellikle Sınıflandırma ve Dil Modeli (Dil Üretimi) problemleri için standarttır.
/// Doğrudan "Logit" (Aktivasyondan geçmemiş, Softmax öncesi ham değerler) aldığını varsayar 
/// ve kendi içinde Softmax uygular. Bu hem sayısal kararlılığı artırır hem de türevi basitleştirir.
/// </summary>
public class CrossEntropyLoss : ILoss
{
    private readonly int _vocabSize;
    private Matrix? _cachedSoftmax; // Geri yayılım (Backprop) için son Softmax değerlerini tutar

    public CrossEntropyLoss(int vocabSize)
    {
        _vocabSize = vocabSize;
    }

    public double Calculate(Matrix logits, Matrix expected)
    {
        int seqLen = logits.Rows / _vocabSize;
        if (logits.Rows % _vocabSize != 0)
        {
            throw new ArgumentException("Logits boyutu VocabSize'ın tam katı olmalıdır.");
        }

        _cachedSoftmax = new Matrix(logits.Rows, 1);
        double totalLoss = 0;

        for (int i = 0; i < seqLen; i++)
        {
            int offset = i * _vocabSize;
            
            // Satır içi (tek token için) maksimum değeri bul
            double maxLogit = double.MinValue;
            for (int v = 0; v < _vocabSize; v++)
            {
                if (logits[offset + v, 0] > maxLogit)
                    maxLogit = logits[offset + v, 0];
            }

            // Softmax hesapla
            double sumExp = 0;
            for (int v = 0; v < _vocabSize; v++)
            {
                double expVal = System.Math.Exp(logits[offset + v, 0] - maxLogit);
                _cachedSoftmax[offset + v, 0] = expVal;
                sumExp += expVal;
            }

            // Olasılıklara çevir ve log loss hesapla
            for (int v = 0; v < _vocabSize; v++)
            {
                double prob = _cachedSoftmax[offset + v, 0] / sumExp;
                _cachedSoftmax[offset + v, 0] = prob; // Cache update

                // Sadece expected == 1.0 olan yerlerde loss hesaplanır (One-hot encoding)
                if (expected[offset + v, 0] > 0.5)
                {
                    totalLoss += -System.Math.Log(prob + 1e-9);
                }
            }
        }

        return totalLoss / seqLen; // Sekans uzunluğuna göre ortalama
    }

    public Matrix Derivative(Matrix logits, Matrix expected)
    {
        if (_cachedSoftmax == null)
            throw new InvalidOperationException("Önce Calculate metodu çağrılmalıdır.");

        // Softmax ile Cross-Entropy türevi çok basittir: (PredictedProbabilities - Expected)
        // Softmax zaten Calculate içinde hesaplandı ve _cachedSoftmax içine atıldı.
        
        var gradient = new Matrix(logits.Rows, 1);
        int seqLen = logits.Rows / _vocabSize;

        for (int i = 0; i < logits.Rows; i++)
        {
            // Ortalamayı almak için seqLen'e bölüyoruz
            gradient[i, 0] = (_cachedSoftmax[i, 0] - expected[i, 0]) / seqLen;
        }

        return gradient;
    }
}
