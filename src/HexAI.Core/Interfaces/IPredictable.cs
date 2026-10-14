namespace HexAI.Core.Interfaces;

/// <summary>
/// Tahmin yapabilen model arayüzü.
/// Eğitilmiş bir modelin girdi alıp çıktı üretme sözleşmesi.
/// </summary>
public interface IPredictable
{
    /// <summary>
    /// Verilen girdiler için tahmin üretir.
    /// </summary>
    /// <param name="inputs">Girdi değerleri</param>
    /// <returns>Tahmin edilen çıktı değerleri</returns>
    double[] Predict(double[] inputs);
}
