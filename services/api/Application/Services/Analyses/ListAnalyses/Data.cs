namespace fiap_hackaton.Application.Services.Analyses.ListAnalyses;

public record Response(IReadOnlyList<AnalysisItem> Items);

public record AnalysisItem(
    Guid Id,
    string FileName,
    string ContentType,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ErrorMessage
);
