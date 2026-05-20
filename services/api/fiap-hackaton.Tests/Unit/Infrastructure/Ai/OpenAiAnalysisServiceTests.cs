using System.Text.Json;
using fiap_hackaton.Infrastructure.Ai;
using fiap_hackaton.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiap_hackaton.Tests.Unit.Infrastructure.Ai;

public class OpenAiAnalysisServiceTests
{
    private const string ValidInnerJson =
        """{"components":"API Gateway → Application Service → PostgreSQL database with connections","risks":"Single point of failure on the gateway layer causing total service outage","recommendations":"Add a load balancer and implement circuit breaker pattern for better resilience"}""";

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-key",
                ["OpenAI:Model"]  = "gpt-test"
            })
            .Build();

    // Wraps an inner JSON string into the OpenAI Chat Completions response envelope.
    private static string OpenAiApiJson(string innerContent)
    {
        var escapedContent = JsonSerializer.Serialize(innerContent);
        return $$$"""{"choices":[{"message":{"content":{{{escapedContent}}}}}]}""";
    }

    private static OpenAiAnalysisService Build(params HttpResponseMessage[] responses) =>
        new(FakeHttpMessageHandler.Factory(responses), Config(), NullLogger<OpenAiAnalysisService>.Instance);

    [Fact]
    public async Task AnalyzeAsync_WithValidResponse_ShouldReturnMappedResult()
    {
        var sut = Build(FakeHttpMessageHandler.Ok(OpenAiApiJson(ValidInnerJson)));

        var result = await sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf");

        Assert.Contains("API Gateway", result.Components);
        Assert.Contains("Single point of failure", result.Risks);
        Assert.Contains("load balancer", result.Recommendations);
    }

    [Fact]
    public async Task AnalyzeAsync_OnHttpError_ShouldThrowInvalidOperation()
    {
        var sut = Build(FakeHttpMessageHandler.InternalServerError());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf"));
    }

    [Fact]
    public async Task AnalyzeAsync_OnRateLimit_ShouldRetryAndSucceed()
    {
        var sut = Build(
            FakeHttpMessageHandler.TooManyRequests(),
            FakeHttpMessageHandler.Ok(OpenAiApiJson(ValidInnerJson)));

        var result = await sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf");

        Assert.False(string.IsNullOrWhiteSpace(result.Components));
    }

    [Fact]
    public async Task AnalyzeAsync_WhenAllRetriesReturn429_ShouldThrowInvalidOperation()
    {
        var sut = Build(FakeHttpMessageHandler.TooManyRequests());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf"));
    }
}
