using fiap_hackaton.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiap_hackaton.Tests.Unit.Infrastructure.Ai;

public class StubAiAnalysisServiceTests
{
    private readonly StubAiAnalysisService _sut = new(NullLogger<StubAiAnalysisService>.Instance);

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnNonEmptyComponents()
    {
        var result = await _sut.AnalyzeAsync("arch.pdf", "arch.pdf", "application/pdf");

        Assert.False(string.IsNullOrWhiteSpace(result.Components));
        Assert.False(string.IsNullOrWhiteSpace(result.Risks));
        Assert.False(string.IsNullOrWhiteSpace(result.Recommendations));
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldIncludeFileNameInComponents()
    {
        var result = await _sut.AnalyzeAsync("my-diagram.png", "my-diagram.png", "image/png");

        Assert.Contains("my-diagram.png", result.Components);
    }
}
