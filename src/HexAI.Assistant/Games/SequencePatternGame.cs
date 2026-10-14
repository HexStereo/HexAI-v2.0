using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace HexAI.Assistant.Games;

/// <summary>
/// Sayısal örüntü ve mantık bulmacası tipi.
/// </summary>
public enum PatternType
{
    Arithmetic,   // Aritmetik Artış (a_n = a_1 + (n-1)d)
    Geometric,    // Geometrik Çarpım (a_n = a_1 * r^(n-1))
    Fibonacci,    // Fibonacci / Lucas Toplamsal Dizi
    Quadratic,    // İkinci Dereceden (n^2 + c veya an^2 + bn + c)
    Alternating,  // Alternatif Çift Kural (+a, *b, +a, *b...)
    GrowingGap    // Kademeli Artan Farklar (+1, +2, +3, +4...)
}

public class SequenceProblem
{
    public int Id { get; set; }
    public PatternType Type { get; set; }
    public string TypeName { get; set; } = "";
    public List<long> GivenSequence { get; set; } = new();
    public long ExpectedNext { get; set; }
    public string RuleDescription { get; set; } = "";
}

public class SequenceAnswerResult
{
    public SequenceProblem Problem { get; set; } = null!;
    public long PredictedValue { get; set; }
    public bool IsCorrect { get; set; }
    public double ErrorMargin { get; set; }
    public double ElapsedMs { get; set; }
    public string Explanation { get; set; } = "";
}

public class SequenceStabilityReport
{
    public int TotalProblems { get; set; }
    public int CorrectCount { get; set; }
    public double AccuracyPercent { get; set; }
    public double ErrorRatePercent { get; set; }
    public double MeanTimeMs { get; set; }
    public double MinTimeMs { get; set; }
    public double MaxTimeMs { get; set; }
    public double StdDevTimeMs { get; set; }
    public double TimeStabilityIndexPercent { get; set; }
    public List<SequenceAnswerResult> Results { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string ToMarkdownReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║   🧩 HEXAI MANTIKSAL DİZİ & ÖRÜNTÜ KARARLILIK TEST RAPORU    ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Tarih                  : {Timestamp:dd/MM/yyyy HH:mm:ss,-35}║");
        sb.AppendLine($"║  Toplam Test Problemi   : {TotalProblems,-35}║");
        sb.AppendLine($"║  Mantıksal Doğruluk     : %{AccuracyPercent:F2} ({CorrectCount}/{TotalProblems}){"",-23}║");
        sb.AppendLine($"║  Hata Oranı             : %{ErrorRatePercent:F2}{"",-35}║");
        sb.AppendLine($"║  Zaman Kararlılığı      : %{TimeStabilityIndexPercent:F2} (Sapma: {StdDevTimeMs:F3} ms){"",-15}║");
        sb.AppendLine($"║  Ortalama Çözüm Süresi  : {MeanTimeMs:F3} ms (Min: {MinTimeMs:F3}, Max: {MaxTimeMs:F3}){"",-10}║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine("📋 Örnek Problemler ve Yapay Zeka Yanıtları:");
        foreach (var r in Results.Take(8))
        {
            string icon = r.IsCorrect ? "✅" : "❌";
            string seqStr = string.Join(", ", r.Problem.GivenSequence) + ", [ ? ]";
            sb.AppendLine($"  {icon} [{r.Problem.TypeName}] {seqStr}");
            sb.AppendLine($"     → Tahmin: {r.PredictedValue} | Beklenen: {r.Problem.ExpectedNext} ({r.ElapsedMs:F3} ms)");
        }
        return sb.ToString();
    }
}

/// <summary>
/// Sayı Dizisi ve Mantık Örüntüsü Mini Oyunu ve Kararlılık Ölçer.
/// </summary>
public static class SequencePatternGame
{
    /// <summary>
    /// Rastgele mantıksal sayı örüntüsü üretir.
    /// </summary>
    public static SequenceProblem GenerateRandomProblem(int id = 1, Random? rng = null)
    {
        rng ??= new Random();
        var types = Enum.GetValues<PatternType>();
        var type = types[rng.Next(types.Length)];

        var seq = new List<long>();
        long expected = 0;
        string ruleDesc = "";
        string typeName = "";

        switch (type)
        {
            case PatternType.Arithmetic:
                typeName = "Aritmetik Dizi";
                long a1 = rng.Next(1, 30);
                long d = rng.Next(2, 15);
                for (int i = 0; i < 5; i++) seq.Add(a1 + i * d);
                expected = a1 + 5 * d;
                ruleDesc = $"Her adımda +{d} artış";
                break;

            case PatternType.Geometric:
                typeName = "Geometrik Dizi";
                long g1 = rng.Next(1, 6);
                long r = rng.Next(2, 4);
                long curG = g1;
                for (int i = 0; i < 4; i++) { seq.Add(curG); curG *= r; }
                expected = curG;
                ruleDesc = $"Her adımda *{r} katlama";
                break;

            case PatternType.Fibonacci:
                typeName = "Fibonacci Toplamsal";
                long f0 = rng.Next(1, 5);
                long f1 = rng.Next(1, 7);
                seq.Add(f0);
                seq.Add(f1);
                for (int i = 0; i < 4; i++)
                {
                    long nextF = seq[^1] + seq[^2];
                    seq.Add(nextF);
                }
                expected = seq[^1] + seq[^2];
                ruleDesc = "Son iki terimin toplamı";
                break;

            case PatternType.Quadratic:
                typeName = "Karesel Örüntü";
                long c = rng.Next(0, 10);
                for (long i = 1; i <= 5; i++) seq.Add(i * i + c);
                expected = 6 * 6 + c;
                ruleDesc = $"n^2 + {c}";
                break;

            case PatternType.Alternating:
                typeName = "Dalgalı Kural";
                long altVal = rng.Next(2, 10);
                long addStep = rng.Next(2, 7);
                seq.Add(altVal);
                for (int i = 0; i < 2; i++)
                {
                    altVal *= 2;
                    seq.Add(altVal);
                    altVal += addStep;
                    seq.Add(altVal);
                }
                expected = altVal * 2;
                ruleDesc = $"Alternatif x2 ve +{addStep}";
                break;

            case PatternType.GrowingGap:
            default:
                typeName = "Kademeli Artış";
                long gapVal = rng.Next(1, 10);
                long gap = rng.Next(1, 4);
                seq.Add(gapVal);
                for (int i = 0; i < 5; i++)
                {
                    gapVal += gap;
                    gap++;
                    seq.Add(gapVal);
                }
                expected = gapVal + gap;
                ruleDesc = "Farklar her adımda +1 artıyor";
                break;
        }

        return new SequenceProblem
        {
            Id = id,
            Type = type,
            TypeName = typeName,
            GivenSequence = seq,
            ExpectedNext = expected,
            RuleDescription = ruleDesc
        };
    }

    /// <summary>
    /// HexAI Mantıksal Akıl Yürütme Algoritması — 
    /// 1. ve 2. mertebeden farkları ve oranları inceleyerek bir sonraki sayıyı tahmin eder.
    /// </summary>
    public static SequenceAnswerResult Solve(SequenceProblem problem)
    {
        var sw = Stopwatch.StartNew();
        var s = problem.GivenSequence;
        long pred = 0;
        string explanation = "";

        if (s.Count >= 3)
        {
            // 1. Derece Farklar (d1)
            var d1 = new List<long>();
            for (int i = 0; i < s.Count - 1; i++) d1.Add(s[i + 1] - s[i]);

            // Aritmetik kontrol: d1 sabit mi?
            if (d1.Distinct().Count() == 1)
            {
                pred = s[^1] + d1[0];
                explanation = $"Sabit fark: {d1[0]}";
            }
            else
            {
                // Geometrik kontrol: oranlar sabit mi?
                bool isGeom = true;
                double ratio = s[0] != 0 ? (double)s[1] / s[0] : 0;
                for (int i = 1; i < s.Count - 1; i++)
                {
                    if (s[i] == 0 || Math.Abs((double)s[i + 1] / s[i] - ratio) > 0.001)
                    {
                        isGeom = false;
                        break;
                    }
                }

                if (isGeom && ratio > 1)
                {
                    pred = (long)Math.Round(s[^1] * ratio);
                    explanation = $"Sabit oran: x{ratio:F0}";
                }
                else
                {
                    // 2. Derece Farklar (d2)
                    var d2 = new List<long>();
                    for (int i = 0; i < d1.Count - 1; i++) d2.Add(d1[i + 1] - d1[i]);

                    if (d2.Distinct().Count() == 1)
                    {
                        long nextD1 = d1[^1] + d2[0];
                        pred = s[^1] + nextD1;
                        explanation = $"İkinci mertebe fark sabit: {d2[0]}";
                    }
                    else
                    {
                        // Fibonacci kontrol
                        bool isFibo = true;
                        for (int i = 2; i < s.Count; i++)
                        {
                            if (s[i] != s[i - 1] + s[i - 2])
                            {
                                isFibo = false;
                                break;
                            }
                        }

                        if (isFibo)
                        {
                            pred = s[^1] + s[^2];
                            explanation = "Fibonacci kuralı (t_n = t_{n-1} + t_{n-2})";
                        }
                        else
                        {
                            // Alternatif kural kestirimi
                            if (s.Count >= 4 && s[1] == s[0] * 2 && s[3] == s[2] * 2)
                            {
                                pred = s[^1] * 2;
                                explanation = "Çift adım x2 kuralı";
                            }
                            else
                            {
                                // Kademeli fark tahmini fallback
                                long avgD2 = d2.Count > 0 ? (long)Math.Round(d2.Average()) : 1;
                                pred = s[^1] + d1[^1] + avgD2;
                                explanation = $"Polinom yaklaşımı (delta: {avgD2})";
                            }
                        }
                    }
                }
            }
        }
        else
        {
            pred = s[^1] + 1;
            explanation = "Minimum adım";
        }

        sw.Stop();
        double elapsedMs = sw.Elapsed.TotalMilliseconds;

        bool correct = pred == problem.ExpectedNext;
        double diff = Math.Abs(pred - problem.ExpectedNext);

        return new SequenceAnswerResult
        {
            Problem = problem,
            PredictedValue = pred,
            IsCorrect = correct,
            ErrorMargin = diff,
            ElapsedMs = elapsedMs,
            Explanation = explanation
        };
    }

    /// <summary>
    /// Çoklu problem ile mantık doğruluğu ve zaman kararlılığı test eder.
    /// </summary>
    public static SequenceStabilityReport RunBenchmark(int count = 20)
    {
        var report = new SequenceStabilityReport { TotalProblems = count };
        var results = new List<SequenceAnswerResult>();
        var rng = new Random();

        for (int i = 1; i <= count; i++)
        {
            var problem = GenerateRandomProblem(i, rng);
            var ans = Solve(problem);
            results.Add(ans);
        }

        report.Results = results;
        report.CorrectCount = results.Count(r => r.IsCorrect);
        report.AccuracyPercent = (double)report.CorrectCount / count * 100.0;
        report.ErrorRatePercent = 100.0 - report.AccuracyPercent;

        var times = results.Select(r => r.ElapsedMs).ToList();
        report.MeanTimeMs = times.Average();
        report.MinTimeMs = times.Min();
        report.MaxTimeMs = times.Max();

        double sumSquares = times.Sum(t => Math.Pow(t - report.MeanTimeMs, 2));
        report.StdDevTimeMs = Math.Sqrt(sumSquares / Math.Max(1, times.Count - 1));

        double cv = report.MeanTimeMs > 0 ? (report.StdDevTimeMs / report.MeanTimeMs) : 0;
        report.TimeStabilityIndexPercent = Math.Clamp(100.0 - (cv * 100.0), 0.0, 100.0);

        return report;
    }
}
