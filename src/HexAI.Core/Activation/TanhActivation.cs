using HexAI.Core.Math;

namespace HexAI.Core.Activation;

/// <summary>
/// Hiperbolik Tanjant aktivasyon fonksiyonu: f(x) = tanh(x)
/// Çıktı aralığı: (-1, 1)
/// Kullanım: Sigmoid'e alternatif, sıfır merkezli çıktı verir.
/// RNN ve LSTM ağlarında yaygın kullanılır.
/// </summary>
public class TanhActivation : IActivation
{
    public string Name => "Tanh";

    public double Activate(double x) => MathHelper.Tanh(x);

    public double Derivative(double activatedOutput) =>
        MathHelper.TanhDerivative(activatedOutput);
}
