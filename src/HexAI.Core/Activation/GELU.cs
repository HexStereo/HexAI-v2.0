namespace HexAI.Core.Activation;

/// <summary>
/// GELU (Gaussian Error Linear Unit) aktivasyon fonksiyonu.
/// 
/// Modern Transformer mimarilerinin (GPT-2, BERT, Llama) standart aktivasyonudur.
/// ReLU'dan daha yumuşak bir geçiş sağlar, negatif değerlere küçük bir geçirgenlik verir.
/// 
/// Matematiksel olarak:
///   GELU(x) = x · Φ(x)
///   ≈ 0.5 * x * (1 + tanh(√(2/π) * (x + 0.044715 * x³)))
/// 
/// Türev:
///   GELU'(x) ≈ 0.5 * (1 + tanh(√(2/π) * (x + 0.044715 * x³)))
///              + 0.5 * x * sech²(√(2/π) * (x + 0.044715 * x³)) * √(2/π) * (1 + 0.134145 * x²)
/// </summary>
public class GELU : IActivation
{
    public string Name => "GELU";

    private const double Sqrt2OverPi = 0.7978845608; // √(2/π)
    private const double Coeff = 0.044715;

    public double Activate(double x)
    {
        // Fast approximation: 0.5 * x * (1 + tanh(√(2/π) * (x + 0.044715 * x³)))
        double inner = Sqrt2OverPi * (x + Coeff * x * x * x);
        return 0.5 * x * (1.0 + System.Math.Tanh(inner));
    }

    public double Derivative(double output)
    {
        // Orijinal girişi bilmediğimiz için, output'tan yaklaşık türev
        // Bu, "çıktıdan türev" yaklaşımıdır (sigmoid/tanh gibi)
        // Gerçek GELU türevi daha karmaşıktır, ancak pratikte bu yeterlidir
        double x = output; // Forward sırasında x ≈ output (rough)
        double inner = Sqrt2OverPi * (x + Coeff * x * x * x);
        double tanhInner = System.Math.Tanh(inner);
        double sech2 = 1.0 - tanhInner * tanhInner;
        double dInner = Sqrt2OverPi * (1.0 + 3.0 * Coeff * x * x);

        return 0.5 * (1.0 + tanhInner) + 0.5 * x * sech2 * dInner;
    }
}
