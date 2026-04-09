using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace fiap_hackaton.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public virtual DbSet<Analysis> Analyses { get; set; }
    public virtual DbSet<Report> Reports { get; set; }
    public virtual DbSet<AnalysisLog> AnalysisLogs { get; set; }

    public AppDbContext() { }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Analysis>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.FileName).HasMaxLength(512).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(128).IsRequired();
            e.Property(a => a.FilePath).HasMaxLength(1024).IsRequired();
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(a => a.ErrorMessage).HasMaxLength(2048);

            e.HasOne(a => a.Report)
             .WithOne()
             .HasForeignKey<Report>(r => r.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Components).IsRequired();
            e.Property(r => r.Risks).IsRequired();
            e.Property(r => r.Recommendations).IsRequired();
        });

        modelBuilder.Entity<AnalysisLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Level).HasMaxLength(16).IsRequired();
            e.Property(l => l.Message).HasMaxLength(4000).IsRequired();
            e.HasIndex(l => l.AnalysisId);
        });
    }
}
