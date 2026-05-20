using fiap_hackaton.Domain.Entities.Analysis;
using Microsoft.EntityFrameworkCore;

namespace fiap_hackaton.Infrastructure.Database;

public class WorkerDbContext : DbContext
{
    public virtual DbSet<Report> Reports { get; set; }

    public WorkerDbContext() { }
    public WorkerDbContext(DbContextOptions<WorkerDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.AnalysisId).IsUnique();
            e.Property(r => r.Components).IsRequired();
            e.Property(r => r.Risks).IsRequired();
            e.Property(r => r.Recommendations).IsRequired();
            e.Property(r => r.Feedback).IsRequired();
        });
    }
}
