using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HexAI.Assistant.Skills;

/// <summary>
/// Sistem bilgisi sorgulama yeteneği.
/// Tarih, saat, disk alanı, RAM, CPU, bilgisayar adı vb. bilgileri sağlar.
/// </summary>
public class SystemInfoSkill : ISkill
{
    public string Name => "Sistem Bilgisi";
    public string Description => "Sistem bilgilerini sorgular (tarih, disk, RAM vb.)";
    public string[] TriggerKeywords => new[] { "saat", "tarih", "disk", "ram", "cpu", "sistem", "bilgisayar" };

    public async Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string lower = userInput.ToLower(new System.Globalization.CultureInfo("tr-TR"));

        // Hangi bilgi isteniyor?
        if (lower.Contains("saat") || lower.Contains("zaman"))
            return SkillResult.Ok(GetTimeInfo());

        if (lower.Contains("tarih") || lower.Contains("gün") || lower.Contains("bugün"))
            return SkillResult.Ok(GetDateInfo());

        if (lower.Contains("disk") || lower.Contains("alan") || lower.Contains("yer") || lower.Contains("depolama"))
            return SkillResult.Ok(GetDiskInfo());

        if (lower.Contains("ram") || lower.Contains("bellek") || lower.Contains("hafıza") || lower.Contains("memory"))
            return await GetMemoryInfoAsync();

        if (lower.Contains("cpu") || lower.Contains("işlemci") || lower.Contains("processor"))
            return await GetCpuInfoAsync();

        if (lower.Contains("süreç") || lower.Contains("process") || lower.Contains("çalışan"))
            return GetRunningProcesses();

        if (lower.Contains("ip") || lower.Contains("ağ") || lower.Contains("network"))
            return await GetNetworkInfoAsync();

        // Genel sistem bilgisi
        return SkillResult.Ok(GetFullSystemInfo());
    }

    private string GetTimeInfo()
    {
        var now = DateTime.Now;
        return $"🕐 Saat: {now:HH:mm:ss}\n📅 Tarih: {now:dd MMMM yyyy, dddd}";
    }

    private string GetDateInfo()
    {
        var now = DateTime.Now;
        var daysTurkish = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday] = "Pazartesi", [DayOfWeek.Tuesday] = "Salı",
            [DayOfWeek.Wednesday] = "Çarşamba", [DayOfWeek.Thursday] = "Perşembe",
            [DayOfWeek.Friday] = "Cuma", [DayOfWeek.Saturday] = "Cumartesi",
            [DayOfWeek.Sunday] = "Pazar"
        };

        var monthsTurkish = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

        return $"📅 Bugün: {now.Day} {monthsTurkish[now.Month]} {now.Year}, {daysTurkish[now.DayOfWeek]}\n🕐 Saat: {now:HH:mm}";
    }

    private string GetDiskInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("💾 Disk Bilgileri:\n");

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;

            double totalGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
            double freeGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            double usedGB = totalGB - freeGB;
            int usedPercent = (int)(usedGB / totalGB * 100);

            int barLen = 20;
            int filled = (int)(barLen * usedPercent / 100.0);
            string bar = new string('█', filled) + new string('░', barLen - filled);

            sb.AppendLine($"  {drive.Name} ({drive.DriveFormat})");
            sb.AppendLine($"    [{bar}] {usedPercent}%");
            sb.AppendLine($"    Kullanılan: {usedGB:F1} GB / Toplam: {totalGB:F1} GB / Boş: {freeGB:F1} GB");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<SkillResult> GetMemoryInfoAsync()
    {
        try
        {
            string output = await PowerShellSkill.RunPowerShellAsync(
                "Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize,FreePhysicalMemory | Format-List");

            // PowerShell çıktısını parse et
            long totalKB = 0, freeKB = 0;
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("TotalVisibleMemorySize"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1) long.TryParse(parts[1].Trim(), out totalKB);
                }
                else if (line.Contains("FreePhysicalMemory"))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1) long.TryParse(parts[1].Trim(), out freeKB);
                }
            }

            if (totalKB > 0)
            {
                double totalGB = totalKB / (1024.0 * 1024.0);
                double freeGB = freeKB / (1024.0 * 1024.0);
                double usedGB = totalGB - freeGB;
                int usedPercent = (int)(usedGB / totalGB * 100);

                int barLen = 20;
                int filled = (int)(barLen * usedPercent / 100.0);
                string bar = new string('█', filled) + new string('░', barLen - filled);

                return SkillResult.Ok(
                    $"🧠 RAM Kullanımı:\n" +
                    $"  [{bar}] {usedPercent}%\n" +
                    $"  Kullanılan: {usedGB:F1} GB / Toplam: {totalGB:F1} GB / Boş: {freeGB:F1} GB");
            }

            return SkillResult.Ok($"🧠 RAM Bilgisi:\n{output}");
        }
        catch (Exception ex)
        {
            return SkillResult.Fail($"RAM bilgisi alınamadı: {ex.Message}");
        }
    }

    private async Task<SkillResult> GetCpuInfoAsync()
    {
        try
        {
            string output = await PowerShellSkill.RunPowerShellAsync(
                "Get-CimInstance Win32_Processor | Select-Object Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed | Format-List");

            return SkillResult.Ok($"⚙️ İşlemci Bilgileri:\n{output}");
        }
        catch (Exception ex)
        {
            return SkillResult.Fail($"CPU bilgisi alınamadı: {ex.Message}");
        }
    }

    private SkillResult GetRunningProcesses()
    {
        var processes = Process.GetProcesses()
            .OrderByDescending(p =>
            {
                try { return p.WorkingSet64; }
                catch { return 0; }
            })
            .Take(15)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("🔄 En çok bellek kullanan süreçler:\n");
        sb.AppendLine($"  {"Süreç",-30} {"RAM",-12} {"PID",-8}");
        sb.AppendLine($"  {new string('─', 50)}");

        foreach (var p in processes)
        {
            try
            {
                double memMB = p.WorkingSet64 / (1024.0 * 1024.0);
                sb.AppendLine($"  {p.ProcessName,-30} {memMB,8:F1} MB  {p.Id,-8}");
            }
            catch { /* erişim izni olmayan süreçleri atla */ }
        }

        return SkillResult.Ok(sb.ToString());
    }

    private async Task<SkillResult> GetNetworkInfoAsync()
    {
        try
        {
            string output = await PowerShellSkill.RunPowerShellAsync(
                "Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -ne '127.0.0.1'} | Select-Object InterfaceAlias,IPAddress | Format-Table -AutoSize");

            return SkillResult.Ok($"🌐 Ağ Bilgileri:\n\n{output}");
        }
        catch (Exception ex)
        {
            return SkillResult.Fail($"Ağ bilgisi alınamadı: {ex.Message}");
        }
    }

    private string GetFullSystemInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("💻 Sistem Bilgileri:\n");
        sb.AppendLine($"  Bilgisayar Adı : {Environment.MachineName}");
        sb.AppendLine($"  Kullanıcı      : {Environment.UserName}");
        sb.AppendLine($"  İşletim Sistemi: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"  Mimari         : {RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"  .NET Sürümü    : {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"  İşlemci Sayısı : {Environment.ProcessorCount} çekirdek");
        sb.AppendLine($"  Çalışma Süresi : {Environment.TickCount64 / 1000 / 60 / 60} saat");
        sb.AppendLine();
        sb.AppendLine(GetDateInfo());
        sb.AppendLine();
        sb.AppendLine(GetDiskInfo());

        return sb.ToString();
    }
}

