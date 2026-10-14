namespace HexAI.Assistant.Skills;

/// <summary>
/// Tüm skill'leri merkezi olarak kaydeden ve yöneten kayıt defteri.
/// LLM'in tool calling ile doğru skill'i seçmesi için tool tanımları sağlar.
/// 
/// ReAct agent pattern'inde kullanılır:
///   1. LLM, kullanıcı mesajını analiz eder
///   2. Uygun tool'u seçer ve çağırır  
///   3. Tool sonucunu alır
///   4. Sonucu kullanıcıya özetler
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, ISkill> _skills = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Kayıtlı skill sayısı.</summary>
    public int Count => _skills.Count;

    /// <summary>Tüm kayıtlı skill'ler.</summary>
    public IReadOnlyDictionary<string, ISkill> Skills => _skills;

    /// <summary>
    /// Yeni bir skill kaydeder.
    /// </summary>
    public void Register(string name, ISkill skill)
    {
        _skills[name] = skill;
    }

    /// <summary>
    /// Skill'i ada göre getirir.
    /// </summary>
    public ISkill? Get(string name)
    {
        return _skills.GetValueOrDefault(name);
    }

    /// <summary>
    /// Anahtar kelimeye göre en uygun skill'i bulur.
    /// </summary>
    public ISkill? FindByKeyword(string keyword)
    {
        string lower = keyword.ToLower();
        foreach (var (name, skill) in _skills)
        {
            if (skill.TriggerKeywords.Any(k => lower.Contains(k.ToLower())))
                return skill;
        }
        return null;
    }

    /// <summary>
    /// LLM'e verilecek tool tanımlarını oluşturur.
    /// </summary>
    public List<ToolDefinition> GetToolDefinitions()
    {
        var definitions = new List<ToolDefinition>();

        foreach (var (name, skill) in _skills)
        {
            definitions.Add(new ToolDefinition
            {
                Name = name,
                Description = skill.Description,
                Parameters = new Dictionary<string, string>
                {
                    ["input"] = "Kullanıcının girdisi veya komutu",
                    ["parameters"] = "Ek parametreler (JSON formatında)"
                }
            });
        }

        return definitions;
    }

    /// <summary>
    /// Tool çağrısını parse eder ve çalıştırır.
    /// LLM çıktısından tool_call bloğunu arar.
    /// </summary>
    /// <param name="llmOutput">LLM'in ürettiği metin.</param>
    /// <returns>Tool çağrısı varsa sonucu, yoksa null.</returns>
    public async Task<ToolCallResult?> TryExecuteToolCallAsync(string llmOutput)
    {
        // <tool_call>...</tool_call> bloğunu ara
        int startTag = llmOutput.IndexOf("<tool_call>", StringComparison.OrdinalIgnoreCase);
        int endTag = llmOutput.IndexOf("</tool_call>", StringComparison.OrdinalIgnoreCase);

        if (startTag < 0 || endTag < 0 || endTag <= startTag)
            return null;

        string toolCallJson = llmOutput.Substring(startTag + 11, endTag - startTag - 11).Trim();

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(toolCallJson);
            var root = doc.RootElement;

            string toolName = root.GetProperty("name").GetString() ?? "";
            var arguments = new Dictionary<string, string>();

            if (root.TryGetProperty("arguments", out var args))
            {
                foreach (var prop in args.EnumerateObject())
                {
                    arguments[prop.Name] = prop.Value.ToString();
                }
            }

            // Skill'i bul ve çalıştır
            var skill = Get(toolName);
            if (skill == null)
            {
                return new ToolCallResult
                {
                    ToolName = toolName,
                    Success = false,
                    Output = $"Araç bulunamadı: {toolName}"
                };
            }

            string input = arguments.GetValueOrDefault("input", "");
            var result = await skill.ExecuteAsync(input, arguments);

            return new ToolCallResult
            {
                ToolName = toolName,
                Success = result.Success,
                Output = result.Response,
                RawOutput = result.RawOutput
            };
        }
        catch (Exception ex)
        {
            return new ToolCallResult
            {
                ToolName = "unknown",
                Success = false,
                Output = $"Tool çağrısı parse hatası: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Varsayılan skill'leri kaydeder.
    /// </summary>
    public void RegisterDefaults()
    {
        Register("dosya_listele", new FileSystemSkill());
        Register("powershell", new PowerShellSkill());
        Register("sistem_bilgi", new SystemInfoSkill());
        Register("matematik", new MathSkill());
        Register("uygulama_ac", new ProcessSkill());
        Register("kod_yaz", new CodingSkill());
        Register("web_ara", new WebSearchSkill());
        Register("web_oku", new WebReadSkill());
        Register("dosya_duzenle", new FileEditSkill());
    }

    /// <summary>Kayıtlı araçların listesini yazdırır.</summary>
    public string GetToolListText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🔧 Kayıtlı Araçlar:\n");
        foreach (var (name, skill) in _skills)
        {
            sb.AppendLine($"  • {name}: {skill.Description}");
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>Tool çağrısı sonucu.</summary>
public class ToolCallResult
{
    public string ToolName { get; set; } = "";
    public bool Success { get; set; }
    public string Output { get; set; } = "";
    public string? RawOutput { get; set; }
}

/// <summary>Araç tanımı — yapay zekanın hangi araçlara sahip olduğunu belirten yapı.</summary>
public class ToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Parameters { get; set; } = new();
}
