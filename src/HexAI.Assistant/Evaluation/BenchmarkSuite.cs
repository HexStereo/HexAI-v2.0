namespace HexAI.Assistant.Evaluation;

/// <summary>
/// Standart benchmark soruları ile modeli test eden sınıf.
/// Türkçe temel bilgi, mantık ve anlam çıkarım soruları içerir.
/// </summary>
public class BenchmarkSuite
{
    /// <summary>
    /// Benchmark soru-cevap çifti.
    /// </summary>
    public class BenchmarkQuestion
    {
        public string Category { get; set; } = "";
        public string Question { get; set; } = "";
        public string[] AcceptableAnswerKeywords { get; set; } = Array.Empty<string>();
        public int Difficulty { get; set; } = 1; // 1=kolay, 2=orta, 3=zor
    }

    /// <summary>
    /// Benchmark sonuç raporu.
    /// </summary>
    public class BenchmarkResult
    {
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int PartialAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public double ScorePercent { get; set; }
        public string Grade { get; set; } = "F";
        public List<BenchmarkDetail> Details { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public long ElapsedMs { get; set; }

        public string ToReportString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════╗");
            sb.AppendLine("║        ⚡ HexAI Benchmark Raporu               ║");
            sb.AppendLine("╠══════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Tarih        : {Timestamp:dd/MM/yyyy HH:mm,-32}║");
            sb.AppendLine($"║  Süre         : {ElapsedMs} ms{"",-30}║");
            sb.AppendLine($"║  Toplam Soru  : {TotalQuestions,-33}║");
            sb.AppendLine($"║  Doğru        : {CorrectAnswers,-33}║");
            sb.AppendLine($"║  Kısmi Doğru  : {PartialAnswers,-33}║");
            sb.AppendLine($"║  Yanlış       : {WrongAnswers,-33}║");
            sb.AppendLine($"║  Puan         : {ScorePercent:F1}%  NOT: {Grade,-20}║");
            sb.AppendLine("╚══════════════════════════════════════════════════╝");

            if (Details.Count > 0)
            {
                sb.AppendLine("\n📋 Detaylar:\n");

                var grouped = Details.GroupBy(d => d.Category);
                foreach (var group in grouped)
                {
                    sb.AppendLine($"  ── {group.Key} ──");
                    foreach (var detail in group)
                    {
                        string icon = detail.Result switch
                        {
                            "correct" => "✅",
                            "partial" => "🟡",
                            _ => "❌"
                        };
                        sb.AppendLine($"  {icon} {detail.Question}");
                        sb.AppendLine($"     → {Truncate(detail.ModelAnswer, 60)}");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "(boş)";
            text = text.Replace("\n", " ");
            return text.Length <= max ? text : text[..max] + "...";
        }
    }

    public class BenchmarkDetail
    {
        public string Category { get; set; } = "";
        public string Question { get; set; } = "";
        public string ModelAnswer { get; set; } = "";
        public string[] ExpectedKeywords { get; set; } = Array.Empty<string>();
        public string Result { get; set; } = "wrong"; // correct, partial, wrong
    }

    /// <summary>
    /// Standart Türkçe benchmark soru seti.
    /// </summary>
    public static List<BenchmarkQuestion> GetDefaultQuestions()
    {
        return new List<BenchmarkQuestion>
        {
            // Temel Bilgi
            new()
            {
                Category = "Temel Bilgi",
                Question = "türkiye başkent",
                AcceptableAnswerKeywords = new[] { "ankara" },
                Difficulty = 1
            },
            new()
            {
                Category = "Temel Bilgi",
                Question = "güneş sistemi gezegen",
                AcceptableAnswerKeywords = new[] { "gezegen", "dünya", "mars", "güneş" },
                Difficulty = 1
            },
            new()
            {
                Category = "Temel Bilgi",
                Question = "su formül",
                AcceptableAnswerKeywords = new[] { "h2o", "hidrojen", "oksijen" },
                Difficulty = 1
            },

            // Teknoloji
            new()
            {
                Category = "Teknoloji",
                Question = "yapay zeka öğrenme",
                AcceptableAnswerKeywords = new[] { "yapay", "zeka", "öğrenme", "makine", "derin" },
                Difficulty = 2
            },
            new()
            {
                Category = "Teknoloji",
                Question = "programlama dil",
                AcceptableAnswerKeywords = new[] { "programlama", "dil", "kod", "yazılım" },
                Difficulty = 2
            },
            new()
            {
                Category = "Teknoloji",
                Question = "bilgisayar işlemci",
                AcceptableAnswerKeywords = new[] { "bilgisayar", "işlemci", "cpu", "bellek" },
                Difficulty = 2
            },

            // Mantık / Anlam
            new()
            {
                Category = "Mantık",
                Question = "büyük küçük karşıt",
                AcceptableAnswerKeywords = new[] { "büyük", "küçük", "karşıt", "zıt" },
                Difficulty = 2
            },
            new()
            {
                Category = "Mantık",
                Question = "kış yaz mevsim",
                AcceptableAnswerKeywords = new[] { "kış", "yaz", "mevsim", "ilkbahar", "sonbahar" },
                Difficulty = 2
            },

            // Dil / Gramer
            new()
            {
                Category = "Dil",
                Question = "merhaba günaydın selamlama",
                AcceptableAnswerKeywords = new[] { "merhaba", "selam", "günaydın", "iyi" },
                Difficulty = 1
            },
            new()
            {
                Category = "Dil",
                Question = "kitap okumak bilgi",
                AcceptableAnswerKeywords = new[] { "kitap", "okumak", "bilgi", "öğrenmek" },
                Difficulty = 2
            },
        };
    }

    /// <summary>
    /// Benchmark'ı çalıştırır.
    /// </summary>
    /// <param name="generateFunc">Model üretim fonksiyonu: seed → üretilen metin.</param>
    /// <param name="questions">Soru listesi (null ise varsayılan).</param>
    public static BenchmarkResult Run(
        Func<string, string> generateFunc,
        List<BenchmarkQuestion>? questions = null)
    {
        questions ??= GetDefaultQuestions();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = new BenchmarkResult
        {
            TotalQuestions = questions.Count,
            Timestamp = DateTime.Now
        };

        foreach (var q in questions)
        {
            string modelAnswer;
            try
            {
                modelAnswer = generateFunc(q.Question);
            }
            catch
            {
                modelAnswer = "(üretim hatası)";
            }

            string lowerAnswer = modelAnswer.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            int matchCount = q.AcceptableAnswerKeywords
                .Count(kw => lowerAnswer.Contains(kw.ToLower(new System.Globalization.CultureInfo("tr-TR"))));

            string evalResult;
            if (matchCount >= 2 || (q.AcceptableAnswerKeywords.Length == 1 && matchCount == 1))
            {
                evalResult = "correct";
                result.CorrectAnswers++;
            }
            else if (matchCount >= 1)
            {
                evalResult = "partial";
                result.PartialAnswers++;
            }
            else
            {
                evalResult = "wrong";
                result.WrongAnswers++;
            }

            result.Details.Add(new BenchmarkDetail
            {
                Category = q.Category,
                Question = q.Question,
                ModelAnswer = modelAnswer,
                ExpectedKeywords = q.AcceptableAnswerKeywords,
                Result = evalResult
            });
        }

        // Puan hesapla: doğru=100, kısmi=50, yanlış=0
        double totalPoints = result.CorrectAnswers * 100 + result.PartialAnswers * 50;
        result.ScorePercent = result.TotalQuestions > 0 
            ? totalPoints / result.TotalQuestions 
            : 0;

        result.Grade = result.ScorePercent switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            >= 50 => "E",
            _ => "F"
        };

        sw.Stop();
        result.ElapsedMs = sw.ElapsedMilliseconds;
        return result;
    }
}

