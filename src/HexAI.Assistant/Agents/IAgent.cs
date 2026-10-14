using System.Threading.Tasks;

namespace HexAI.Assistant.Agents;

/// <summary>
/// Ajanların ortak sözleşmesini tanımlayan arayüz.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Ajanın görünen adı (Örn: "Yazılımcı", "Mimar")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Ajanın sistem rolü (Örn: "Sen bir uzman yazılımcısın. Verilen mimariyi C# koduna dönüştürürsün.")
    /// </summary>
    string RoleDescription { get; }

    /// <summary>
    /// Girdi mesajını işleyerek yanıt üretir.
    /// </summary>
    /// <param name="inputMessage">Önceki ajandan veya kullanıcıdan gelen girdi.</param>
    /// <returns>Ajanın ürettiği çıktı.</returns>
    Task<string> ProcessAsync(string inputMessage);
}
