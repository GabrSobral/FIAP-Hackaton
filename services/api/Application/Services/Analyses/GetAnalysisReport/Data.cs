namespace fiap_hackaton.Application.Services.Analyses.GetAnalysisReport;

public record Request(Guid AnalysisId);

public record Response(
    Guid AnalysisId,
    string Components,
    string Risks,
    string Recommendations,
    string Feedback,
    DateTime GeneratedAt
);
