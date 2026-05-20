using fiap_hackaton.Domain.Core;

namespace fiap_hackaton.Domain.Entities.Analysis;

public class Report : Entity
{
    public Guid AnalysisId { get; private set; }
    public string Components { get; private set; } = string.Empty;
    public string Risks { get; private set; } = string.Empty;
    public string Recommendations { get; private set; } = string.Empty;
    public string Feedback { get; private set; } = string.Empty;
    public DateTime GeneratedAt { get; private set; }

    private Report() { } // EF Core

    private Report(Guid analysisId, string components, string risks, string recommendations, string feedback)
    {
        Id = Guid.NewGuid();
        AnalysisId = analysisId;
        Components = components;
        Risks = risks;
        Recommendations = recommendations;
        Feedback = feedback;
        GeneratedAt = DateTime.UtcNow;
    }

    public static Report Create(Guid analysisId, string components, string risks, string recommendations, string feedback = "")
        => new(analysisId, components, risks, recommendations, feedback);
}
