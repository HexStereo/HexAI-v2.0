namespace HexAI.Core.Data;

/// <summary>
/// Veri normalizasyon sınıfı.
/// Verinin belirli bir aralığa dönüştürülmesini sağlar.
/// Normalizasyon, sinir ağlarının daha hızlı ve kararlı öğrenmesi için kritiktir.
/// </summary>
public class DataNormalizer
{
    private double[]? _mins;
    private double[]? _maxs;
    private double[]? _means;
    private double[]? _stds;

    /// <summary>
    /// Min-Max normalizasyon: Veriyi [0, 1] aralığına ölçekler.
    /// Formül: (x - min) / (max - min)
    /// </summary>
    public double[][] MinMaxNormalize(double[][] data)
    {
        int features = data[0].Length;
        _mins = new double[features];
        _maxs = new double[features];

        // Her özellik için min/max bul
        for (int j = 0; j < features; j++)
        {
            _mins[j] = double.MaxValue;
            _maxs[j] = double.MinValue;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i][j] < _mins[j]) _mins[j] = data[i][j];
                if (data[i][j] > _maxs[j]) _maxs[j] = data[i][j];
            }
        }

        // Normalize et
        var result = new double[data.Length][];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = new double[features];
            for (int j = 0; j < features; j++)
            {
                double range = _maxs[j] - _mins[j];
                result[i][j] = range == 0 ? 0.5 : (data[i][j] - _mins[j]) / range;
            }
        }
        return result;
    }

    /// <summary>
    /// Z-Score normalizasyon (standardizasyon): Veriyi ortalama=0, standart sapma=1 olacak şekilde dönüştürür.
    /// Formül: (x - mean) / std
    /// </summary>
    public double[][] ZScoreNormalize(double[][] data)
    {
        int features = data[0].Length;
        _means = new double[features];
        _stds = new double[features];

        // Ortalama hesapla
        for (int j = 0; j < features; j++)
        {
            double sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum += data[i][j];
            _means[j] = sum / data.Length;
        }

        // Standart sapma hesapla
        for (int j = 0; j < features; j++)
        {
            double sumSq = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double diff = data[i][j] - _means[j];
                sumSq += diff * diff;
            }
            _stds[j] = System.Math.Sqrt(sumSq / data.Length);
        }

        // Normalize et
        var result = new double[data.Length][];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = new double[features];
            for (int j = 0; j < features; j++)
            {
                result[i][j] = _stds[j] == 0 ? 0 : (data[i][j] - _means[j]) / _stds[j];
            }
        }
        return result;
    }
}
