using HexAI.Core.Math;

namespace HexAI.NeuralNetwork.Layers;

/// <summary>
/// Katman ağırlıklarını serialize/deserialize edebilen katman arayüzü.
/// Model kaydetme/yükleme için kullanılır.
/// </summary>
public interface ISerializableLayer : ILayer
{
    /// <summary>Katmanın tüm ağırlıklarını düz dizi olarak döndürür.</summary>
    double[] GetWeights();

    /// <summary>Katmana ağırlıkları yükler.</summary>
    void SetWeights(double[] weights);

    /// <summary>Toplam ağırlık (parametre) sayısı.</summary>
    int ParameterCount { get; }
}
