namespace fiap_hackaton.Application.Services.Analyses.GetAnalysis;

public record Request(Guid AnalysisId);

public record Response(
    Guid Id,
    string FileName,
    string ContentType,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ErrorMessage
);
