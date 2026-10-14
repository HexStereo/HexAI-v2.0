using System.Collections.Generic;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Optimizers;

/// <summary>
/// Adaptive Moment Estimation (Adam) Eniyileyicisi.
/// Hem Momentum (hız) hem de RMSprop (hız ivmesi) tekniklerini birleştirerek
/// Derin Sinir Ağlarının en verimli şekilde eğitilmesini sağlayan modern standarttır.
/// </summary>
public class AdamOptimizer : IOptimizer
{
    public double LearningRate { get; set; }
    public double Beta1 { get; }
    public double Beta2 { get; }
    public double Epsilon { get; }

    // Her bir ağırlık matrisi için durumu (m ve v) ayrı tutmalıyız
    private Dictionary<Matrix, Matrix> _m = new();
    private Dictionary<Matrix, Matrix> _v = new();
    
    // Her ağırlık matrisi için kendi iterasyon (t) sayısı
    private Dictionary<Matrix, int> _t = new();

    public AdamOptimizer(double learningRate = 0.001, double beta1 = 0.9, double beta2 = 0.999, double epsilon = 1e-8)
    {
        LearningRate = learningRate;
        Beta1 = beta1;
        Beta2 = beta2;
        Epsilon = epsilon;
    }

    public Matrix ComputeUpdate(Matrix gradient, Matrix weightReference)
    {
        if (!_m.ContainsKey(weightReference))
        {
            _m[weightReference] = new Matrix(gradient.Rows, gradient.Cols);
            _v[weightReference] = new Matrix(gradient.Rows, gradient.Cols);
            _t[weightReference] = 0;
        }

        Matrix m = _m[weightReference];
        Matrix v = _v[weightReference];
        int t = _t[weightReference] + 1;
        _t[weightReference] = t;

        // g_t^2 hesaplama
        Matrix gradientSquared = gradient.HadamardProduct(gradient);

        // m_t = Beta1 * m_{t-1} + (1 - Beta1) * g_t
        m = (m * Beta1) + (gradient * (1.0 - Beta1));
        
        // v_t = Beta2 * v_{t-1} + (1 - Beta2) * g_t^2
        v = (v * Beta2) + (gradientSquared * (1.0 - Beta2));

        // Durumları kaydet
        _m[weightReference] = m;
        _v[weightReference] = v;

        // Bias correction
        double beta1_t = 1.0 - System.Math.Pow(Beta1, t);
        double beta2_t = 1.0 - System.Math.Pow(Beta2, t);

        Matrix m_hat = m * (1.0 / beta1_t);
        Matrix v_hat = v * (1.0 / beta2_t);

        // Update = LearningRate * m_hat / (sqrt(v_hat) + epsilon)
        // Optimizasyon için Map fonksiyonu kullanalım
        Matrix update = new Matrix(gradient.Rows, gradient.Cols);
        for(int i = 0; i < update.Data.Length; i++)
        {
            update.Data[i] = (LearningRate * m_hat.Data[i]) / (System.Math.Sqrt(v_hat.Data[i]) + Epsilon);
        }

        return update;
    }
}
