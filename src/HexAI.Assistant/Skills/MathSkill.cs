using System.Data;
using System.Text.RegularExpressions;

namespace HexAI.Assistant.Skills;

/// <summary>
/// Matematik hesaplama yeteneği.
/// Basit aritmetik ifadeleri çözer.
/// </summary>
public class MathSkill : ISkill
{
    public string Name => "Matematik";
    public string Description => "Matematiksel hesaplamalar yapar";
    public string[] TriggerKeywords => new[] { "hesapla", "topla", "çarp", "böl", "kaç eder" };

    public Task<SkillResult> ExecuteAsync(string userInput, Dictionary<string, string> parameters)
    {
        string expression = parameters.GetValueOrDefault("expression", userInput);

        // Türkçe ifadeleri matematik operatörlerine çevir
        expression = NormalizeMathExpression(expression);

        try
        {
            // Güvenli hesaplama — sadece sayılar ve operatörler
            if (!IsValidExpression(expression))
            {
                return Task.FromResult(SkillResult.Fail("Geçersiz matematiksel ifade."));
            }

            double result = EvaluateExpression(expression);

            string formattedResult = result == Math.Floor(result)
                ? result.ToString("F0")
                : result.ToString("F6").TrimEnd('0').TrimEnd('.');

            return Task.FromResult(SkillResult.Ok(
                $"🔢 Hesaplama:\n  {expression} = **{formattedResult}**"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Fail($"Hesaplama hatası: {ex.Message}"));
        }
    }

    /// <summary>
    /// Türkçe ifadeleri matematiksel operatörlere çevirir.
    /// </summary>
    private string NormalizeMathExpression(string input)
    {
        string expr = input.ToLower(new System.Globalization.CultureInfo("tr-TR"));

        // Türkçe kelime → operatör
        expr = Regex.Replace(expr, @"\bartı\b", "+");
        expr = Regex.Replace(expr, @"\beksi\b", "-");
        expr = Regex.Replace(expr, @"\bçarpı\b", "*");
        expr = Regex.Replace(expr, @"\bbölü\b", "/");
        expr = Regex.Replace(expr, @"\büssü\b", "^");
        expr = Regex.Replace(expr, @"\bkere\b", "*");

        // Gereksiz kelimeleri temizle
        string[] removeWords = { "hesapla", "kaç", "eder", "kaçtır", "sonucu",
                                 "nedir", "ne", "bul", "ver", "söyle" };
        foreach (var word in removeWords)
        {
            expr = Regex.Replace(expr, $@"\b{word}\b", "");
        }

        // Virgülü noktaya çevir (Türkçe ondalık gösterimi)
        expr = expr.Replace(",", ".");

        // Sadece sayılar, operatörler ve parantezler kalsın
        expr = Regex.Replace(expr, @"[^\d\.\+\-\*\/\^\(\)\s]", "");

        // Çoklu boşlukları temizle
        expr = Regex.Replace(expr, @"\s+", " ").Trim();

        return expr;
    }

    /// <summary>
    /// İfadenin güvenli olup olmadığını kontrol eder.
    /// </summary>
    private bool IsValidExpression(string expr)
    {
        // Sadece sayılar, operatörler, nokta, boşluk ve parantez
        return Regex.IsMatch(expr, @"^[\d\.\+\-\*\/\^\(\)\s]+$") && expr.Length > 0;
    }

    /// <summary>
    /// Basit matematiksel ifadeyi hesaplar.
    /// Üs (^) operatörünü destekler.
    /// </summary>
    private double EvaluateExpression(string expression)
    {
        // Üs operatörünü işle
        expression = Regex.Replace(expression, @"(\d+\.?\d*)\s*\^\s*(\d+\.?\d*)", m =>
        {
            double baseNum = double.Parse(m.Groups[1].Value);
            double exp = double.Parse(m.Groups[2].Value);
            return Math.Pow(baseNum, exp).ToString();
        });

        // DataTable.Compute ile güvenli hesaplama
        var table = new DataTable();
        var result = table.Compute(expression, null);
        return Convert.ToDouble(result);
    }
}

