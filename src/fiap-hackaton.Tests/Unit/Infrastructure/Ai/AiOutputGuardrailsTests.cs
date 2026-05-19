using fiap_hackaton.Domain.Interfaces;
using fiap_hackaton.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiap_hackaton.Tests.Unit.Infrastructure.Ai;

public class AiOutputGuardrailsTests
{
    private static readonly AiAnalysisResult ValidResult = new(
        Components:      "API Gateway → Application Service → PostgreSQL database with connections",
        Risks:           "Single point of failure on the gateway layer causing total service outage",
        Recommendations: "Add a load balancer and implement circuit breaker pattern for resilience",
        Feedback:        "Solid architecture overall with minor resilience gaps to address.");

    [Fact]
    public void ValidateAndSanitize_WithValidInput_ShouldReturnResult()
    {
        var result = AiOutputGuardrails.ValidateAndSanitize(ValidResult, NullLogger.Instance, "Test");

        Assert.Equal(ValidResult.Components,      result.Components);
        Assert.Equal(ValidResult.Risks,           result.Risks);
        Assert.Equal(ValidResult.Recommendations, result.Recommendations);
    }

    [Fact]
    public void ValidateAndSanitize_WithEmptyComponents_ShouldThrow()
    {
        var input = ValidResult with { Components = "" };

        Assert.Throws<AiGuardrailException>(() =>
            AiOutputGuardrails.ValidateAndSanitize(input, NullLogger.Instance, "Test"));
    }

    [Fact]
    public void ValidateAndSanitize_WithTooShortRisks_ShouldThrow()
    {
        var input = ValidResult with { Risks = "too short" }; // < 20 chars

        Assert.Throws<AiGuardrailException>(() =>
            AiOutputGuardrails.ValidateAndSanitize(input, NullLogger.Instance, "Test"));
    }

    [Fact]
    public void ValidateAndSanitize_WithTooShortRecommendations_ShouldThrow()
    {
        var input = ValidResult with { Recommendations = "add lb" }; // < 20 chars

        Assert.Throws<AiGuardrailException>(() =>
            AiOutputGuardrails.ValidateAndSanitize(input, NullLogger.Instance, "Test"));
    }

    [Theory]
    [InlineData("n/a")]
    [InlineData("none")]
    [InlineData("nenhum")]
    [InlineData("não há riscos")]
    [InlineData("no risks identified")]
    [InlineData("not available")]
    public void ValidateAndSanitize_WithPlaceholderComponents_ShouldThrow(string placeholder)
    {
        var input = ValidResult with { Components = placeholder };

        Assert.Throws<AiGuardrailException>(() =>
            AiOutputGuardrails.ValidateAndSanitize(input, NullLogger.Instance, "Test"));
    }

    [Fact]
    public void ValidateAndSanitize_ShouldStripControlCharacters()
    {
        // Input has \x00 between "API" and "Gateway", and \x07 between "Gateway" and " →".
        // After stripping, both segments must be directly adjacent.
        var input = ValidResult with
        {
            Components = "API\x00Gateway\x07 → Service → Database with all connections kept"
        };

        var result = AiOutputGuardrails.ValidateAndSanitize(input, NullLogger.Instance, "Test");

        // "APIGateway" is only a contiguous substring when \x00 is removed.
        Assert.Contains("APIGateway", result.Components);
        // "Gateway →" is only a contiguous substring when \x07 is removed.
        Assert.Contains("Gateway →", result.Components);
    }

    [Fact]
    public void ValidateAndSanitize_ShouldCollapseExcessiveNewlines()
    {
        var input = ValidResult with
        {
            Risks = "Single point of failure\n\n\n\n\non the gateway layer causing outages"
        };

        var result = AiOutputGuardrails.ValidateAndSanitize(input, NullLogger.Instance, "Test");

        Assert.DoesNotContain("\n\n\n", result.Risks);
    }

    [Fact]
    public void ValidateAndSanitize_ShouldNotRequireFeedback()
    {
        var input = ValidResult with { Feedback = "" };

        var result = AiOutputGuardrails.ValidateAndSanitize(input, NullLogger.Instance, "Test");

        Assert.Equal("", result.Feedback);
    }
}
