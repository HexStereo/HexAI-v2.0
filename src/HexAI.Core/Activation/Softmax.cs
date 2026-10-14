using System;
using HexAI.Core.Math;

namespace HexAI.Core.Activation;

/// <summary>
/// Softmax aktivasyon fonksiyonu.
/// Çıktıları bir olasılık dağılımına dönüştürür (toplamları 1 olacak şekilde ayarlar).
/// Genellikle sınıflandırma (classification) katmanlarında veya Attention mekanizmasında kullanılır.
/// </summary>
public class Softmax : IActivation
{
    public string Name => "Softmax";

    // Softmax matris bazlı bir aktivasyon olduğu için tek bir double değer üzerinden 
    // çalışması tam doğru değildir, ama IActivation arayüzüne uyması için dummy bir işlem yapıyoruz.
    // Gerçek Softmax, VectorSoftmax statik metodu ile hesaplanmalıdır.
    public double Activate(double x)
    {
        return System.Math.Exp(x); // Gerçek normalizasyon matris boyutunda yapılmalı
    }

    public double Derivative(double activatedOutput)
    {
        return activatedOutput * (1.0 - activatedOutput);
    }

    /// <summary>
    /// Verilen bir matris veya vektör üzerinde Softmax hesaplar.
    /// (Sayısal denge - numerical stability - için en büyük değer çıkarılarak hesaplanır)
    /// </summary>
    public static Matrix Calculate(Matrix input)
    {
        var result = new Matrix(input.Rows, input.Cols);
        
        // Overflow'u engellemek için maksimum değeri bul
        double max = double.MinValue;
        for (int i = 0; i < input.Data.Length; i++)
        {
            if (input.Data[i] > max)
                max = input.Data[i];
        }

        double sum = 0;
        for (int i = 0; i < input.Data.Length; i++)
        {
            double val = System.Math.Exp(input.Data[i] - max);
            result.Data[i] = val;
            sum += val;
        }

        for (int i = 0; i < input.Data.Length; i++)
        {
            result.Data[i] /= sum;
        }

        return result;
    }

    /// <summary>
    /// Matrisin her bir satırı için ayrı ayrı Softmax hesaplar.
    /// (Self-Attention mekanizmasında Q*K^T sonuçlarında her kelimenin diğerlerine dikkatini normalize etmek için kullanılır)
    /// </summary>
    public static Matrix CalculateRowWise(Matrix input)
    {
        var result = new Matrix(input.Rows, input.Cols);
        
        for (int r = 0; r < input.Rows; r++)
        {
            int rowOffset = r * input.Cols;
            
            // Satırdaki max değeri bul
            double max = double.MinValue;
            for (int c = 0; c < input.Cols; c++)
            {
                if (input.Data[rowOffset + c] > max)
                    max = input.Data[rowOffset + c];
            }

            double sum = 0;
            for (int c = 0; c < input.Cols; c++)
            {
                double val = System.Math.Exp(input.Data[rowOffset + c] - max);
                result.Data[rowOffset + c] = val;
                sum += val;
            }

            for (int c = 0; c < input.Cols; c++)
            {
                result.Data[rowOffset + c] /= sum;
            }
        }

        return result;
    }
}
