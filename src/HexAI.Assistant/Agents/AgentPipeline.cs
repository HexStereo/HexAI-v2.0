using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HexAI.Assistant.Agents;

/// <summary>
/// Ajanları sıralı olarak (pipeline) çalıştıran orkestratör.
/// Birinin çıktısını diğerine girdi olarak verir.
/// </summary>
public class AgentPipeline
{
    private readonly List<IAgent> _agents = new();

    /// <summary>
    /// Pipeline'a yeni bir ajan ekler.
    /// </summary>
    public void AddAgent(IAgent agent)
    {
        _agents.Add(agent);
    }

    /// <summary>
    /// Pipeline'ı çalıştırır.
    /// </summary>
    /// <param name="initialInput">İlk ajana verilecek görev veya mesaj.</param>
    /// <param name="onStepCompleted">Her adım tamamlandığında çağrılacak opsiyonel geri arama (Ajan Adı, Üretilen Çıktı).</param>
    /// <returns>Son ajanın ürettiği nihai çıktı.</returns>
    public async Task<string> RunAsync(string initialInput, Action<string, string>? onStepCompleted = null)
    {
        if (_agents.Count == 0)
            return initialInput;

        string currentData = initialInput;

        foreach (var agent in _agents)
        {
            // Ajan mesajı işler
            currentData = await agent.ProcessAsync(currentData);
            
            // Eğer bir callback tanımlandıysa çağır (örn. konsola yazdırmak için)
            onStepCompleted?.Invoke(agent.Name, currentData);
        }

        return currentData;
    }
}
