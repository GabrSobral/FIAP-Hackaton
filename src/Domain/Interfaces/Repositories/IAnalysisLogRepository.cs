using fiap_hackaton.Domain.Entities.Analysis;

namespace fiap_hackaton.Domain.Interfaces.Repositories;

public interface IAnalysisLogRepository
{
    Task AddAsync(AnalysisLog log, CancellationToken ct = default);
    IReadOnlyList<AnalysisLog> GetByAnalysisId(Guid analysisId);
}
