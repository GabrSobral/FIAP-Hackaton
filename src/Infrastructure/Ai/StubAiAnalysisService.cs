using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Ai;

/// <summary>
/// Fallback AI service used in development when no OpenAI API key is configured.
/// Returns clearly-labelled placeholder data so the end-to-end flow can be tested locally.
/// </summary>
public class StubAiAnalysisService(ILogger<StubAiAnalysisService> logger) : IAiAnalysisService
{
    public async Task<AiAnalysisResult> AnalyzeAsync(
        string filePath,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "[STUB] No OpenAI API key configured — returning placeholder analysis for '{FileName}'.", fileName);

        // Simulate realistic processing time
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        return new AiAnalysisResult(
            Components:
                "[STUB] API Gateway → Application Service → PostgreSQL database. " +
                $"File '{fileName}' ({contentType}) was received but not analysed by a real LLM. " +
                "Configure OpenAI:ApiKey in appsettings to enable real analysis.",

            Risks:
                "[STUB] Unable to identify real risks without LLM processing. " +
                "Common risks in distributed systems include: single points of failure, " +
                "missing circuit breakers, lack of observability, and unencrypted inter-service communication.",

            Recommendations:
                "[STUB] Configure OpenAI:ApiKey to enable real AI-powered analysis. " +
                "Typical recommendations: add health checks, enforce TLS everywhere, " +
                "implement retry policies, and add distributed tracing."
        );
    }
}
