using fiap_hackaton.Domain.Core;

namespace fiap_hackaton.Domain.Entities.Analysis;

public class AnalysisLog : Entity
{
    public Guid AnalysisId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Level { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    private AnalysisLog() { } // EF Core

    public AnalysisLog(Guid analysisId, string level, string message)
    {
        Id = Guid.NewGuid();
        AnalysisId = analysisId;
        Timestamp = DateTime.UtcNow;
        Level = level;
        Message = message;
    }
}
