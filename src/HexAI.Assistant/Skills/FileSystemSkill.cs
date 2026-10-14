using System.Text;

namespace HexAI.Assistant.Skills;

/// <summary>
/// Dosya sistemi işlemleri yeteneği.
/// Dosya/klasör listeleme, okuma, oluşturma ve arama işlemlerini yönetir.
/// </summary>
public class FileSystemSkill : ISkill
{
    public string Name => "Dosya Sistemi";
    public string Description => "Dosya ve klasör işlemleri yapar";
    public string[] TriggerKeywords => new[] { "dosya", "klasör", "listele", "oku", "yaz", "aç" };

    public Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string action = parameters.GetValueOrDefault("action", "list");
        string path = parameters.GetValueOrDefault("path", "");

        // Yol belirtilmediyse kullanıcı girdisinden akıllı çıkarım yap
        if (string.IsNullOrWhiteSpace(path))
        {
            path = ExtractPathFromInput(userInput);
        }

        return action.ToLower() switch
        {
            "list" => ListDirectory(path),
            "read" => ReadFile(path),
            "search" => SearchFiles(path, parameters.GetValueOrDefault("pattern", "*")),
            _ => ListDirectory(path)
        };
    }

    /// <summary>
    /// Klasör içeriğini listeler.
    /// </summary>
    private Task<SkillResult> ListDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        // Özel klasör adları
        path = ResolveSpecialPath(path);

        if (!Directory.Exists(path))
        {
            return Task.FromResult(SkillResult.Fail($"Klasör bulunamadı: {path}"));
        }

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📁 {path}\n");

            // Klasörler
            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs.Take(30))
            {
                string dirName = Path.GetFileName(dir);
                sb.AppendLine($"  📁 {dirName}/");
            }

            // Dosyalar
            var files = Directory.GetFiles(path);
            foreach (var file in files.Take(30))
            {
                string fileName = Path.GetFileName(file);
                long size = new FileInfo(file).Length;
                string sizeStr = FormatFileSize(size);
                string icon = GetFileIcon(fileName);
                sb.AppendLine($"  {icon} {fileName} ({sizeStr})");
            }

            if (dirs.Length + files.Length == 0)
            {
                sb.AppendLine("  (boş klasör)");
            }

            int total = dirs.Length + files.Length;
            if (total > 30)
            {
                sb.AppendLine($"\n  ... ve {total - 30} öğe daha");
            }

            sb.AppendLine($"\n  Toplam: {dirs.Length} klasör, {files.Length} dosya");

            return Task.FromResult(SkillResult.Ok(sb.ToString()));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(SkillResult.Fail($"Erişim reddedildi: {path}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Metin dosyasını okur.
    /// </summary>
    private Task<SkillResult> ReadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(SkillResult.Fail("Dosya yolu belirtilmedi."));
        }

        path = ResolveSpecialPath(path);

        if (!File.Exists(path))
        {
            return Task.FromResult(SkillResult.Fail($"Dosya bulunamadı: {path}"));
        }

        try
        {
            var info = new FileInfo(path);

            // Büyük dosyalar için sınır
            if (info.Length > 1024 * 100) // 100KB
            {
                string preview = File.ReadLines(path).Take(50).Aggregate((a, b) => a + "\n" + b);
                return Task.FromResult(SkillResult.Ok(
                    $"📄 {Path.GetFileName(path)} ({FormatFileSize(info.Length)})\n" +
                    $"⚠️ Dosya çok büyük, ilk 50 satır gösteriliyor:\n\n```\n{preview}\n```"));
            }

            string content = File.ReadAllText(path);
            return Task.FromResult(SkillResult.Ok(
                $"📄 {Path.GetFileName(path)} ({FormatFileSize(info.Length)})\n\n```\n{content}\n```",
                content));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Dosya arama.
    /// </summary>
    private Task<SkillResult> SearchFiles(string path, string pattern)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        path = ResolveSpecialPath(path);

        try
        {
            var files = Directory.GetFiles(path, $"*{pattern}*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MaxRecursionDepth = 3
            }).Take(20).ToList();

            if (files.Count == 0)
            {
                return Task.FromResult(SkillResult.Ok($"🔍 '{pattern}' için sonuç bulunamadı."));
            }

            var sb = new StringBuilder();
            sb.AppendLine($"🔍 '{pattern}' arama sonuçları:\n");
            foreach (var file in files)
            {
                sb.AppendLine($"  📄 {file}");
            }

            return Task.FromResult(SkillResult.Ok(sb.ToString()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Özel klasör adlarını gerçek yollara çevirir.
    /// </summary>
    private static string ResolveSpecialPath(string path)
    {
        string lower = path.ToLower(new System.Globalization.CultureInfo("tr-TR")).Trim();
        return lower switch
        {
            "masaüstü" or "desktop" => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "belgeler" or "documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "indirilenler" or "downloads" => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            "müzikler" or "music" => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            "resimler" or "pictures" => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "ev" or "home" or "~" => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            _ => path
        };
    }

    /// <summary>
    /// Kullanıcı girdisinden dosya/klasör yolunu çıkarır.
    /// </summary>
    private static string ExtractPathFromInput(string input)
    {
        string lower = input.ToLower(new System.Globalization.CultureInfo("tr-TR"));

        // Özel klasör adlarını kontrol et
        string[] specialNames = { "masaüstü", "desktop", "belgeler", "documents",
                                  "indirilenler", "downloads", "ev", "home" };
        foreach (var name in specialNames)
        {
            if (lower.Contains(name)) return name;
        }

        // Windows yolu ara
        var words = input.Split(' ');
        foreach (var word in words)
        {
            if (word.Length > 2 && word[1] == ':' && (word[2] == '\\' || word[2] == '/'))
                return word;
        }

        return "";
    }

    /// <summary>Dosya uzantısına göre ikon döndürür.</summary>
    private static string GetFileIcon(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLower();
        return ext switch
        {
            ".txt" or ".md" or ".log" => "📝",
            ".cs" or ".py" or ".js" or ".ts" or ".java" or ".cpp" or ".c" or ".h" => "💻",
            ".json" or ".xml" or ".yaml" or ".yml" or ".toml" => "📋",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".svg" => "🖼️",
            ".mp3" or ".wav" or ".flac" or ".ogg" => "🎵",
            ".mp4" or ".avi" or ".mkv" or ".mov" => "🎬",
            ".pdf" => "📕",
            ".doc" or ".docx" => "📄",
            ".xls" or ".xlsx" => "📊",
            ".ppt" or ".pptx" => "📈",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
            ".exe" or ".msi" => "⚙️",
            ".dll" => "🔧",
            ".sln" or ".csproj" => "🏗️",
            _ => "📄"
        };
    }

    /// <summary>Dosya boyutunu okunabilir formata çevirir.</summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
}

