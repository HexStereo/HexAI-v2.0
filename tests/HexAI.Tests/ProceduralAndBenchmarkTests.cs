using System.Text;
using System.Threading.Tasks;
using HexAI.Assistant;
using HexAI.Assistant.Config;
using HexAI.Assistant.Evaluation;
using HexAI.Assistant.Knowledge;
using HexAI.Assistant.Research;
using Xunit;

namespace HexAI.Tests;

public class ProceduralAndBenchmarkTests
{
    private static readonly AssistantEngine SharedEngine;

    static ProceduralAndBenchmarkTests()
    {
        var config = AssistantConfig.Load();
        config.ApplyHardwareTier(HardwareTier.Standard);
        config.HiddenSize = 32;
        config.NumHeads = 1;
        config.NumLayers = 1;
        config.SequenceLength = 16;
        config.DefaultEpochs = 1;
        config.AutoWebResearch = true;
        SharedEngine = new AssistantEngine(config);
        SharedEngine.Initialize();
    }

    [Fact]
    public async Task TestMakarnaNasilYapilirDirectStepByStep()
    {
        var engine = SharedEngine;

        // 1. Doğrudan "makarna nasıl yapılır"
        var sb = new StringBuilder();
        await foreach (var token in engine.ProcessMessageStreamAsync("makarna nasıl yapılır"))
        {
            sb.Append(token);
        }
        string reply = sb.ToString();

        Assert.NotEmpty(reply);
        Assert.Contains("Makarna Nasıl Yapılır", reply);
        Assert.Contains("Gerekli Malzemeler", reply);
        Assert.Contains("Adım Adım Yapılışı", reply);
        Assert.Contains("Suyu Kaynatın", reply);
        Assert.Contains("Tuz", reply);
        Assert.Contains("Süzme", reply);

        // 2. "bana makarna nasıl yapılır anlat"
        var sb2 = new StringBuilder();
        await foreach (var token in engine.ProcessMessageStreamAsync("bana makarna nasıl yapılır anlat"))
        {
            sb2.Append(token);
        }
        string reply2 = sb2.ToString();

        Assert.NotEmpty(reply2);
        Assert.Contains("Adım Adım", reply2);
        Assert.Contains("Suyu Kaynatın", reply2);
    }

    [Fact]
    public void TestBenchmarkSuiteHighGrade()
    {
        var engine = SharedEngine;
        var result = engine.RunBenchmark();

        Assert.True(result.ScorePercent >= 90.0, $"Benchmark skoru beklenenden düşük: {result.ScorePercent}%");
        Assert.Equal("A", result.Grade);
        Assert.Equal(0, result.WrongAnswers);
    }

    [Fact]
    public void TestEvaluationSuiteAccuracyAndConsistency()
    {
        var engine = SharedEngine;
        var eval = engine.RunEvaluation();

        Assert.True(eval.AccuracyScore >= 40.0, $"Doğruluk skoru beklenenden düşük: {eval.AccuracyScore}");
        Assert.True(eval.HallucinationRate <= 25.0, $"Halüsinasyon oranı beklenenden yüksek: {eval.HallucinationRate}%");
        Assert.NotEqual("F", eval.OverallGrade);
    }

    [Fact]
    public void TestKnowledgeSynthesizerProceduralGuide()
    {
        var research = new ResearchResult
        {
            Question = "makarna nasıl yapılır",
            Sources = new System.Collections.Generic.List<ResearchSource>
            {
                new()
                {
                    Index = 1,
                    Title = "Nefis Yemek Tarifleri",
                    Url = "https://www.nefisyemektarifleri.com",
                    Text = "Geniş tencerede bol suyu kaynatın.\nSu kaynayınca tuzu ekleyin.\nMakarnayı atıp 8 dakika haşlayın.\nSüzgece alıp süzün."
                }
            }
        };

        string synthesized = KnowledgeSynthesizer.Synthesize(research);

        Assert.Contains("Adım Adım", synthesized);
        Assert.Contains("Gerekli Malzemeler", synthesized);
        Assert.Contains("1.", synthesized);
        Assert.Contains("2.", synthesized);
        Assert.Contains("İncelenen ve Bilgi Alınan Kaynaklar", synthesized);
    }
}
