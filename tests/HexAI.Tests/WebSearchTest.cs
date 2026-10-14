using Xunit;
using Xunit.Abstractions;
using HexAI.Assistant.Research;
using System.Threading.Tasks;

namespace HexAI.Tests;

public class WebSearchTest
{
    private readonly ITestOutputHelper _output;

    public WebSearchTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestSearchDuckDuckGoAndWiki()
    {
        var client = new WebSearchClient();
        var wiki = await client.SearchWikipediaAsync("Makarna", "tr", 1000);
        Assert.NotNull(wiki);
        Assert.Contains("makarna", wiki.Snippet.ToLower());
    }

    [Fact]
    public async Task TestResearchGatherAndSynthesize()
    {
        var engine = new ResearchEngine();
        var res = await engine.GatherAsync("makarna nasıl yapılır");
        _output.WriteLine($"Sources count: {res.Sources.Count}");
        foreach (var s in res.Sources)
        {
            _output.WriteLine($"Source: {s.Title} ({s.Url}) -> text length: {s.Text.Length}");
        }
        string report = engine.SynthesizeReport(res);
        _output.WriteLine("--- REPORT ---");
        _output.WriteLine(report);
        _output.WriteLine("--------------");
    }
}
