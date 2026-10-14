using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace HexAI.Assistant.Games;

/// <summary>
/// Labirent hücresi durumu.
/// </summary>
public enum MazeCellType
{
    Wall = 0,
    Path = 1,
    Start = 2,
    Goal = 3,
    Visited = 4,
    Solution = 5,
    DeadEnd = 6
}

/// <summary>
/// 2B Labirent Haritası ve Geometri Temsili.
/// </summary>
public class MazeGrid
{
    public int Width { get; }
    public int Height { get; }
    public MazeCellType[,] Cells { get; }
    public (int X, int Y) Start { get; set; }
    public (int X, int Y) Goal { get; set; }

    public MazeGrid(int width, int height)
    {
        // Genişlik ve yükseklik tek sayı olmalıdır
        Width = width % 2 == 0 ? width + 1 : width;
        Height = height % 2 == 0 ? height + 1 : height;
        Cells = new MazeCellType[Width, Height];

        // Başlangıçta her yer duvar
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Cells[x, y] = MazeCellType.Wall;

        Start = (1, 1);
        Goal = (Width - 2, Height - 2);
    }

    /// <summary>
    /// Rastgele Derinlik Öncelikli Arama (DFS / Recursive Backtracker) ile
    /// çözülebilir, mükemmel labirent oluşturur.
    /// </summary>
    public static MazeGrid GenerateRandom(int width = 21, int height = 21, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var maze = new MazeGrid(width, height);

        var stack = new Stack<(int X, int Y)>();
        var visited = new bool[maze.Width, maze.Height];

        // Başlangıç hücresi
        int startX = 1;
        int startY = 1;
        maze.Cells[startX, startY] = MazeCellType.Path;
        visited[startX, startY] = true;
        stack.Push((startX, startY));

        (int dx, int dy)[] directions = { (0, -2), (0, 2), (-2, 0), (2, 0) };

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Peek();

            // Ziyaret edilmemiş komşuları bul
            var unvisitedNeighbors = new List<(int nx, int ny, int wx, int wy)>();
            foreach (var (dx, dy) in directions)
            {
                int nx = cx + dx;
                int ny = cy + dy;
                if (nx > 0 && nx < maze.Width - 1 && ny > 0 && ny < maze.Height - 1 && !visited[nx, ny])
                {
                    unvisitedNeighbors.Add((nx, ny, cx + dx / 2, cy + dy / 2));
                }
            }

            if (unvisitedNeighbors.Count > 0)
            {
                // Rastgele komşuya git
                var chosen = unvisitedNeighbors[rng.Next(unvisitedNeighbors.Count)];
                maze.Cells[chosen.wx, chosen.wy] = MazeCellType.Path; // Duvarı yık
                maze.Cells[chosen.nx, chosen.ny] = MazeCellType.Path; // Yeni hücreyi aç
                visited[chosen.nx, chosen.ny] = true;
                stack.Push((chosen.nx, chosen.ny));
            }
            else
            {
                stack.Pop();
            }
        }

        maze.Cells[maze.Start.X, maze.Start.Y] = MazeCellType.Start;
        maze.Cells[maze.Goal.X, maze.Goal.Y] = MazeCellType.Goal;

        return maze;
    }

    public bool IsPassable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return Cells[x, y] != MazeCellType.Wall;
    }
}

/// <summary>
/// Tek bir labirent çalıştırmasının sonuç ve metrik raporu.
/// </summary>
public class MazeRunResult
{
    public int RunIndex { get; set; }
    public int MazeWidth { get; set; }
    public int MazeHeight { get; set; }
    public bool Solved { get; set; }
    public double ElapsedMs { get; set; }
    public int TotalSteps { get; set; }
    public int OptimalSteps { get; set; }
    public int DeadEndsEncountered { get; set; }
    public int BacktrackSteps { get; set; }
    public double ErrorRatePercent { get; set; }
    public double PathEfficiencyPercent { get; set; }
    public List<(int X, int Y)> TraversedPath { get; set; } = new();
    public List<(int X, int Y)> OptimalPath { get; set; } = new();
    public List<(int X, int Y)> DeadEnds { get; set; } = new();
}

/// <summary>
/// Birden çok labirent testinin zaman kararlılığı (Time Stability) ve doğruluk analizi.
/// </summary>
public class MazeStabilityReport
{
    public int TotalRuns { get; set; }
    public int SolvedCount { get; set; }
    public double AccuracyPercent { get; set; }
    public double MeanTimeMs { get; set; }
    public double MinTimeMs { get; set; }
    public double MaxTimeMs { get; set; }
    public double StdDevTimeMs { get; set; }
    public double JitterMs { get; set; }
    public double TimeStabilityIndexPercent { get; set; }
    public double MeanErrorRatePercent { get; set; }
    public double MeanPathEfficiencyPercent { get; set; }
    public List<MazeRunResult> Runs { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string ToMarkdownReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║       🎮 HEXAI YAPAY ZEKA ZAMAN KARARLILIĞI & LABİRENT RAPORU  ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Tarih                  : {Timestamp:dd/MM/yyyy HH:mm:ss,-35}║");
        sb.AppendLine($"║  Toplam Test Döngüsü    : {TotalRuns,-35}║");
        sb.AppendLine($"║  Çözüm Başarısı (Acc)   : %{AccuracyPercent:F2} ({SolvedCount}/{TotalRuns}){"",-23}║");
        sb.AppendLine($"║  Zaman Kararlılığı      : %{TimeStabilityIndexPercent:F2} (Sapma: {StdDevTimeMs:F3} ms){"",-15}║");
        sb.AppendLine($"║  Ortalama Çözüm Süresi  : {MeanTimeMs:F3} ms (Min: {MinTimeMs:F3}, Max: {MaxTimeMs:F3}){"",-10}║");
        sb.AppendLine($"║  Ortalama Hata Oranı    : %{MeanErrorRatePercent:F2}{"",-35}║");
        sb.AppendLine($"║  Yol Verimliliği        : %{MeanPathEfficiencyPercent:F2}{"",-35}║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine("📋 Detaylı Çalıştırma Kayıtları (İlk 10):");
        sb.AppendLine("| # | Süre (ms) | Atılan Adım | Optimal | Ölü Nokta | Hata Oranı | Verimlilik | Durum |");
        sb.AppendLine("|---|-----------|-------------|---------|-----------|------------|------------|-------|");
        foreach (var r in Runs.Take(10))
        {
            string status = r.Solved ? "✅ Başarılı" : "❌ Başarısız";
            sb.AppendLine($"| {r.RunIndex} | {r.ElapsedMs:F3} | {r.TotalSteps} | {r.OptimalSteps} | {r.DeadEndsEncountered} | %{r.ErrorRatePercent:F1} | %{r.PathEfficiencyPercent:F1} | {status} |");
        }
        if (Runs.Count > 10)
        {
            sb.AppendLine($"... ve {Runs.Count - 10} adet ek test tamamlandı.");
        }
        return sb.ToString();
    }
}

/// <summary>
/// Labirent Tabanlı Yapay Zeka Çözüm ve Kararlılık Ölçüm Motoru.
/// </summary>
public static class MazeBenchmark
{
    private static readonly (int dx, int dy)[] Moves = { (0, -1), (1, 0), (0, 1), (-1, 0) }; // Yukarı, Sağ, Aşağı, Sol

    /// <summary>
    /// BFS ile optimal en kısa yolu bulur (Kıyaslama referansı).
    /// </summary>
    public static List<(int X, int Y)> SolveOptimalBFS(MazeGrid maze)
    {
        var start = maze.Start;
        var goal = maze.Goal;

        var queue = new Queue<(int X, int Y)>();
        var parent = new Dictionary<(int X, int Y), (int X, int Y)>();
        var visited = new HashSet<(int X, int Y)>();

        queue.Enqueue(start);
        visited.Add(start);

        bool found = false;
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == goal)
            {
                found = true;
                break;
            }

            foreach (var (dx, dy) in Moves)
            {
                var next = (cur.X + dx, cur.Y + dy);
                if (maze.IsPassable(next.Item1, next.Item2) && !visited.Contains(next))
                {
                    visited.Add(next);
                    parent[next] = cur;
                    queue.Enqueue(next);
                }
            }
        }

        var path = new List<(int X, int Y)>();
        if (!found) return path;

        var trace = goal;
        while (trace != start)
        {
            path.Add(trace);
            trace = parent[trace];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Yapay Zeka Heuristik Karar Ajanı ile labirenti adım adım çözer.
    /// Karar hatalarını, ölü noktaları ve yürütme süresini kaydeder.
    /// </summary>
    public static MazeRunResult SolveWithAIAgent(MazeGrid maze, int runIndex = 1)
    {
        var sw = Stopwatch.StartNew();

        var optimalPath = SolveOptimalBFS(maze);
        int optimalSteps = Math.Max(1, optimalPath.Count - 1);

        var start = maze.Start;
        var goal = maze.Goal;

        var current = start;
        var path = new List<(int X, int Y)> { current };
        var deadEnds = new List<(int X, int Y)>();
        var visitCount = new Dictionary<(int X, int Y), int> { [current] = 1 };
        var decisionStack = new Stack<(int X, int Y)>();
        decisionStack.Push(current);

        int maxStepBudget = maze.Width * maze.Height * 5;
        int deadEndsEncountered = 0;
        int backtrackSteps = 0;
        bool solved = false;

        while (path.Count < maxStepBudget)
        {
            if (current == goal)
            {
                solved = true;
                break;
            }

            // Çıkış yollarını değerlendir
            var validMoves = new List<(int nx, int ny, double score)>();
            foreach (var (dx, dy) in Moves)
            {
                int nx = current.X + dx;
                int ny = current.Y + dy;
                if (!maze.IsPassable(nx, ny)) continue;

                int visits = visitCount.GetValueOrDefault((nx, ny), 0);
                // Heuristik Puanı: Manhattan uzaklığı + Ziyaret cezası
                double dist = Math.Abs(nx - goal.X) + Math.Abs(ny - goal.Y);
                double penalty = visits * 15.0; // Daha önce ziyaret edildiyse ceza puanı
                double score = -(dist + penalty); // Yüksek puan tercih edilir

                validMoves.Add((nx, ny, score));
            }

            if (validMoves.Count == 0 || validMoves.All(m => visitCount.GetValueOrDefault((m.nx, m.ny), 0) >= 3))
            {
                // Ölü nokta / kapana kısılma
                deadEndsEncountered++;
                deadEnds.Add(current);

                if (decisionStack.Count > 1)
                {
                    decisionStack.Pop();
                    current = decisionStack.Peek();
                    backtrackSteps++;
                    path.Add(current);
                    continue;
                }
                else break;
            }

            // En iyi puanlı hamleyi seç
            validMoves.Sort((a, b) => b.score.CompareTo(a.score));
            var best = validMoves[0];

            // Eğer tek bir geçiş varsa ve daha önce geçilmişse dead-end'dir
            if (validMoves.Count == 1 && visitCount.GetValueOrDefault((best.nx, best.ny), 0) > 0)
            {
                deadEndsEncountered++;
                deadEnds.Add(current);
            }

            current = (best.nx, best.ny);
            visitCount[current] = visitCount.GetValueOrDefault(current, 0) + 1;
            path.Add(current);
            decisionStack.Push(current);
        }

        sw.Stop();
        double elapsedMs = sw.Elapsed.TotalMilliseconds;

        int totalSteps = Math.Max(1, path.Count - 1);
        double errorRate = Math.Min(100.0, ((double)(deadEndsEncountered * 2 + backtrackSteps) / totalSteps) * 100.0);
        double efficiency = Math.Min(100.0, ((double)optimalSteps / totalSteps) * 100.0);

        return new MazeRunResult
        {
            RunIndex = runIndex,
            MazeWidth = maze.Width,
            MazeHeight = maze.Height,
            Solved = solved,
            ElapsedMs = elapsedMs,
            TotalSteps = totalSteps,
            OptimalSteps = optimalSteps,
            DeadEndsEncountered = deadEndsEncountered,
            BacktrackSteps = backtrackSteps,
            ErrorRatePercent = errorRate,
            PathEfficiencyPercent = efficiency,
            TraversedPath = path,
            OptimalPath = optimalPath,
            DeadEnds = deadEnds
        };
    }

    /// <summary>
    /// Çoklu rastgele labirent oluşturup çözerek Zaman Kararlılığı (Time Stability),
    /// Ortalama Gecikme (Jitter/StdDev) ve Hata Oranlarını hesaplar.
    /// </summary>
    public static MazeStabilityReport RunStabilityBenchmark(int runs = 10, int width = 21, int height = 21,
        Action<int, int, MazeRunResult>? onProgress = null)
    {
        var report = new MazeStabilityReport { TotalRuns = runs };
        var results = new List<MazeRunResult>();

        for (int i = 1; i <= runs; i++)
        {
            var maze = MazeGrid.GenerateRandom(width, height);
            var result = SolveWithAIAgent(maze, i);
            results.Add(result);
            onProgress?.Invoke(i, runs, result);
        }

        report.Runs = results;
        report.SolvedCount = results.Count(r => r.Solved);
        report.AccuracyPercent = (double)report.SolvedCount / runs * 100.0;

        var times = results.Select(r => r.ElapsedMs).ToList();
        report.MeanTimeMs = times.Average();
        report.MinTimeMs = times.Min();
        report.MaxTimeMs = times.Max();
        report.JitterMs = report.MaxTimeMs - report.MinTimeMs;

        // Standart Sapma
        double sumSquares = times.Sum(t => Math.Pow(t - report.MeanTimeMs, 2));
        report.StdDevTimeMs = Math.Sqrt(sumSquares / Math.Max(1, times.Count - 1));

        // Zaman Kararlılığı İndeksi (Stability Index): Varyasyon Katsayısı (CV = sigma / mu) üzerinden hesaplanır.
        // Düşük sapma = yüksek kararlılık (100%'e yakın).
        double cv = report.MeanTimeMs > 0 ? (report.StdDevTimeMs / report.MeanTimeMs) : 0;
        report.TimeStabilityIndexPercent = Math.Clamp(100.0 - (cv * 100.0), 0.0, 100.0);

        report.MeanErrorRatePercent = results.Average(r => r.ErrorRatePercent);
        report.MeanPathEfficiencyPercent = results.Average(r => r.PathEfficiencyPercent);

        return report;
    }
}
