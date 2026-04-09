using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace fiap_hackaton.Infrastructure.Database.Repositories;

public class AnalysisRepository(AppDbContext dbContext) : IAnalysisRepository
{
    public async Task AddAsync(Analysis analysis, CancellationToken cancellationToken = default)
        => await dbContext.Analyses.AddAsync(analysis, cancellationToken);

    public async Task<Analysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Analyses
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<Analysis?> GetByIdWithReportAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Analyses
            .Include(a => a.Report)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Analysis>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Analyses
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(Analysis analysis, CancellationToken cancellationToken = default)
    {
        dbContext.Entry(analysis).State = EntityState.Modified;

        if (analysis.Report is not null &&
            dbContext.Entry(analysis.Report).State == EntityState.Detached)
        {
            dbContext.Entry(analysis.Report).State = EntityState.Added;
        }

        return Task.CompletedTask;
    }
}
