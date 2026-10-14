namespace HexAI.Core.Interfaces;

/// <summary>
/// Bir modelin disk üzerine yazılabilmesi (Serialization) için durumunu
/// (state) dışarı aktarmasını ve içeri almasını sağlayan arayüz.
/// </summary>
/// <typeparam name="TDto">Modelin durumunu temsil eden Data Transfer Object (DTO) tipi.</typeparam>
public interface IExportableModel<TDto>
{
    /// <summary>
    /// Modelin mevcut durumunu (ağırlıklar, kurallar vb.) bir DTO'ya dönüştürerek döndürür.
    /// </summary>
    TDto ExportState();

    /// <summary>
    /// Verilen DTO'daki durumu (ağırlıklar, kurallar vb.) modele yükler.
    /// </summary>
    void ImportState(TDto state);
}
