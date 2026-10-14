using System.Text;

namespace HexAI.Assistant.Skills;

/// <summary>
/// Dosya düzenleme yeteneği.
/// Mevcut dosyaları okur, belirli satırları değiştirir, içerik ekler/çıkarır.
/// LLM'in "dosyayı düzenle" tarzı komutlarını işler.
/// </summary>
public class FileEditSkill : ISkill
{
    public string Name => "Dosya Düzenleme";
    public string Description => "Dosya içeriğini okur, düzenler, satır ekler/çıkarır, dosya oluşturur.";
    public string[] TriggerKeywords => new[]
    {
        "düzenle", "değiştir", "güncelle", "ekle", "çıkar", "sil satır",
        "yaz dosya", "edit", "modify", "append", "replace", "insert"
    };

    public async Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string action = parameters.GetValueOrDefault("action", "read");
        string path = parameters.GetValueOrDefault("path", "");
        string content = parameters.GetValueOrDefault("content", "");
        string lineNum = parameters.GetValueOrDefault("line", "");
        string search = parameters.GetValueOrDefault("search", "");
        string replace = parameters.GetValueOrDefault("replace", "");

        return action.ToLower() switch
        {
            "read" => await ReadFileAsync(path),
            "write" or "create" => await WriteFileAsync(path, content),
            "append" => await AppendFileAsync(path, content),
            "replace" => await ReplaceInFileAsync(path, search, replace),
            "insert_line" => await InsertLineAsync(path, content, lineNum),
            "delete_lines" => await DeleteLinesAsync(path, lineNum),
            _ => await ReadFileAsync(path)
        };
    }

    private Task<SkillResult> ReadFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(SkillResult.Fail("Dosya yolu belirtilmedi."));

        if (!File.Exists(path))
            return Task.FromResult(SkillResult.Fail($"Dosya bulunamadı: {path}"));

        try
        {
            string content = File.ReadAllText(path);
            var lines = content.Split('\n');
            var sb = new StringBuilder();
            sb.AppendLine($"📄 {Path.GetFileName(path)} ({lines.Length} satır)\n");

            // Satır numarası ile göster (max 100 satır)
            int maxLines = Math.Min(lines.Length, 100);
            for (int i = 0; i < maxLines; i++)
            {
                sb.AppendLine($"{i + 1,4} | {lines[i].TrimEnd('\r')}");
            }

            if (lines.Length > 100)
                sb.AppendLine($"\n  ... ve {lines.Length - 100} satır daha");

            return Task.FromResult(SkillResult.Ok(sb.ToString(), content));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    private Task<SkillResult> WriteFileAsync(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(SkillResult.Fail("Dosya yolu belirtilmedi."));

        try
        {
            string? dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);

            File.WriteAllText(path, content);
            int lineCount = content.Split('\n').Length;
            return Task.FromResult(SkillResult.Ok($"✅ Dosya yazıldı: {path} ({lineCount} satır)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    private Task<SkillResult> AppendFileAsync(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(SkillResult.Fail("Dosya yolu belirtilmedi."));

        try
        {
            File.AppendAllText(path, "\n" + content);
            return Task.FromResult(SkillResult.Ok($"✅ İçerik eklendi: {path}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    private Task<SkillResult> ReplaceInFileAsync(string path, string search, string replace)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Task.FromResult(SkillResult.Fail($"Dosya bulunamadı: {path}"));

        if (string.IsNullOrEmpty(search))
            return Task.FromResult(SkillResult.Fail("Aranacak metin belirtilmedi."));

        try
        {
            string content = File.ReadAllText(path);
            int count = (content.Length - content.Replace(search, "").Length) / search.Length;

            if (count == 0)
                return Task.FromResult(SkillResult.Ok($"⚠️ '{search}' metni dosyada bulunamadı."));

            string newContent = content.Replace(search, replace);
            File.WriteAllText(path, newContent);

            return Task.FromResult(SkillResult.Ok(
                $"✅ Değiştirme yapıldı: {count} eşleşme\n  '{search}' → '{replace}'\n  Dosya: {path}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    private Task<SkillResult> InsertLineAsync(string path, string content, string lineNumStr)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Task.FromResult(SkillResult.Fail($"Dosya bulunamadı: {path}"));

        if (!int.TryParse(lineNumStr, out int lineNum))
            return Task.FromResult(SkillResult.Fail("Geçerli bir satır numarası belirtilmedi."));

        try
        {
            var lines = File.ReadAllLines(path).ToList();
            int insertIndex = Math.Max(0, Math.Min(lineNum - 1, lines.Count));
            lines.Insert(insertIndex, content);
            File.WriteAllLines(path, lines);

            return Task.FromResult(SkillResult.Ok(
                $"✅ Satır {lineNum}'e eklendi: {path}\n  İçerik: {content}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }

    private Task<SkillResult> DeleteLinesAsync(string path, string lineRange)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Task.FromResult(SkillResult.Fail($"Dosya bulunamadı: {path}"));

        try
        {
            var lines = File.ReadAllLines(path).ToList();

            // "5" veya "5-10" formatında
            if (lineRange.Contains('-'))
            {
                var parts = lineRange.Split('-');
                if (int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    start = Math.Max(1, start);
                    end = Math.Min(lines.Count, end);
                    lines.RemoveRange(start - 1, end - start + 1);
                    File.WriteAllLines(path, lines);
                    return Task.FromResult(SkillResult.Ok($"✅ Satır {start}-{end} silindi: {path}"));
                }
            }
            else if (int.TryParse(lineRange, out int lineNum))
            {
                if (lineNum >= 1 && lineNum <= lines.Count)
                {
                    string removed = lines[lineNum - 1];
                    lines.RemoveAt(lineNum - 1);
                    File.WriteAllLines(path, lines);
                    return Task.FromResult(SkillResult.Ok(
                        $"✅ Satır {lineNum} silindi: {path}\n  Silinen: {removed}"));
                }
            }

            return Task.FromResult(SkillResult.Fail("Geçerli satır numarası/aralığı belirtilmedi."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail(ex.Message));
        }
    }
}
