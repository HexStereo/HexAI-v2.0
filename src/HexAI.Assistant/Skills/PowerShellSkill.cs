using System.Diagnostics;
using System.Text;

namespace HexAI.Assistant.Skills;

/// <summary>
/// PowerShell komutu çalıştırma yeteneği.
/// Kullanıcının istediği komutları güvenli bir şekilde çalıştırır.
/// Tehlikeli komutlar filtrelenir.
/// </summary>
public class PowerShellSkill : ISkill
{
    public string Name => "PowerShell";
    public string Description => "PowerShell komutu çalıştırır";
    public string[] TriggerKeywords => new[] { "komut", "çalıştır", "powershell", "terminal", "cmd" };

    // Tehlikeli komut kalıpları — bu komutlar için uyarı verilir
    private static readonly string[] DangerousPatterns = new[]
    {
        "remove-item", "rm ", "del ", "format", "clear-disk",
        "stop-computer", "restart-computer", "shutdown",
        "set-executionpolicy", "reg delete", "diskpart"
    };

    // Yasaklı komutlar — hiçbir koşulda çalıştırılmaz
    private static readonly string[] BlockedPatterns = new[]
    {
        "format c:", "format d:", "rm -rf /", ":(){ :|:& };:"
    };

    public async Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string command = parameters.ContainsKey("command") ? parameters["command"] : userInput;

        if (string.IsNullOrWhiteSpace(command))
        {
            return SkillResult.Fail("Çalıştırılacak komut belirtilmedi.");
        }

        string normalizedCmd = command.ToLower(new System.Globalization.CultureInfo("tr-TR"));

        // Yasaklı komut kontrolü
        foreach (var blocked in BlockedPatterns)
        {
            if (normalizedCmd.Contains(blocked))
            {
                return SkillResult.Fail($"Bu komut güvenlik nedeniyle engellenmiştir: {command}");
            }
        }

        // Tehlikeli komut uyarısı
        bool isDangerous = DangerousPatterns.Any(p => normalizedCmd.Contains(p));

        try
        {
            var output = await RunPowerShellAsync(command);

            string prefix = isDangerous ? "⚠️ [Dikkat: Sistem değiştiren komut]\n" : "";
            string response = $"{prefix}📟 PowerShell Çıktısı:\n```\n{output}\n```";

            if (string.IsNullOrWhiteSpace(output))
            {
                response = $"{prefix}✅ Komut başarıyla çalıştırıldı (çıktı yok).";
            }

            return SkillResult.Ok(response, output);
        }
        catch (Exception ex)
        {
            return SkillResult.Fail($"Komut çalıştırılamadı: {ex.Message}");
        }
    }

    /// <summary>
    /// PowerShell komutu çalıştırır ve çıktısını döndürür.
    /// </summary>
    public static async Task<string> RunPowerShellAsync(string command, int timeoutMs = 30000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command \"{command.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("PowerShell başlatılamadı.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        bool finished = process.WaitForExit(timeoutMs);

        if (!finished)
        {
            process.Kill();
            return "[Zaman aşımı: Komut 30 saniye içinde tamamlanmadı]";
        }

        string output = await outputTask;
        string error = await errorTask;

        if (!string.IsNullOrWhiteSpace(error) && string.IsNullOrWhiteSpace(output))
        {
            return $"[Hata]: {error}";
        }

        return string.IsNullOrWhiteSpace(output) ? "" : output.Trim();
    }
}

