using System.Text.RegularExpressions;
using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Ai;

/// <summary>
/// Post-response guardrails applied to every AI provider before the result is returned.
///
/// Responsibilities:
///   1. Sanitise — strip control characters and normalise excessive whitespace.
///   2. Validate — reject empty, too-short, or placeholder-only fields.
///
/// Throws <see cref="AiGuardrailException"/> so callers with retry loops can treat
/// a bad response as retryable without conflating it with HTTP/network errors.
/// </summary>
public static partial class AiOutputGuardrails
{
    private const int MinFieldLength = 20;

    private static readonly HashSet<string> PlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "n/a", "não disponível", "not available", "not applicable",
        "no information", "sem informação", "nenhum", "none",
        "não identificado", "não há riscos", "no risks identified",
    };

    public static AiAnalysisResult ValidateAndSanitize(
        AiAnalysisResult result,
        ILogger logger,
        string providerName)
    {
        var sanitized = new AiAnalysisResult(
            SanitizeField(result.Components),
            SanitizeField(result.Risks),
            SanitizeField(result.Recommendations),
            SanitizeField(result.Feedback));

        ValidateRequiredField(sanitized.Components,      "components",      providerName, logger);
        ValidateRequiredField(sanitized.Risks,           "risks",           providerName, logger);
        ValidateRequiredField(sanitized.Recommendations, "recommendations", providerName, logger);

        return sanitized;
    }

    private static void ValidateRequiredField(
        string value, string fieldName, string provider, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < MinFieldLength)
        {
            logger.LogWarning(
                "AI guardrail [{Provider}]: field '{Field}' is empty or too short ({Len} chars).",
                provider, fieldName, value?.Length ?? 0);

            throw new AiGuardrailException(
                $"Field '{fieldName}' from {provider} is empty or too short ({value?.Length ?? 0} chars).");
        }

        var firstLine = value.Trim().Split('\n')[0].Trim();
        if (PlaceholderValues.Contains(value.Trim()) || PlaceholderValues.Contains(firstLine))
        {
            logger.LogWarning(
                "AI guardrail [{Provider}]: field '{Field}' contains a placeholder value.",
                provider, fieldName);

            throw new AiGuardrailException(
                $"Field '{fieldName}' from {provider} contains a placeholder value.");
        }
    }

    private static string SanitizeField(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Strip control characters — keep \n (newline) and \t (tab)
        var clean = ControlCharRegex().Replace(value, "");
        // Collapse three or more consecutive blank lines to two
        clean = ExcessiveNewlinesRegex().Replace(clean.Trim(), "\n\n");
        return clean;
    }

    // Matches C0/C1 control characters except \t (0x09) and \n (0x0A)
    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewlinesRegex();
}

/// <summary>
/// Thrown when an AI response passes structural parsing but fails semantic validation.
/// Services with retry loops should treat this as a retryable soft failure.
/// </summary>
public sealed class AiGuardrailException(string message) : InvalidOperationException(message);
