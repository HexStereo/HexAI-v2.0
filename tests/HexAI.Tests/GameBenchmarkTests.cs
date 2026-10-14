namespace HexAI.Tests;

using Xunit;
using HexAI.Assistant.Games;
using HexAI.Assistant.Config;
using HexAI.Assistant.Research;
using HexAI.Assistant.Knowledge;
using System;
using System.IO;

public class GameBenchmarkTests
{
    [Fact]
    public void TestMazeGenerationAndSolving()
    {
        var maze = MazeGrid.GenerateRandom(15, 15, seed: 42);
        Assert.NotNull(maze);
        Assert.Equal(15, maze.Width);
        Assert.Equal(15, maze.Height);

        var result = MazeBenchmark.SolveWithAIAgent(maze, runIndex: 1);
        Assert.NotNull(result);
        Assert.True(result.Solved, "Maze should be solved.");
        Assert.NotEmpty(result.OptimalPath);
        Assert.NotEmpty(result.TraversedPath);
        Assert.True(result.ErrorRatePercent >= 0 && result.ErrorRatePercent <= 100);
        Assert.True(result.PathEfficiencyPercent >= 0);
        Assert.True(result.ElapsedMs >= 0);
    }

    [Fact]
    public void TestMazeBenchmarkBatchStability()
    {
        var result = MazeBenchmark.RunStabilityBenchmark(runs: 10, width: 15, height: 15);

        Assert.NotNull(result);
        Assert.Equal(10, result.TotalRuns);
        Assert.Equal(10, result.SolvedCount);
        Assert.True(result.TimeStabilityIndexPercent >= 0 && result.TimeStabilityIndexPercent <= 100);
        Assert.True(result.AccuracyPercent >= 0 && result.AccuracyPercent <= 100);
        Assert.True(result.MeanErrorRatePercent >= 0 && result.MeanErrorRatePercent <= 100);
        Assert.NotEmpty(result.Runs);
    }

    [Fact]
    public void TestSequencePatternPuzzleAndAIReasoning()
    {
        var rng = new Random(123);
        var problem = SequencePatternGame.GenerateRandomProblem(id: 1, rng: rng);

        Assert.NotNull(problem);
        Assert.NotEmpty(problem.GivenSequence);
        Assert.True(problem.GivenSequence.Count >= 4);

        var answer = SequencePatternGame.Solve(problem);
        Assert.NotNull(answer);
        Assert.True(answer.ElapsedMs >= 0);
        Assert.False(string.IsNullOrWhiteSpace(answer.Explanation));
    }

    [Fact]
    public void TestSequenceBenchmarkBatch()
    {
        var benchmark = SequencePatternGame.RunBenchmark(count: 12);

        Assert.NotNull(benchmark);
        Assert.Equal(12, benchmark.TotalProblems);
        Assert.True(benchmark.AccuracyPercent >= 0 && benchmark.AccuracyPercent <= 100);
        Assert.True(benchmark.TimeStabilityIndexPercent >= 0 && benchmark.TimeStabilityIndexPercent <= 100);
        Assert.NotEmpty(benchmark.Results);
    }

    [Fact]
    public void TestFullGameBenchmarkSuite()
    {
        var report = GameBenchmarkSuite.RunFullSuite(mazeRuns: 5, sequenceCount: 6);

        Assert.NotNull(report);
        Assert.Equal(5, report.MazeReport.TotalRuns);
        Assert.Equal(6, report.SequenceReport.TotalProblems);
        Assert.True(report.OverallStabilityScore >= 0 && report.OverallStabilityScore <= 100);

        string md = report.ToFullReport();
        Assert.Contains("HEXAI MINI-GAMES", md);
        Assert.Contains("HEXAI YAPAY ZEKA ZAMAN KARARLILIĞI", md);
    }

    [Fact]
    public void TestUltraHardwareTierConfig()
    {
        var config = new AssistantConfig();
        config.ApplyHardwareTier(HardwareTier.Ultra);

        Assert.Equal(HardwareTier.Ultra, config.HardwareProfile);
        Assert.Equal(256, config.HiddenSize);
        Assert.Equal(8, config.NumHeads);
        Assert.Equal(4, config.NumLayers);
        Assert.Equal(32, config.SequenceLength);
        Assert.Equal(6, config.ResearchMaxSources);
        Assert.Equal(5000, config.ResearchCharsPerSource);
    }

    [Fact]
    public void TestResearchEngineQuestionDetection()
    {
        // Conceptual / factual Turkish questions should trigger research
        Assert.True(ResearchEngine.ShouldResearch("kuantum bilgisayar nedir"));
        Assert.True(ResearchEngine.ShouldResearch("karadelikler nasıl oluşur?"));
        Assert.True(ResearchEngine.ShouldResearch("en son teknoloji haberlerini araştır"));
        Assert.True(ResearchEngine.ShouldResearch("RTX 5090 özellikleri"));

        // Casual conversation should not trigger web research
        Assert.False(ResearchEngine.ShouldResearch("merhaba"));
        Assert.False(ResearchEngine.ShouldResearch("nasılsın"));
        Assert.False(ResearchEngine.ShouldResearch("selam"));
    }

    [Fact]
    public void TestKnowledgeBasePersistenceAndSearch()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "HexAI_Test_KB_" + Guid.NewGuid().ToString("N"));
        try
        {
            var kb = new KnowledgeBase(tempDir);
            kb.Save("Derin öğrenme nedir", "Derin öğrenme çok katmanlı yapay sinir ağları ile öğrenmedir.", 9.5, "Test");

            Assert.Equal(1, kb.Count);

            var found = kb.Search("Derin öğrenme nedir");
            Assert.NotNull(found);
            Assert.Contains("sinir ağları", found.Answer);

            // Re-instantiate from same folder to verify JSON persistence
            var kb2 = new KnowledgeBase(tempDir);
            Assert.Equal(1, kb2.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task TestAssistantEngineConversationalFlowAndGamesCommand()
    {
        var config = new AssistantConfig();
        config.AutoWebResearch = false; // test offline conversational path
        var engine = new HexAI.Assistant.AssistantEngine(config);

        // Test conversational dialogue response
        var responseBuilder = new System.Text.StringBuilder();
        await foreach (var chunk in engine.ProcessMessageStreamAsync("merhaba nasılsın"))
        {
            responseBuilder.Append(chunk);
        }
        string reply = responseBuilder.ToString();
        Assert.NotEmpty(reply);
        Assert.True(reply.Length > 10);
        Assert.Contains("Merhaba", reply);

        // Test /games command execution
        var gamesReply = new System.Text.StringBuilder();
        await foreach (var chunk in engine.ProcessMessageStreamAsync("/games"))
        {
            gamesReply.Append(chunk);
        }
        string gamesText = gamesReply.ToString();
        Assert.Contains("HEXAI MINI-GAMES", gamesText);
        Assert.Contains("LABİRENT RAPORU", gamesText);
    }

    [Fact]
    public async System.Threading.Tasks.Task TestChatbotDialogueUnderstandingAndReasoning()
    {
        var config = new AssistantConfig();
        var engine = new HexAI.Assistant.AssistantEngine(config);

        // 1. Sohbet isteği testi
        var chatSb = new System.Text.StringBuilder();
        await foreach (var token in engine.ProcessMessageStreamAsync("benimle sohbet eder misin"))
        {
            chatSb.Append(token);
        }
        string chatReply = chatSb.ToString();
        Assert.NotEmpty(chatReply);
        Assert.False(chatReply.StartsWith("⚠️"));

        // 2. Yazılım ve kodlama sorusu
        var codeSb = new System.Text.StringBuilder();
        await foreach (var token in engine.ProcessMessageStreamAsync("C# ile nasıl kod yazarım ve nelere dikkat etmeliyim?"))
        {
            codeSb.Append(token);
        }
        string codeReply = codeSb.ToString();
        Assert.Contains("Yazılım", codeReply);

        // 3. Plan ve tavsiye isteği
        var planSb = new System.Text.StringBuilder();
        await foreach (var token in engine.ProcessMessageStreamAsync("bana bir çalışma planı yap"))
        {
            planSb.Append(token);
        }
        string planReply = planSb.ToString();
        Assert.Contains("Yol Haritası", planReply);

        // 4. Doğrulanmış olgu sorgusu
        var kbSb = new System.Text.StringBuilder();
        await foreach (var token in engine.ProcessMessageStreamAsync("yapay zeka nedir"))
        {
            kbSb.Append(token);
        }
        string kbReply = kbSb.ToString();
        Assert.NotEmpty(kbReply);
        Assert.False(kbReply.StartsWith("⚠️"));
    }
}
