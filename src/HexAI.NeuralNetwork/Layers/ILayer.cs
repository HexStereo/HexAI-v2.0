using HexAI.Core.Activation;
using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Sinir ağı katman arayüzü.
/// Her katman ileri (forward) ve geri (backward) yön hesaplamalarını destekler.
/// </summary>
public interface ILayer
{
    /// <summary>Katmandaki nöron sayısı.</summary>
    int NeuronCount { get; }

    /// <summary>Girdi boyutu.</summary>
    int InputSize { get; }

    /// <summary>Katmanın aktivasyon fonksiyonu.</summary>
    IActivation Activation { get; }

    /// <summary>Son ileri hesaplamanın çıktısı.</summary>
    Matrix LastOutput { get; }

    /// <summary>Son ileri hesaplamanın girdisi.</summary>
    Matrix LastInput { get; }

    /// <summary>
    /// İleri yön hesaplaması (forward pass).
    /// Girdi → Ağırlık çarpımı → Bias ekleme → Aktivasyon → Çıktı
    /// </summary>
    Matrix Forward(Matrix input);

    /// <summary>
    /// Geri yön hesaplaması (backward pass).
    /// Gradientleri hesaplar ve ağırlıkları günceller.
    /// </summary>
    /// <param name="outputError">Çıktı hatası (loss'un bu katmanın çıktısına göre türevi)</param>
    /// <param name="optimizer">Ağırlıkları güncelleyecek optimizasyon algoritması</param>
    /// <returns>Girdi hatası (bir önceki katmana gönderilecek)</returns>
    Matrix Backward(Matrix outputError, HexAI.NeuralNetwork.Optimizers.IOptimizer optimizer);
}
