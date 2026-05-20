using System.Net.Http.Headers;
using System.Text.Json;
using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Ai;

/// <summary>
/// Sends the diagram to the local Python ia-service (FastAPI + Qwen2-VL)
/// and maps the response back to <see cref="AiAnalysisResult"/>.
/// </summary>
public class LocalAiAnalysisService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<LocalAiAnalysisService> logger) : IAiAnalysisService
{
    public async Task<AiAnalysisResult> AnalyzeAsync(
        string filePath,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["LocalAi:BaseUrl"]
            ?? throw new InvalidOperationException("LocalAi:BaseUrl is not configured.");

        logger.LogInformation(
            "Sending '{FileName}' to local AI service at {BaseUrl}", fileName, baseUrl);

        var client = httpClientFactory.CreateClient("localai");

        await using var stream  = File.OpenRead(filePath);
        using var form          = new MultipartFormDataContent();
        var fileContent         = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);

        var response = await client.PostAsync($"{baseUrl}/analyze", form, cancellationToken);
        var json     = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Local AI service returned {Status}: {Body}", response.StatusCode, json);
            throw new InvalidOperationException(
                $"Local AI service returned {response.StatusCode}.");
        }

        var result = JsonSerializer.Deserialize<LocalAiResponse>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Local AI service returned an empty response.");

        logger.LogInformation(
            "Local AI analysis completed in {Ms} ms using {Model}",
            result.ProcessingTimeMs, result.ModelUsed);

        var analysisResult = new AiAnalysisResult(
            result.Components,
            result.Risks,
            result.Recommendations,
            result.Feedback);

        return AiOutputGuardrails.ValidateAndSanitize(analysisResult, logger, "LocalAI");
    }

    private record LocalAiResponse(
        string Components,
        string Risks,
        string Recommendations,
        string Feedback,
        string ModelUsed,
        int ProcessingTimeMs);
}
