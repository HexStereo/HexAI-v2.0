using HexAI.Assistant.Skills;

namespace HexAI.Assistant.Agents;

/// <summary>
/// HexAI Otonom Ajan (ReAct - Akıl Yürütme ve Eylem Döngüsü).
/// Dış kütüphaneye ihtiyaç duymadan kendi yerel araç kayıt defteri üzerinden:
///   1. Kullanıcının komutunu analiz eder.
///   2. İlgili aracı seçer ve çalıştırır.
///   3. Gözlem sonucunu değerlendirip yanıtı üretir.
/// </summary>
public class ReActAgent
{
    private readonly ToolRegistry _toolRegistry;

    /// <summary>Her adımda çağrılan callback.</summary>
    public Action<string, string>? OnStep { get; set; }

    public ReActAgent(ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
    }

    /// <summary>
    /// Kullanıcı mesajına uygun aracı belirler, çalıştırır ve sonucu döndürür.
    /// </summary>
    public async Task<string> RunAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var skill = _toolRegistry.FindByKeyword(userMessage);
        if (skill == null)
            return "İsteğinize uygun bir araç bulunamadı.";

        OnStep?.Invoke("🤔 Analiz", $"'{skill.Name}' aracı seçildi.");

        var parameters = new Dictionary<string, string>
        {
            ["input"] = userMessage,
            ["query"] = userMessage
        };

        var result = await skill.ExecuteAsync(userMessage, parameters);
        OnStep?.Invoke($"🔧 {skill.Name}", result.Response);

        return result.Response;
    }

    /// <summary>
    /// Adım adım akışlı (streaming) araç çalıştırma.
    /// </summary>
    public async IAsyncEnumerable<(string Step, string Content)> RunStreamAsync(string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return ("🤔 Analiz", "İstek inceleniyor ve uygun araç belirleniyor...");

        var skill = _toolRegistry.FindByKeyword(userMessage);
        if (skill == null)
        {
            yield return ("⚠️ Araç", "İsteğinizi yerine getirecek uygun bir araç eşleşmedi.");
            yield break;
        }

        yield return ($"🔧 {skill.Name}", "İşlem yürütülüyor...");

        var parameters = new Dictionary<string, string>
        {
            ["input"] = userMessage,
            ["query"] = userMessage
        };

        var result = await skill.ExecuteAsync(userMessage, parameters);

        yield return ($"✅ {skill.Name}", result.Response);
    }
}
