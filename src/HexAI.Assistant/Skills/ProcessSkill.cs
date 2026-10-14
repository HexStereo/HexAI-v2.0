using System.Diagnostics;
using System.Text;

namespace HexAI.Assistant.Skills;

/// <summary>
/// Uygulama/program çalıştırma yeteneği.
/// Kullanıcının belirttiği uygulamaları başlatır.
/// </summary>
public class ProcessSkill : ISkill
{
    public string Name => "Program Yönetimi";
    public string Description => "Uygulamaları başlatır ve süreçleri yönetir";
    public string[] TriggerKeywords => new[] { "aç", "başlat", "program", "uygulama", "kapat" };

    // Bilinen uygulama kısayolları
    private static readonly Dictionary<string, string> KnownApps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["notepad"] = "notepad.exe",
        ["not defteri"] = "notepad.exe",
        ["hesap makinesi"] = "calc.exe",
        ["calculator"] = "calc.exe",
        ["calc"] = "calc.exe",
        ["chrome"] = "chrome",
        ["firefox"] = "firefox",
        ["explorer"] = "explorer.exe",
        ["dosya gezgini"] = "explorer.exe",
        ["tarayıcı"] = "chrome",
        ["paint"] = "mspaint.exe",
        ["word"] = "winword.exe",
        ["excel"] = "excel.exe",
        ["cmd"] = "cmd.exe",
        ["powershell"] = "powershell.exe",
        ["görev yöneticisi"] = "taskmgr.exe",
        ["task manager"] = "taskmgr.exe",
        ["denetim masası"] = "control.exe",
        ["ayarlar"] = "ms-settings:",
        ["vscode"] = "code",
        ["code"] = "code",
        ["spotify"] = "spotify",
    };

    public Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string lower = userInput.ToLower(new System.Globalization.CultureInfo("tr-TR"));

        // Bilinen uygulamaları kontrol et
        foreach (var (key, exe) in KnownApps)
        {
            if (lower.Contains(key))
            {
                return LaunchApp(exe, key);
            }
        }

        // Genel program adı çıkarma
        string appName = ExtractAppName(lower);
        if (!string.IsNullOrWhiteSpace(appName))
        {
            return LaunchApp(appName, appName);
        }

        return Task.FromResult(SkillResult.Fail("Hangi uygulamayı açmamı istediğini anlayamadım. Lütfen program adını belirt."));
    }

    private Task<SkillResult> LaunchApp(string executable, string displayName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true
            };

            Process.Start(psi);
            return Task.FromResult(SkillResult.Ok($"🚀 '{displayName}' başlatıldı!"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail($"'{displayName}' başlatılamadı: {ex.Message}"));
        }
    }

    private string ExtractAppName(string input)
    {
        // "xxx aç", "xxx başlat", "xxx çalıştır" kalıplarından program adını çıkar
        string[] triggers = { " aç", " başlat", " çalıştır", " açsana", " açabilir misin" };
        foreach (var trigger in triggers)
        {
            int idx = input.IndexOf(trigger);
            if (idx > 0)
            {
                string before = input[..idx].Trim();
                // Son kelimeyi al (program adı büyük ihtimalle)
                var words = before.Split(' ');
                return words[^1];
            }
        }
        return "";
    }
}

