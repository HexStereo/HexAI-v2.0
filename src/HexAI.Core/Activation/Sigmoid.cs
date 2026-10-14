using HexAI.Core.Math;

namespace HexAI.Core.Activation;

/// <summary>
/// Sigmoid aktivasyon fonksiyonu: f(x) = 1 / (1 + e^(-x))
/// Çıktı aralığı: (0, 1)
/// Kullanım: İkili sınıflandırma çıktı katmanı, olasılık tahmini.
/// </summary>
public class Sigmoid : IActivation
{
    public string Name => "Sigmoid";

    public double Activate(double x) => MathHelper.Sigmoid(x);

    public double Derivative(double activatedOutput) =>
        MathHelper.SigmoidDerivative(activatedOutput);
}
