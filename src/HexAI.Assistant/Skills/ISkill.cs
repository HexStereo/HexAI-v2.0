namespace HexAI.Assistant.Skills;

/// <summary>
/// Asistanın bir yeteneğini (skill) temsil eden arayüz.
/// Her yetenek belirli bir görevi yerine getirir (dosya işlemi, komut çalıştırma vb.).
/// </summary>
public interface ISkill
{
    /// <summary>Yetenek adı.</summary>
    string Name { get; }

    /// <summary>Yeteneğin kısa açıklaması.</summary>
    string Description { get; }

    /// <summary>Bu yeteneği tetikleyen anahtar kelimeler.</summary>
    string[] TriggerKeywords { get; }

    /// <summary>
    /// Yeteneği çalıştırır.
    /// </summary>
    /// <param name="userInput">Kullanıcının orijinal girdisi</param>
    /// <param name="parameters">Intent algılama sonucu çıkarılan parametreler</param>
    /// <returns>Yetenek sonucu</returns>
    Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters);
}

/// <summary>
/// Skill çalışma sonucu.
/// </summary>
public class SkillResult
{
    /// <summary>İşlem başarılı mı?</summary>
    public bool Success { get; set; }

    /// <summary>Kullanıcıya gösterilecek yanıt metni.</summary>
    public string Response { get; set; } = "";

    /// <summary>Ham çıktı verisi (opsiyonel).</summary>
    public string? RawOutput { get; set; }

    /// <summary>Hata mesajı (başarısız işlemler için).</summary>
    public string? Error { get; set; }

    /// <summary>Başarılı sonuç oluşturma kolaylık metodu.</summary>
    public static SkillResult Ok(string response, string? rawOutput = null) =>
        new() { Success = true, Response = response, RawOutput = rawOutput };

    /// <summary>Başarısız sonuç oluşturma kolaylık metodu.</summary>
    public static SkillResult Fail(string error) =>
        new() { Success = false, Error = error, Response = $"❌ Hata: {error}" };
}
