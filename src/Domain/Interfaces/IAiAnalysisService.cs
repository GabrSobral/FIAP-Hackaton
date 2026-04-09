namespace fiap_hackaton.Domain.Interfaces;

/// <summary>
/// Analyzes an architecture diagram using an LLM (OpenAI / Azure OpenAI).
/// Returns a structured report with Components, Risks and Recommendations.
/// </summary>
public interface IAiAnalysisService
{
    Task<AiAnalysisResult> AnalyzeAsync(
        string filePath,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}

/// <summary>Structured output produced by the LLM analysis.</summary>
public record AiAnalysisResult(
    string Components,
    string Risks,
    string Recommendations,
    string Feedback = ""
);
