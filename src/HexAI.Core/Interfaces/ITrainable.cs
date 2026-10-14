using HexAI.Core.Data;

namespace HexAI.Core.Interfaces;

/// <summary>
/// Eğitilebilir model arayüzü.
/// Tüm AI modellerinin (sinir ağı, karar ağacı, regresyon vb.)
/// ortak eğitim sözleşmesini tanımlar.
/// </summary>
public interface ITrainable
{
    /// <summary>
    /// Modeli verilen veri seti ile eğitir.
    /// </summary>
    /// <param name="dataSet">Eğitim veri seti</param>
    /// <param name="epochs">Eğitim tekrar sayısı</param>
    /// <param name="learningRate">Öğrenme oranı</param>
    void Train(DataSet dataSet, int epochs, double learningRate);
}
