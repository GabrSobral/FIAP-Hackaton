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
        You are an expert software architect. All content you write MUST be in Brazilian Portuguese (pt-BR).
        Analyse the provided architecture diagram and return ONLY a valid JSON object with exactly these four keys:

        {
          "components": [
            "**<Component name>** — <role or relationship>",
            "**<Component name>** — <role or relationship>"
          ],
          "risks": [
            "**<Risk title>** — <explanation of the risk and its impact>",
            "**<Risk title>** — <explanation of the risk and its impact>"
          ],
          "recommendations": [
            "**<Action title>** — <concrete step to address a risk or improvement>",
            "**<Action title>** — <concrete step to address a risk or improvement>"
          ],
          "feedback": [
            "<First paragraph: overall maturity level and general impression.>",
            "<Second paragraph: main strengths.>",
            "<Third paragraph: top concern and what to prioritise.>"
          ]
        }

        Rules:
        - Every value must be a JSON array of plain strings — no nested objects, no markdown headings inside items.
        - Each string is one self-contained item. Do not combine multiple items into one string.
        - Use **bold** only for the leading title in each item, followed by " — " and the explanation.
        - Do not number the items — numbering will be added automatically.
        - Return ONLY the JSON object with no markdown fences and no extra text.
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
            max_tokens = 4096,
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
                var node    = JsonNode.Parse(json)!;
                var rawText = node["content"]![0]!["text"]!.GetValue<string>();
                var safeJson = RepairTruncatedJson(rawText);
                var result  = JsonSerializer.Deserialize<AiJsonResult>(safeJson, JsonOptions)
                    ?? throw new InvalidOperationException("Could not deserialize Claude structured response.");

                return new AiAnalysisResult(
                    BuildBulletList(result.Components),
                    BuildNumberedList(result.Risks),
                    BuildNumberedList(result.Recommendations),
                    BuildParagraphs(result.Feedback));
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

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Closes any unclosed strings, arrays, and objects left open when Claude's
    /// response is cut off by the token limit, producing parseable JSON from
    /// whatever portion was completed.
    /// </summary>
    private static string RepairTruncatedJson(string raw)
    {
        var trimmed = raw.Trim();

        // Fast path: already valid
        try { JsonDocument.Parse(trimmed); return trimmed; }
        catch (JsonException) { /* fall through to repair */ }

        var sb       = new System.Text.StringBuilder(trimmed);
        var inString = false;
        var escaped  = false;
        var depth    = 0; // nesting depth of [ and {

        foreach (var c in trimmed)
        {
            if (escaped)  { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }

            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c is '{' or '[') depth++;
            else if (c is '}' or ']') depth--;
        }

        // If we're mid-string, close it
        if (inString) sb.Append('"');

        // Remove any trailing comma before we close containers
        var s = sb.ToString().TrimEnd();
        while (s.EndsWith(',')) s = s[..^1].TrimEnd();
        sb.Clear();
        sb.Append(s);

        // Close open arrays / objects (depth tells us how many are open)
        // Walk the original to figure out what type each level is
        var stack    = new Stack<char>();
        inString = false;
        escaped  = false;
        foreach (var c in trimmed)
        {
            if (escaped)  { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == '{') stack.Push('}');
            else if (c == '[') stack.Push(']');
            else if (c is '}' or ']') { if (stack.Count > 0) stack.Pop(); }
        }

        while (stack.Count > 0)
            sb.Append(stack.Pop());

        return sb.ToString();
    }

    private static string BuildBulletList(List<string>? items) =>
        items is { Count: > 0 }
            ? string.Join("\n", items.Select(i => $"- {i.Trim()}"))
            : string.Empty;

    private static string BuildNumberedList(List<string>? items) =>
        items is { Count: > 0 }
            ? string.Join("\n", items.Select((i, idx) => $"{idx + 1}. {i.Trim()}"))
            : string.Empty;

    private static string BuildParagraphs(List<string>? items) =>
        items is { Count: > 0 }
            ? string.Join("\n\n", items.Select(i => i.Trim()))
            : string.Empty;

    private record AiJsonResult(
        List<string>? Components,
        List<string>? Risks,
        List<string>? Recommendations,
        List<string>? Feedback);
}
