namespace HexAI.Core.Activation;

/// <summary>
/// Aktivasyon fonksiyonu arayüzü.
/// Her aktivasyon fonksiyonu hem ileri (forward) hem geri (backward) yön
/// hesaplamasını desteklemelidir.
/// </summary>
public interface IActivation
{
    /// <summary>Aktivasyon fonksiyonunun adı.</summary>
    string Name { get; }

    /// <summary>
    /// İleri yön: f(x)
    /// </summary>
    double Activate(double x);

    /// <summary>
    /// Geri yön (türev): f'(x)
    /// NOT: Girdi olarak Activate'in çıktısını alır (performans optimizasyonu).
    /// </summary>
    double Derivative(double activatedOutput);
}
