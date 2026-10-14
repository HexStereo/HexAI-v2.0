namespace HexAI.Core.Math;

/// <summary>
/// Yardımcı matematik fonksiyonları.
/// Aktivasyon hesaplamaları, kayıp fonksiyonları ve genel yardımcılar.
/// </summary>
public static class MathHelper
{
    private static readonly Random _globalRng = new();

    /// <summary>
    /// Sigmoid fonksiyonu: 1 / (1 + e^(-x))
    /// Çıktıyı 0-1 arasına sıkıştırır. Olasılık çıktısı için idealdir.
    /// </summary>
    public static double Sigmoid(double x)
    {
        // Overflow koruması
        if (x > 500) return 1.0;
        if (x < -500) return 0.0;
        return 1.0 / (1.0 + System.Math.Exp(-x));
    }

    /// <summary>
    /// Sigmoid türevi: sigmoid(x) * (1 - sigmoid(x))
    /// Backpropagation'da gradient hesaplaması için kullanılır.
    /// NOT: Girdi olarak sigmoid'in çıktısını alır (zaten hesaplanmış).
    /// </summary>
    public static double SigmoidDerivative(double sigmoidOutput)
    {
        return sigmoidOutput * (1.0 - sigmoidOutput);
    }

    /// <summary>
    /// ReLU (Rectified Linear Unit): max(0, x)
    /// Modern sinir ağlarında en çok kullanılan aktivasyon.
    /// </summary>
    public static double ReLU(double x)
    {
        return System.Math.Max(0, x);
    }

    /// <summary>
    /// ReLU türevi: x > 0 ise 1, değilse 0
    /// </summary>
    public static double ReLUDerivative(double x)
    {
        return x > 0 ? 1.0 : 0.0;
    }

    /// <summary>
    /// Hiperbolik tanjant: (e^x - e^(-x)) / (e^x + e^(-x))
    /// Çıktıyı -1 ile 1 arasına sıkıştırır.
    /// </summary>
    public static double Tanh(double x)
    {
        return System.Math.Tanh(x);
    }

    /// <summary>
    /// Tanh türevi: 1 - tanh²(x)
    /// NOT: Girdi olarak tanh'ın çıktısını alır.
    /// </summary>
    public static double TanhDerivative(double tanhOutput)
    {
        return 1.0 - tanhOutput * tanhOutput;
    }

    /// <summary>
    /// Ortalama Kare Hatası (MSE — Mean Squared Error)
    /// Kayıp fonksiyonu olarak kullanılır: modelin ne kadar hatalı olduğunu ölçer.
    /// </summary>
    public static double MeanSquaredError(double[] predicted, double[] actual)
    {
        if (predicted.Length != actual.Length)
            throw new ArgumentException("Dizi uzunlukları eşit olmalı.");

        double sum = 0;
        for (int i = 0; i < predicted.Length; i++)
        {
            double diff = predicted[i] - actual[i];
            sum += diff * diff;
        }
        return sum / predicted.Length;
    }

    /// <summary>
    /// -1 ile 1 arasında rastgele sayı üretir.
    /// </summary>
    public static double RandomWeight(Random? rng = null)
    {
        rng ??= _globalRng;
        return rng.NextDouble() * 2.0 - 1.0;
    }

    /// <summary>
    /// Gaussian (normal) dağılımdan rastgele sayı üretir.
    /// Box-Muller dönüşümü kullanır.
    /// </summary>
    public static double RandomGaussian(double mean = 0, double stdDev = 1, Random? rng = null)
    {
        rng ??= _globalRng;
        // Box-Muller dönüşümü
        double u1 = 1.0 - rng.NextDouble(); // (0,1] aralığında
        double u2 = rng.NextDouble();
        double z = System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                   System.Math.Cos(2.0 * System.Math.PI * u2);
        return mean + stdDev * z;
    }

    /// <summary>
    /// Diziyi 0-1 arasına normalize eder (Min-Max normalizasyon).
    /// </summary>
    public static double[] Normalize(double[] data)
    {
        double min = data.Min();
        double max = data.Max();
        double range = max - min;

        if (range == 0) return data.Select(_ => 0.5).ToArray();

        return data.Select(x => (x - min) / range).ToArray();
    }

    /// <summary>
    /// Softmax fonksiyonu — çıktıları olasılık dağılımına dönüştürür.
    /// Çok sınıflı sınıflandırmada çıktı katmanında kullanılır.
    /// </summary>
    public static double[] Softmax(double[] input)
    {
        double max = input.Max(); // Overflow koruması
        double[] exps = input.Select(x => System.Math.Exp(x - max)).ToArray();
        double sum = exps.Sum();
        return exps.Select(e => e / sum).ToArray();
    }
}
