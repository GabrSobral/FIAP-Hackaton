namespace fiap_hackaton.Application.Services.Analyses.CreateAnalysis;

public record Request(IFormFile File);

public record Response(
    Guid AnalysisId,
    string Status,
    DateTime CreatedAt
);
