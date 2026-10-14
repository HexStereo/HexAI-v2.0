namespace HexAI.Assistant.Evaluation;

/// <summary>
/// Eğitim değerlendirme sonuçlarını tutan veri modeli.
/// </summary>
public class EvaluationResult
{
    /// <summary>Tutarlılık puanı (0-100): Aynı soruya verilen cevapların ne kadar tutarlı olduğu.</summary>
    public double ConsistencyScore { get; set; }

    /// <summary>Doğruluk puanı (0-100): Beklenen cevaplarla ne kadar örtüştüğü.</summary>
    public double AccuracyScore { get; set; }

    /// <summary>Halüsinasyon oranı (0-100): Bilmediği konularda uydurma cevap verme oranı. Düşük = iyi.</summary>
    public double HallucinationRate { get; set; }

    /// <summary>Bağlam koruma puanı (0-100): Çok turlu sohbette bağlamı hatırlama oranı.</summary>
    public double ContextRetentionScore { get; set; }

    /// <summary>Perplexity skoru: Düşük = model daha iyi. -1 = hesaplanamadı.</summary>
    public double PerplexityScore { get; set; } = -1;

    /// <summary>Genel not (A-F).</summary>
    public string OverallGrade { get; set; } = "F";

    /// <summary>Genel yüzde puan (0-100).</summary>
    public double OverallScore { get; set; }

    /// <summary>Her testin ayrıntılı sonuçları.</summary>
    public List<TestDetail> DetailedResults { get; set; } = new();

    /// <summary>Değerlendirme tarihi.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Değerlendirilen model adı.</summary>
    public string ModelName { get; set; } = "";

    /// <summary>Model eğitilmiş miydi?</summary>
    public bool WasModelTrained { get; set; }

    /// <summary>Toplam test süresi (ms).</summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Genel notu hesaplar.
    /// </summary>
    public void CalculateOverallGrade()
    {
        // Halüsinasyon düşük olması iyi, ters çevir
        double hallucinationGood = 100 - HallucinationRate;

        OverallScore = (ConsistencyScore * 0.30) +
                       (AccuracyScore * 0.30) +
                       (hallucinationGood * 0.20) +
                       (ContextRetentionScore * 0.20);

        OverallGrade = OverallScore switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            >= 50 => "E",
            _ => "F"
        };
    }

    /// <summary>
    /// İnsan tarafından okunabilir rapor metnini döndürür.
    /// </summary>
    public string ToReportString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════╗");
        sb.AppendLine("║       🧪 HexAI Eğitim Değerlendirme Raporu     ║");
        sb.AppendLine("╠══════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Model     : {ModelName,-35}║");
        sb.AppendLine($"║  Tarih     : {Timestamp:dd/MM/yyyy HH:mm,-35}║");
        sb.AppendLine($"║  Süre      : {ElapsedMs} ms{"",-30}║");
        sb.AppendLine("╠══════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Tutarlılık    : {ConsistencyScore,6:F1}/100  {GetBar(ConsistencyScore)}  ║");
        sb.AppendLine($"║  Doğruluk      : {AccuracyScore,6:F1}/100  {GetBar(AccuracyScore)}  ║");
        sb.AppendLine($"║  Halüsinasyon  : {HallucinationRate,6:F1}/100  {GetBar(100 - HallucinationRate)}  ║");
        sb.AppendLine($"║  Bağlam Koruma : {ContextRetentionScore,6:F1}/100  {GetBar(ContextRetentionScore)}  ║");
        if (PerplexityScore >= 0)
            sb.AppendLine($"║  Perplexity    : {PerplexityScore,6:F2}{"",-20}║");
        sb.AppendLine("╠══════════════════════════════════════════════════╣");
        sb.AppendLine($"║  GENEL PUAN    : {OverallScore,6:F1}/100  NOT: {OverallGrade,-12}║");
        sb.AppendLine("╚══════════════════════════════════════════════════╝");

        if (DetailedResults.Count > 0)
        {
            sb.AppendLine("\n📋 Detaylı Sonuçlar:\n");
            foreach (var detail in DetailedResults)
            {
                string icon = detail.Passed ? "✅" : "❌";
                sb.AppendLine($"  {icon} [{detail.Category}] {detail.TestName}");
                sb.AppendLine($"     Soru: {detail.Question}");
                sb.AppendLine($"     Yanıt: {Truncate(detail.ActualAnswer, 80)}");
                if (!string.IsNullOrEmpty(detail.ExpectedAnswer))
                    sb.AppendLine($"     Beklenen: {Truncate(detail.ExpectedAnswer, 80)}");
                sb.AppendLine($"     Puan: {detail.Score:F1}/100");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string GetBar(double value)
    {
        int filled = (int)(value / 100 * 10);
        filled = Math.Clamp(filled, 0, 10);
        return new string('█', filled) + new string('░', 10 - filled);
    }

    private static string Truncate(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return "(boş)";
        text = text.Replace("\n", " ").Replace("\r", "");
        return text.Length <= maxLen ? text : text[..maxLen] + "...";
    }
}

/// <summary>
/// Tek bir testin ayrıntılı sonucu.
/// </summary>
public class TestDetail
{
    /// <summary>Test kategorisi: Consistency, Accuracy, Hallucination, ContextRetention.</summary>
    public string Category { get; set; } = "";

    /// <summary>Test adı.</summary>
    public string TestName { get; set; } = "";

    /// <summary>Sorulan soru.</summary>
    public string Question { get; set; } = "";

    /// <summary>Modelin verdiği yanıt.</summary>
    public string ActualAnswer { get; set; } = "";

    /// <summary>Beklenen yanıt (varsa).</summary>
    public string ExpectedAnswer { get; set; } = "";

    /// <summary>Bu test başarılı mı?</summary>
    public bool Passed { get; set; }

    /// <summary>Test puanı (0-100).</summary>
    public double Score { get; set; }
}
