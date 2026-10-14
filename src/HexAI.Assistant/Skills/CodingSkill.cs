using System.Text;

namespace HexAI.Assistant.Skills;

/// <summary>
/// Kod yazma, açıklama, düzenleme ve debug yeteneği.
/// LLM ile entegre çalışarak kaynak kod dosyaları oluşturur ve düzenler.
/// </summary>
public class CodingSkill : ISkill
{
    public string Name => "Kod Yazma";
    public string Description => "Kod yazar, açıklar, düzenler ve debug eder. Dosya oluşturma destekler.";
    public string[] TriggerKeywords => new[]
    {
        "kod yaz", "fonksiyon yaz", "sınıf yaz", "class yaz", "function yaz",
        "kod oluştur", "script yaz", "program yaz", "kod düzenle", "refactor",
        "debug", "hata bul", "kod açıkla", "nasıl yazılır", "örnek kod",
        "algoritma", "veri yapısı", "api", "endpoint"
    };

    public async Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string action = parameters.GetValueOrDefault("action", "write");
        string language = parameters.GetValueOrDefault("language", DetectLanguage(userInput));
        string filePath = parameters.GetValueOrDefault("path", "");

        return action.ToLower() switch
        {
            "write" => await WriteCodeAsync(userInput, language, filePath),
            "explain" => await ExplainCodeAsync(userInput, filePath),
            "create_file" => await CreateFileAsync(filePath, userInput),
            "review" => await ReviewCodeAsync(filePath),
            _ => await WriteCodeAsync(userInput, language, filePath)
        };
    }

    private Task<SkillResult> WriteCodeAsync(string description, string language, string filePath)
    {
        // Bu skill, LLM tarafından çağrılır ve cevap LLM'den gelir.
        // Burada sadece formatlamayı ve dosya yazma işlemini yönetiyoruz.
        var sb = new StringBuilder();
        sb.AppendLine($"💻 Kod Üretimi");
        sb.AppendLine($"  Dil: {language}");
        sb.AppendLine($"  İstek: {description}");

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            sb.AppendLine($"  Dosya: {filePath}");
        }

        sb.AppendLine();
        sb.AppendLine("⚙️ LLM bu isteği işleyecek ve kod üretecek.");

        return Task.FromResult(SkillResult.Ok(sb.ToString()));
    }

    private Task<SkillResult> ExplainCodeAsync(string code, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            code = File.ReadAllText(filePath);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(SkillResult.Fail("Açıklanacak kod bulunamadı."));
        }

        return Task.FromResult(SkillResult.Ok(
            $"📝 Kod Açıklama İsteği:\n```\n{code[..Math.Min(code.Length, 2000)]}\n```\n\n⚙️ LLM bu kodu analiz edecek.",
            code));
    }

    private Task<SkillResult> CreateFileAsync(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(SkillResult.Fail("Dosya yolu belirtilmedi."));
        }

        try
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (dir != null) Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, content);
            return Task.FromResult(SkillResult.Ok($"✅ Dosya oluşturuldu: {filePath}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail($"Dosya oluşturulamadı: {ex.Message}"));
        }
    }

    private Task<SkillResult> ReviewCodeAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return Task.FromResult(SkillResult.Fail($"Dosya bulunamadı: {filePath}"));
        }

        string code = File.ReadAllText(filePath);
        return Task.FromResult(SkillResult.Ok(
            $"🔍 Kod İnceleme: {Path.GetFileName(filePath)}\n```\n{code[..Math.Min(code.Length, 3000)]}\n```\n\n⚙️ LLM bu kodu inceleyecek.",
            code));
    }

    /// <summary>Girdi metninden programlama dilini tahmin eder.</summary>
    private static string DetectLanguage(string input)
    {
        string lower = input.ToLower();
        if (lower.Contains("c#") || lower.Contains("csharp")) return "C#";
        if (lower.Contains("python") || lower.Contains("py")) return "Python";
        if (lower.Contains("javascript") || lower.Contains("js")) return "JavaScript";
        if (lower.Contains("typescript") || lower.Contains("ts")) return "TypeScript";
        if (lower.Contains("java ") || lower.Contains("java.")) return "Java";
        if (lower.Contains("rust")) return "Rust";
        if (lower.Contains("go ") || lower.Contains("golang")) return "Go";
        if (lower.Contains("html")) return "HTML";
        if (lower.Contains("css")) return "CSS";
        if (lower.Contains("sql")) return "SQL";
        if (lower.Contains("bash") || lower.Contains("shell")) return "Bash";
        return "C#"; // Varsayılan
    }
}
