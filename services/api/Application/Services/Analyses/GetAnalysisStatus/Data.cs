namespace fiap_hackaton.Application.Services.Analyses.GetAnalysisStatus;

public record Request(Guid AnalysisId);

public record LogEntry(DateTime Timestamp, string Level, string Message);

public record Response(
    Guid Id,
    string Status,
    DateTime UpdatedAt,
    string? ErrorMessage,
    LogEntry[] Logs
);
