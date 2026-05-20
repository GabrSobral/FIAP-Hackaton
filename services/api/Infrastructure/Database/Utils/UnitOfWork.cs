using fiap_hackaton.Domain.Interfaces;
using fiap_hackaton.Infrastructure.Database;

namespace fiap_hackaton.Infrastructure.Database.Utils;

public class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}
