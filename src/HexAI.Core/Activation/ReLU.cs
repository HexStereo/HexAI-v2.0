using HexAI.Core.Math;

namespace HexAI.Core.Activation;

/// <summary>
/// ReLU (Rectified Linear Unit) aktivasyon fonksiyonu: f(x) = max(0, x)
/// Çıktı aralığı: [0, +∞)
/// Kullanım: Gizli katmanlar için en yaygın tercih. Vanishing gradient sorununu çözer.
/// </summary>
public class ReLU : IActivation
{
    public string Name => "ReLU";

    public double Activate(double x) => MathHelper.ReLU(x);

    /// <summary>
    /// ReLU türevi — NOT: Burada orijinal girdi değil, aktivasyon çıktısı kullanılır.
    /// Çıktı > 0 ise türev 1, değilse 0.
    /// </summary>
    public double Derivative(double activatedOutput) =>
        activatedOutput > 0 ? 1.0 : 0.0;
}
