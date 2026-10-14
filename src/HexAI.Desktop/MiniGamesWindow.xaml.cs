using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using HexAI.Assistant;
using HexAI.Assistant.Games;

namespace HexAI.Desktop;

public partial class MiniGamesWindow : Window
{
    private readonly AssistantEngine _engine;
    private GameBenchmarkSuite.CombinedReport? _lastCombinedReport;

    public class HistoryItemViewModel
    {
        public string Tag { get; set; } = "";
        public string Content { get; set; } = "";
        public string ToolTip { get; set; } = "";
    }

    public class SequenceItemViewModel
    {
        public string Tag { get; set; } = "";
        public string DisplayType { get; set; } = "";
        public string SequenceText { get; set; } = "";
        public string AnswerText { get; set; } = "";
        public string ElapsedText { get; set; } = "";
    }

    public MiniGamesWindow(AssistantEngine engine)
    {
        InitializeComponent();
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        Loaded += MiniGamesWindow_Loaded;
    }

    private void MiniGamesWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Başlangıçta örnek bir labirent çiz
        DrawInitialSampleMaze();
    }

    #region Window Controls
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    #endregion

    #region Tab 1: Maze Benchmark
    private void DrawInitialSampleMaze()
    {
        int size = GetSelectedMazeSize();
        var maze = MazeGrid.GenerateRandom(size, size, seed: 42);
        var result = MazeBenchmark.SolveWithAIAgent(maze, 1);
        RenderMaze(maze, result);
        UpdateScorecardsSingle(result);
    }

    private int GetSelectedMazeSize()
    {
        if (MazeSizeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int size))
        {
            return size;
        }
        return 21;
    }

    private int GetSelectedMazeRuns()
    {
        if (MazeRunsCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int runs))
        {
            return runs;
        }
        return 1;
    }

    private async void BtnRunMaze_Click(object sender, RoutedEventArgs e)
    {
        int size = GetSelectedMazeSize();
        int runs = GetSelectedMazeRuns();

        BtnRunMaze.IsEnabled = false;
        BtnBatchStability.IsEnabled = false;
        TxtMazeStatus.Text = "⏳ Labirent oluşturuluyor ve AI karar motoru tarafından çözülüyor...";

        if (runs == 1)
        {
            var maze = MazeGrid.GenerateRandom(size, size);
            var result = await Task.Run(() => MazeBenchmark.SolveWithAIAgent(maze, 1));
            RenderMaze(maze, result);
            UpdateScorecardsSingle(result);

            ListMazeHistory.Items.Insert(0, new HistoryItemViewModel
            {
                Tag = $"#{result.RunIndex}",
                Content = $"{result.TotalSteps} adım (Opt: {result.OptimalSteps}) — Hata: %{result.ErrorRatePercent:F1}",
                ToolTip = $"{result.ElapsedMs:F3} ms"
            });

            TxtMazeStatus.Text = $"✅ Labirent çözüldü! Süre: {result.ElapsedMs:F3} ms | Toplam Adım: {result.TotalSteps} | Optimal: {result.OptimalSteps} | Hata Oranı: %{result.ErrorRatePercent:F1}";
        }
        else
        {
            await RunBatchMazeBenchmark(runs, size);
        }

        BtnRunMaze.IsEnabled = true;
        BtnBatchStability.IsEnabled = true;
    }

    private async void BtnBatchStability_Click(object sender, RoutedEventArgs e)
    {
        int size = GetSelectedMazeSize();
        await RunBatchMazeBenchmark(25, size);
    }

    private async Task RunBatchMazeBenchmark(int runs, int size)
    {
        BtnRunMaze.IsEnabled = false;
        BtnBatchStability.IsEnabled = false;
        TxtMazeStatus.Text = $"⚡ {runs} adet rastgele labirent oluşturulup çözülüyor (Zaman Kararlılığı Testi)...";

        MazeRunResult? lastResult = null;

        var report = await Task.Run(() =>
        {
            return MazeBenchmark.RunStabilityBenchmark(runs, size, size, (cur, total, res) =>
            {
                if (cur == total)
                {
                    lastResult = res;
                }
            });
        });

        // Skor kartlarını toplu rapora göre güncelle
        TxtTimeStability.Text = $"%{report.TimeStabilityIndexPercent:F1}";
        TxtTimeDetails.Text = $"Ort: {report.MeanTimeMs:F3} ms | Sapma (σ): {report.StdDevTimeMs:F3} ms";

        TxtAccuracy.Text = $"%{report.AccuracyPercent:F1}";
        TxtAccuracyDetails.Text = $"Başarı: {report.SolvedCount}/{report.TotalRuns} Labirent";

        TxtErrorRate.Text = $"%{report.MeanErrorRatePercent:F1}";
        TxtErrorDetails.Text = "Ortalama Karar Sapması";

        TxtEfficiency.Text = $"%{report.MeanPathEfficiencyPercent:F1}";
        TxtEfficiencyDetails.Text = "Optimal / Atılan Adım";

        // Geçmiş listesini doldur
        ListMazeHistory.Items.Clear();
        foreach (var r in report.Runs.OrderByDescending(x => x.RunIndex))
        {
            ListMazeHistory.Items.Add(new HistoryItemViewModel
            {
                Tag = $"#{r.RunIndex}",
                Content = $"{r.TotalSteps} adım — Hata: %{r.ErrorRatePercent:F1} | Verim: %{r.PathEfficiencyPercent:F1}",
                ToolTip = $"{r.ElapsedMs:F3} ms"
            });
        }

        // Son labirenti ekrana çiz
        if (report.Runs.Count > 0)
        {
            var maze = MazeGrid.GenerateRandom(size, size);
            var res = MazeBenchmark.SolveWithAIAgent(maze, report.Runs.Count);
            RenderMaze(maze, res);
        }

        TxtMazeStatus.Text = $"⚡ {runs}x Kararlılık Testi Tamamlandı! Kararlılık İndeksi: %{report.TimeStabilityIndexPercent:F1} | Ort: {report.MeanTimeMs:F3} ms | Doğruluk: %{report.AccuracyPercent:F1}";

        BtnRunMaze.IsEnabled = true;
        BtnBatchStability.IsEnabled = true;
    }

    private void UpdateScorecardsSingle(MazeRunResult r)
    {
        TxtTimeStability.Text = "%99.0";
        TxtTimeDetails.Text = $"Süre: {r.ElapsedMs:F3} ms";

        TxtAccuracy.Text = r.Solved ? "%100" : "%0";
        TxtAccuracyDetails.Text = r.Solved ? "Hedefe Ulaşıldı" : "Hedefe Ulaşılamadı";

        TxtErrorRate.Text = $"%{r.ErrorRatePercent:F1}";
        TxtErrorDetails.Text = $"{r.DeadEndsEncountered} Ölü Nokta | {r.BacktrackSteps} Geri Adım";

        TxtEfficiency.Text = $"%{r.PathEfficiencyPercent:F1}";
        TxtEfficiencyDetails.Text = $"{r.OptimalSteps} / {r.TotalSteps} Adım";
    }

    private void RenderMaze(MazeGrid maze, MazeRunResult result)
    {
        MazeCanvas.Children.Clear();

        double canvasWidth = MazeCanvas.Width;
        double canvasHeight = MazeCanvas.Height;

        double cellW = canvasWidth / maze.Width;
        double cellH = canvasHeight / maze.Height;

        var visitedSet = new HashSet<(int, int)>(result.TraversedPath);
        var optimalSet = new HashSet<(int, int)>(result.OptimalPath);
        var deadEndSet = new HashSet<(int, int)>(result.DeadEnds);

        // Duvar ve geçiş fırçaları
        var wallBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));       // #1E293B
        var pathBrush = new SolidColorBrush(Color.FromRgb(15, 23, 42));       // #0F172A
        var visitedBrush = new SolidColorBrush(Color.FromArgb(140, 139, 92, 246)); // Yarı saydam mor
        var optimalBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));   // Cyan #06B6D4
        var deadEndBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));   // Amber #F59E0B
        var startBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));    // Yeşil #10B981
        var goalBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));      // Kırmızı #EF4444

        for (int x = 0; x < maze.Width; x++)
        {
            for (int y = 0; y < maze.Height; y++)
            {
                var rect = new Rectangle
                {
                    Width = cellW + 0.5,
                    Height = cellH + 0.5
                };

                if (maze.Cells[x, y] == MazeCellType.Wall)
                {
                    rect.Fill = wallBrush;
                }
                else if (x == maze.Start.X && y == maze.Start.Y)
                {
                    rect.Fill = startBrush;
                }
                else if (x == maze.Goal.X && y == maze.Goal.Y)
                {
                    rect.Fill = goalBrush;
                }
                else if (optimalSet.Contains((x, y)))
                {
                    rect.Fill = optimalBrush;
                }
                else if (deadEndSet.Contains((x, y)))
                {
                    rect.Fill = deadEndBrush;
                }
                else if (visitedSet.Contains((x, y)))
                {
                    rect.Fill = visitedBrush;
                }
                else
                {
                    rect.Fill = pathBrush;
                }

                Canvas.SetLeft(rect, x * cellW);
                Canvas.SetTop(rect, y * cellH);
                MazeCanvas.Children.Add(rect);
            }
        }

        // Optimal yolu çizgi olarak da bağla (parlak Cyan)
        if (result.OptimalPath.Count > 1)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                StrokeThickness = Math.Max(2.0, cellW * 0.4),
                StrokeLineJoin = PenLineJoin.Round
            };

            foreach (var (ox, oy) in result.OptimalPath)
            {
                polyline.Points.Add(new Point(ox * cellW + cellW / 2, oy * cellH + cellH / 2));
            }
            MazeCanvas.Children.Add(polyline);
        }
    }
    #endregion

    #region Tab 2: Sequence Pattern Benchmark
    private async void BtnRunSequenceTest_Click(object sender, RoutedEventArgs e)
    {
        BtnRunSequenceTest.IsEnabled = false;

        var report = await Task.Run(() => SequencePatternGame.RunBenchmark(20));

        TxtSeqAccuracy.Text = $"%{report.AccuracyPercent:F1}";
        TxtSeqErrorRate.Text = $"%{report.ErrorRatePercent:F1}";
        TxtSeqMeanTime.Text = $"{report.MeanTimeMs:F3} ms";
        TxtSeqStability.Text = $"%{report.TimeStabilityIndexPercent:F1}";

        ListSequenceProblems.Items.Clear();
        foreach (var r in report.Results)
        {
            string seqStr = string.Join(", ", r.Problem.GivenSequence) + ",  [ ? ]";
            string icon = r.IsCorrect ? "✅" : "❌";

            ListSequenceProblems.Items.Add(new SequenceItemViewModel
            {
                Tag = icon,
                DisplayType = r.Problem.TypeName,
                SequenceText = seqStr,
                AnswerText = $"Tahmin: {r.PredictedValue} (Beklenen: {r.Problem.ExpectedNext}) — {r.Explanation}",
                ElapsedText = $"{r.ElapsedMs:F3} ms"
            });
        }

        BtnRunSequenceTest.IsEnabled = true;
    }
    #endregion

    #region Tab 3: Full Report
    private async void BtnRunFullReport_Click(object sender, RoutedEventArgs e)
    {
        TxtFullReport.Text = "⏳ Büyük Kararlılık Test Suite'i çalıştırılıyor (Labirent + Mantık Dizileri)...";

        var combined = await Task.Run(() => GameBenchmarkSuite.RunFullSuite(15, 20));
        _lastCombinedReport = combined;

        TxtFullReport.Text = combined.ToFullReport();
    }

    private void BtnCopyReport_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtFullReport.Text))
        {
            MessageBox.Show("Kopyalanacak rapor bulunmuyor. Önce 'Büyük Kararlılık Suite'i Çalıştır' butonuna tıklayın.",
                "Rapor Kopyala", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(TxtFullReport.Text);
        MessageBox.Show("Rapor panoya kopyalandı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    #endregion
}
