using fiap_hackaton.Domain.Entities.Analysis;

namespace fiap_hackaton.Domain.Interfaces.Repositories;

public interface IAnalysisRepository
{
    Task AddAsync(Analysis analysis, CancellationToken cancellationToken = default);
    Task<Analysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Analysis?> GetByIdWithReportAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Analysis analysis, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Analysis>> GetAllAsync(CancellationToken cancellationToken = default);
}
