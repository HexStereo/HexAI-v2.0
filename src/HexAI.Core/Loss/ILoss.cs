using HexAI.Core.Math;

namespace HexAI.Core.Loss;

/// <summary>
/// Sinir ağlarında modelin tahminleri ile gerçek/beklenen değerler arasındaki hatayı hesaplayan temel arayüz.
/// </summary>
public interface ILoss
{
    /// <summary>
    /// Kayıp değerini hesaplar.
    /// </summary>
    double Calculate(Matrix predicted, Matrix expected);

    /// <summary>
    /// Kayıp fonksiyonunun tahminlere göre türevini hesaplar.
    /// Bu değer backpropagation başlangıç hatası olarak kullanılır.
    /// </summary>
    Matrix Derivative(Matrix predicted, Matrix expected);
}
