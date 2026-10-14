using HexAI.Core.Math;

namespace HexAI.Core.Loss;

/// <summary>
/// Ortalama Kare Hata (Mean Squared Error).
/// Regresyon problemleri veya sigmoid tabanlı basit ağlar için kullanılır.
/// </summary>
public class MSELoss : ILoss
{
    public double Calculate(Matrix predicted, Matrix expected)
    {
        double sum = 0;
        for (int i = 0; i < predicted.Rows; i++)
        {
            double diff = predicted[i, 0] - expected[i, 0];
            sum += diff * diff;
        }
        return sum / predicted.Rows; // Mean
    }

    public Matrix Derivative(Matrix predicted, Matrix expected)
    {
        // MSE türevi: 2 * (predicted - expected) / N
        // Optimizasyon algoritmalarında sabit 2/N genellikle Learning Rate içine yutulduğu için 
        // doğrudan (predicted - expected) döndürebiliriz.
        return predicted - expected;
    }
}
