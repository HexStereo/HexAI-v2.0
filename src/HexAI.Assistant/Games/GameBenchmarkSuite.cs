using System;
using System.Text;

namespace HexAI.Assistant.Games;

/// <summary>
/// Oyun Tabanlı Genel YZ Kararlılık ve Doğruluk Değerlendirmesi.
/// </summary>
public class GameBenchmarkSuite
{
    public class CombinedReport
    {
        public MazeStabilityReport MazeReport { get; set; } = null!;
        public SequenceStabilityReport SequenceReport { get; set; } = null!;
        public double OverallStabilityScore { get; set; }
        public double OverallAccuracyScore { get; set; }
        public double OverallErrorRate { get; set; }
        public string OverallGrade { get; set; } = "A";
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string ToFullReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║   🏆 HEXAI MINI-GAMES BİLİŞSEL VE ZAMAN KARARLILIĞI BÜYÜK RAPORU ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Tarih                  : {Timestamp:dd/MM/yyyy HH:mm:ss,-37}║");
            sb.AppendLine($"║  Genel Kararlılık Notu  : {OverallGrade,-37}║");
            sb.AppendLine($"║  Zaman Kararlılık Skoru : %{OverallStabilityScore:F2}{"",-33}║");
            sb.AppendLine($"║  Genel Doğruluk Skoru   : %{OverallAccuracyScore:F2}{"",-33}║");
            sb.AppendLine($"║  Genel Hata Oranı       : %{OverallErrorRate:F2}{"",-33}║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝\n");

            sb.AppendLine(MazeReport.ToMarkdownReport());
            sb.AppendLine();
            sb.AppendLine(SequenceReport.ToMarkdownReport());

            return sb.ToString();
        }
    }

    /// <summary>
    /// Kapsamlı oyun benchmark'ını çalıştırır.
    /// </summary>
    public static CombinedReport RunFullSuite(int mazeRuns = 10, int sequenceCount = 15)
    {
        var mazeReport = MazeBenchmark.RunStabilityBenchmark(mazeRuns);
        var sequenceReport = SequencePatternGame.RunBenchmark(sequenceCount);

        double stability = (mazeReport.TimeStabilityIndexPercent + sequenceReport.TimeStabilityIndexPercent) / 2.0;
        double accuracy = (mazeReport.AccuracyPercent + sequenceReport.AccuracyPercent) / 2.0;
        double errorRate = (mazeReport.MeanErrorRatePercent + sequenceReport.ErrorRatePercent) / 2.0;

        string grade = (stability * 0.5 + accuracy * 0.5) switch
        {
            >= 90 => "A+ (Mükemmel Kararlılık & Doğruluk)",
            >= 80 => "A  (Çok Yüksek Kararlılık)",
            >= 70 => "B  (İyi Kararlılık)",
            >= 60 => "C  (Orta Seviye)",
            _ => "D  (Düşük Kararlılık)"
        };

        return new CombinedReport
        {
            MazeReport = mazeReport,
            SequenceReport = sequenceReport,
            OverallStabilityScore = stability,
            OverallAccuracyScore = accuracy,
            OverallErrorRate = errorRate,
            OverallGrade = grade,
            Timestamp = DateTime.Now
        };
    }
}
