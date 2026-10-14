namespace HexAI.NeuralNetwork.Optimizers;

using HexAI.Core.Math;

/// <summary>
/// Ağ ağırlıklarının nasıl güncelleneceğini belirleyen eniyileyici (Optimizer) arayüzü.
/// </summary>
public interface IOptimizer
{
    /// <summary>
    /// Öğrenme Hızı (Learning Rate)
    /// </summary>
    double LearningRate { get; set; }

    /// <summary>
    /// Geri yayılım (Backpropagation) sırasında hesaplanan ham gradyanı (gradient) alır,
    /// seçili algoritmaya (Örn: Adam, RMSprop, SGD) göre optimize edilmiş güncelleme miktarını döndürür.
    /// </summary>
    /// <param name="gradient">Ham hata gradyanı</param>
    /// <param name="weightReference">Hangi ağırlık matrisi güncelleniyorsa onun referansı (Durum tutmak için)</param>
    /// <returns>Ağırlıktan çıkarılacak olan nihai delta (adım)</returns>
    Matrix ComputeUpdate(Matrix gradient, Matrix weightReference);
}
