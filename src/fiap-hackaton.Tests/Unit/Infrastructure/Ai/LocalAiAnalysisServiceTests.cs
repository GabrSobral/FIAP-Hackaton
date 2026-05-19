using fiap_hackaton.Infrastructure.Ai;
using fiap_hackaton.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiap_hackaton.Tests.Unit.Infrastructure.Ai;

public class LocalAiAnalysisServiceTests : IDisposable
{
    // LocalAiAnalysisService reads the file from disk, so a real temp file is needed.
    private readonly string _tempFile = Path.GetTempFileName();

    private const string ValidLocalAiJson = """
        {
          "components":      "API Gateway → Application Service → PostgreSQL database with connections",
          "risks":           "Single point of failure on the gateway layer causing total service outage",
          "recommendations": "Add a load balancer and implement circuit breaker pattern for resilience",
          "feedback":        "Solid architecture with minor resilience gaps to be addressed soon",
          "modelUsed":       "Qwen/Qwen2-VL-2B-Instruct",
          "processingTimeMs": 1500
        }
        """;

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalAi:BaseUrl"] = "http://localhost:8000"
            })
            .Build();

    private LocalAiAnalysisService Build(params HttpResponseMessage[] responses) =>
        new(FakeHttpMessageHandler.Factory(responses), Config(), NullLogger<LocalAiAnalysisService>.Instance);

    [Fact]
    public async Task AnalyzeAsync_WithValidResponse_ShouldReturnMappedResult()
    {
        var sut = Build(FakeHttpMessageHandler.Ok(ValidLocalAiJson));

        var result = await sut.AnalyzeAsync(_tempFile, "diagram.png", "image/png");

        Assert.Contains("API Gateway", result.Components);
        Assert.Contains("Single point of failure", result.Risks);
        Assert.Contains("load balancer", result.Recommendations);
        Assert.False(string.IsNullOrWhiteSpace(result.Feedback));
    }

    [Fact]
    public async Task AnalyzeAsync_OnHttpError_ShouldThrowInvalidOperation()
    {
        var sut = Build(FakeHttpMessageHandler.InternalServerError());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AnalyzeAsync(_tempFile, "diagram.png", "image/png"));
    }

    public void Dispose() => File.Delete(_tempFile);
}
