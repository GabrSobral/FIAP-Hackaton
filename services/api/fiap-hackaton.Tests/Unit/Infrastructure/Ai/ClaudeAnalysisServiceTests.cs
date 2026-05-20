using System.Text.Json;
using fiap_hackaton.Infrastructure.Ai;
using fiap_hackaton.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiap_hackaton.Tests.Unit.Infrastructure.Ai;

public class ClaudeAnalysisServiceTests
{
    // Valid inner JSON that Claude is expected to return inside the "text" field.
    private const string ValidInnerJson =
        """{"components":["**API Gateway** — ponto de entrada principal para todas as requisições externas recebidas"],"risks":["**Ponto Único de Falha** — o gateway não possui redundância e pode causar indisponibilidade total do serviço"],"recommendations":["**Adicionar Load Balancer** — distribuir o tráfego entre múltiplas instâncias eliminando o ponto único de falha"],"feedback":["Arquitetura sólida com lacunas menores de resiliência que precisam ser endereçadas."]}""";

    // Truncated inner JSON: recommendations array never closed.
    private const string TruncatedInnerJson =
        """{"components":["**API Gateway** — ponto de entrada principal para todas as requisições externas"],"risks":["**Ponto Único de Falha** — o gateway não possui redundância e pode causar indisponibilidade total"],"recommendations":["**Adicionar Load Balancer** — distribuir o tráfego entre múltiplas instâncias eliminando o ponto único""";

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anthropic:ApiKey"] = "test-key",
                ["Anthropic:Model"]  = "claude-test"
            })
            .Build();

    // Wraps an inner JSON string into the Claude API response envelope.
    private static string ClaudeApiJson(string innerJson)
    {
        var escapedText = JsonSerializer.Serialize(innerJson);
        return $$"""{"content":[{"text":{{escapedText}}}]}""";
    }

    private static ClaudeAnalysisService Build(params HttpResponseMessage[] responses) =>
        new(FakeHttpMessageHandler.Factory(responses), Config(), NullLogger<ClaudeAnalysisService>.Instance);

    [Fact]
    public async Task AnalyzeAsync_WithValidResponse_ShouldReturnMappedResult()
    {
        var sut = Build(FakeHttpMessageHandler.Ok(ClaudeApiJson(ValidInnerJson)));

        var result = await sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf");

        Assert.Contains("API Gateway", result.Components);
        Assert.Contains("Ponto Único de Falha", result.Risks);
        Assert.Contains("Load Balancer", result.Recommendations);
    }

    [Fact]
    public async Task AnalyzeAsync_WithTruncatedJson_ShouldRepairAndReturnResult()
    {
        // The repaired JSON will have components, risks, and recommendations — enough to pass guardrails.
        var sut = Build(FakeHttpMessageHandler.Ok(ClaudeApiJson(TruncatedInnerJson)));

        var result = await sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf");

        Assert.False(string.IsNullOrWhiteSpace(result.Components));
        Assert.False(string.IsNullOrWhiteSpace(result.Risks));
        Assert.False(string.IsNullOrWhiteSpace(result.Recommendations));
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
        // First call: 429 (with Retry-After: 0 so Task.Delay completes instantly).
        // Second call: 200 with valid response.
        var sut = Build(
            FakeHttpMessageHandler.TooManyRequests(),
            FakeHttpMessageHandler.Ok(ClaudeApiJson(ValidInnerJson)));

        var result = await sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf");

        Assert.False(string.IsNullOrWhiteSpace(result.Components));
    }

    [Fact]
    public async Task AnalyzeAsync_WhenAllRetriesReturn429_ShouldThrowInvalidOperation()
    {
        // Single 429 response repeated — exhausts all MaxRetries (4).
        var sut = Build(FakeHttpMessageHandler.TooManyRequests());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AnalyzeAsync("doc.pdf", "doc.pdf", "application/pdf"));
    }
}
