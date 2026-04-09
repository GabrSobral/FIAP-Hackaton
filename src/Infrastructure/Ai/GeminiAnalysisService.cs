using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Ai;

/// <summary>
/// Calls the Google Gemini API for architecture diagram analysis.
/// Images are base64-encoded and sent inline via the inlineData part.
/// Retries with exponential backoff on 429 TooManyRequests.
/// </summary>
public class GeminiAnalysisService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GeminiAnalysisService> logger) : IAiAnalysisService
{
    private const string BaseUrl   = "https://generativelanguage.googleapis.com/v1beta/models";
    private const int    MaxRetries = 5;

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
        var apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        var model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";

        logger.LogInformation("Calling Gemini ({Model}) for file: {FileName}", model, fileName);

        var parts = ImageTypes.Contains(contentType)
            ? await BuildImagePartsAsync(filePath, contentType, cancellationToken)
            : BuildTextParts(fileName);

        var requestBody = new
        {
            contents         = new[] { new { parts } },
            generationConfig = new { responseMimeType = "application/json" }
        };

        var url     = $"{BaseUrl}/{model}:generateContent?key={apiKey}";
        var payload = JsonSerializer.Serialize(requestBody);

        return await SendWithRetryAsync(url, payload, cancellationToken);
    }

    private async Task<AiAnalysisResult> SendWithRetryAsync(
        string url, string payload, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(15);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            var client  = httpClientFactory.CreateClient("gemini");
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request, cancellationToken);
            var json     = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var node   = JsonNode.Parse(json)!;
                var text   = node["candidates"]![0]!["content"]!["parts"]![0]!["text"]!.GetValue<string>();
                var clean  = StripMarkdownFences(text);
                var parsed = JsonNode.Parse(clean)
                    ?? throw new InvalidOperationException("Gemini returned an empty JSON body.");

                return new AiAnalysisResult(
                    NodeToString(parsed, "components"),
                    NodeToString(parsed, "risks"),
                    NodeToString(parsed, "recommendations"));
            }

            // 429 = rate limited — wait and retry with exponential backoff
            if ((int)response.StatusCode == 429 && attempt < MaxRetries)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? delay;
                logger.LogWarning(
                    "Gemini rate limit hit (attempt {Attempt}/{Max}). Retrying in {Delay}s…",
                    attempt, MaxRetries, retryAfter.TotalSeconds);

                await Task.Delay(retryAfter, cancellationToken);
                delay *= 2; // 5s → 10s → 20s → 40s
                continue;
            }

            logger.LogError("Gemini API error {Status}: {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"Gemini API returned {response.StatusCode}.");
        }

        throw new InvalidOperationException("Gemini API rate limit exceeded after maximum retries.");
    }

    private static async Task<object[]> BuildImagePartsAsync(
        string filePath, string contentType, CancellationToken ct)
    {
        var bytes  = await File.ReadAllBytesAsync(filePath, ct);
        var base64 = Convert.ToBase64String(bytes);

        return
        [
            new { text = AnalysisPrompt },
            new { inlineData = new { mimeType = contentType, data = base64 } }
        ];
    }

    private static object[] BuildTextParts(string fileName) =>
    [
        new
        {
            text = AnalysisPrompt + "\n\n" +
                   $"Architecture document filename: '{fileName}'. " +
                   "Provide a general analysis based on what a document with this name likely contains."
        }
    ];

    /// Extracts a field as string regardless of whether the model returned a
    /// plain string, an object, or an array.
    private static string NodeToString(JsonNode parent, string key)
    {
        var child = parent[key] ?? parent[char.ToUpper(key[0]) + key[1..]];
        if (child is null) return string.Empty;
        return child is JsonValue val && val.TryGetValue<string>(out var s) ? s : child.ToJsonString();
    }

    private static string StripMarkdownFences(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```")) return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0) return trimmed;

        var withoutOpen = trimmed[(firstNewline + 1)..];
        var lastFence   = withoutOpen.LastIndexOf("```");
        return lastFence >= 0 ? withoutOpen[..lastFence].Trim() : withoutOpen.Trim();
    }

}
