using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace fiap_hackaton.Infrastructure.Database.Repositories;

public class AnalysisLogRepository(AppDbContext dbContext) : IAnalysisLogRepository
{
    public async Task AddAsync(AnalysisLog log, CancellationToken ct = default)
        => await dbContext.AddAsync(log, ct);

    public IReadOnlyList<AnalysisLog> GetByAnalysisId(Guid analysisId)
    {
        return dbContext.Set<AnalysisLog>()
            .AsNoTracking()
            .Where(l => l.AnalysisId == analysisId)
            .OrderBy(l => l.Timestamp)
            .ToList();
    }
}
