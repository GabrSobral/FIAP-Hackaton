using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Ai;

/// <summary>
/// Calls the Anthropic Claude API for architecture diagram analysis.
/// Images are base64-encoded and sent as vision content blocks.
/// Retries with exponential backoff on 429 TooManyRequests.
/// </summary>
public class ClaudeAnalysisService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ClaudeAnalysisService> logger) : IAiAnalysisService
{
    private const string MessagesUrl = "https://api.anthropic.com/v1/messages";
    private const int    MaxRetries  = 4;

    private static readonly string[] ImageTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];

    private static readonly string AnalysisPrompt = """
        You are an expert software architect.
        Analyse the provided architecture diagram and return ONLY a valid JSON object with exactly these three keys:
        {
          "components": "<list the identified components, services, databases, queues, and their relationships>",
          "risks": "<identify architectural risks, single points of failure, security concerns, scalability bottlenecks>",
          "recommendations": "<provide concrete improvements, best practices, and architectural recommendations>"
        }
        Be concise but thorough. Return only the JSON object, no markdown, no extra text.
        """;

    public async Task<AiAnalysisResult> AnalyzeAsync(
        string filePath,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
        var model = configuration["Anthropic:Model"] ?? "claude-sonnet-4-5";

        logger.LogInformation("Calling Claude ({Model}) for file: {FileName}", model, fileName);

        var content = ImageTypes.Contains(contentType)
            ? await BuildImageContentAsync(filePath, contentType, cancellationToken)
            : BuildTextContent(fileName);

        var requestBody = new
        {
            model,
            max_tokens = 1500,
            messages   = new[]
            {
                new { role = "user", content }
            }
        };

        var payload = JsonSerializer.Serialize(requestBody);

        return await SendWithRetryAsync(apiKey, payload, cancellationToken);
    }

    private async Task<AiAnalysisResult> SendWithRetryAsync(
        string apiKey, string payload, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            var client  = httpClientFactory.CreateClient("anthropic");
            var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await client.SendAsync(request, cancellationToken);
            var json     = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var node   = JsonNode.Parse(json)!;
                var text   = node["content"]![0]!["text"]!.GetValue<string>();
                var result = JsonSerializer.Deserialize<AiJsonResult>(text)
                    ?? throw new InvalidOperationException("Could not deserialize Claude structured response.");

                return new AiAnalysisResult(result.components, result.risks, result.recommendations);
            }

            // 429 = rate limited — wait and retry with exponential backoff
            if ((int)response.StatusCode == 429 && attempt < MaxRetries)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? delay;
                logger.LogWarning(
                    "Claude rate limit hit (attempt {Attempt}/{Max}). Retrying in {Delay}s…",
                    attempt, MaxRetries, retryAfter.TotalSeconds);

                await Task.Delay(retryAfter, cancellationToken);
                delay *= 2; // 5s → 10s → 20s → 40s
                continue;
            }

            logger.LogError("Claude API error {Status}: {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"Claude API returned {response.StatusCode}.");
        }

        throw new InvalidOperationException("Claude API rate limit exceeded after maximum retries.");
    }

    private static async Task<object[]> BuildImageContentAsync(
        string filePath, string contentType, CancellationToken ct)
    {
        var bytes  = await File.ReadAllBytesAsync(filePath, ct);
        var base64 = Convert.ToBase64String(bytes);

        return
        [
            new
            {
                type   = "image",
                source = new { type = "base64", media_type = contentType, data = base64 }
            },
            new { type = "text", text = AnalysisPrompt }
        ];
    }

    private static object[] BuildTextContent(string fileName) =>
    [
        new
        {
            type = "text",
            text = AnalysisPrompt + "\n\n" +
                   $"Architecture document filename: '{fileName}'. " +
                   "Provide a general analysis based on what a document with this name likely contains."
        }
    ];

    private record AiJsonResult(string components, string risks, string recommendations);
}
