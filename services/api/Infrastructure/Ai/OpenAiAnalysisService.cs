using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Ai;

/// <summary>
/// Calls the OpenAI Chat Completions API (GPT-4o with vision support).
/// Images are base64-encoded and sent inline.
/// PDFs are analysed based on their filename + a text-only prompt.
/// Retries with exponential backoff on 429 TooManyRequests and guardrail failures.
/// </summary>
public class OpenAiAnalysisService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OpenAiAnalysisService> logger) : IAiAnalysisService
{
    private const int MaxRetries = 3;

    private static readonly string[] ImageTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];

    private static readonly string SystemPrompt = """
        You are an expert software architect.
        Analyse the provided architecture diagram and return ONLY a valid JSON object with exactly these three keys:
        {
          "components": "<list the identified components, services, databases, queues, and their relationships>",
          "risks": "<identify architectural risks, single points of failure, security concerns, scalability bottlenecks>",
          "recommendations": "<provide concrete improvements, best practices, and architectural recommendations>"
        }
        Be concise but thorough. Return only the JSON object, no markdown, no explanation.
        """;

    public async Task<AiAnalysisResult> AnalyzeAsync(
        string filePath,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
        var model  = configuration["OpenAI:Model"] ?? "gpt-4o";

        logger.LogInformation("Calling OpenAI ({Model}) for file: {FileName}", model, fileName);

        var isImage  = ImageTypes.Contains(contentType);
        var messages = isImage
            ? await BuildImageMessagesAsync(filePath, contentType, cancellationToken)
            : BuildTextMessages(fileName);

        var payload = JsonSerializer.Serialize(new
        {
            model,
            messages,
            max_tokens      = 1500,
            response_format = new { type = "json_object" }
        });

        return await SendWithRetryAsync(apiKey, payload, cancellationToken);
    }

    private async Task<AiAnalysisResult> SendWithRetryAsync(
        string apiKey, string payload, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            var client  = httpClientFactory.CreateClient("openai");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", apiKey) },
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, cancellationToken);
            var json     = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var node    = JsonNode.Parse(json)!;
                var content = node["choices"]![0]!["message"]!["content"]!.GetValue<string>();
                var result  = JsonSerializer.Deserialize<AiJsonResult>(content)
                    ?? throw new InvalidOperationException("Could not deserialize OpenAI structured response.");

                var analysisResult = new AiAnalysisResult(result.components, result.risks, result.recommendations);

                try
                {
                    return AiOutputGuardrails.ValidateAndSanitize(analysisResult, logger, "OpenAI");
                }
                catch (AiGuardrailException ex) when (attempt < MaxRetries)
                {
                    logger.LogWarning(
                        "OpenAI guardrail failed (attempt {Attempt}/{Max}): {Message}. Retrying…",
                        attempt, MaxRetries, ex.Message);
                    continue;
                }
            }

            // 429 = rate limited — wait and retry with exponential backoff
            if ((int)response.StatusCode == 429 && attempt < MaxRetries)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? delay;
                logger.LogWarning(
                    "OpenAI rate limit hit (attempt {Attempt}/{Max}). Retrying in {Delay}s…",
                    attempt, MaxRetries, retryAfter.TotalSeconds);

                await Task.Delay(retryAfter, cancellationToken);
                delay *= 2; // 5s → 10s → 20s
                continue;
            }

            logger.LogError("OpenAI API error {Status}: {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"OpenAI API returned {response.StatusCode}.");
        }

        throw new InvalidOperationException("OpenAI API rate limit exceeded after maximum retries.");
    }

    private static async Task<object[]> BuildImageMessagesAsync(
        string filePath, string contentType, CancellationToken ct)
    {
        var bytes   = await File.ReadAllBytesAsync(filePath, ct);
        var base64  = Convert.ToBase64String(bytes);
        var dataUri = $"data:{contentType};base64,{base64}";

        return
        [
            new { role = "system", content = SystemPrompt },
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text",      text = "Analyse this architecture diagram." },
                    new { type = "image_url", image_url = new { url = dataUri, detail = "high" } }
                }
            }
        ];
    }

    private static object[] BuildTextMessages(string fileName) =>
    [
        new { role = "system", content = SystemPrompt },
        new
        {
            role    = "user",
            content = $"Analyse this architecture document. File name: '{fileName}'. " +
                      "Provide a general analysis based on what a document with this name likely contains."
        }
    ];

    private record AiJsonResult(string components, string risks, string recommendations);
}
