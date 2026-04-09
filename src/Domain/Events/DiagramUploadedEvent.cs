namespace fiap_hackaton.Domain.Events;

/// <summary>
/// Published to the 'diagram-analysis' queue after a diagram is successfully uploaded.
/// The AI Processing Service consumes this event to begin analysis.
/// </summary>
public record DiagramUploadedEvent(
    Guid AnalysisId,
    string FilePath,
    string FileName,
    string ContentType,
    DateTime UploadedAt
);
