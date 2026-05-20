using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace fiap_hackaton.Tests.Integration.Infrastructure;

public class WorkerDbContextTests : IDisposable
{
    private readonly WorkerDbContext _db;

    public WorkerDbContextTests()
    {
        var options = new DbContextOptionsBuilder<WorkerDbContext>()
            .UseInMemoryDatabase($"WorkerDb_{Guid.NewGuid()}")
            .Options;

        _db = new WorkerDbContext(options);
    }

    [Fact]
    public async Task AddReport_ShouldPersistAndRetrieveByAnalysisId()
    {
        var analysisId = Guid.NewGuid();
        var report = Report.Create(analysisId, "api, db, cache", "single point of failure on db", "add read replica");

        await _db.Reports.AddAsync(report);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var found = await _db.Reports.FirstOrDefaultAsync(r => r.AnalysisId == analysisId);

        Assert.NotNull(found);
        Assert.Equal(analysisId,        found.AnalysisId);
        Assert.Equal("api, db, cache",  found.Components);
        Assert.Equal("add read replica", found.Recommendations);
    }

    [Fact]
    public async Task FindReport_WithNonExistentAnalysisId_ShouldReturnNull()
    {
        var found = await _db.Reports.FirstOrDefaultAsync(r => r.AnalysisId == Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task WorkerDbContext_ShouldNotHaveForeignKeyToAnalyses()
    {
        // Reports can be inserted with any AnalysisId — no FK to an Analyses table.
        // This is the key property of the database-per-service pattern: worker_db
        // owns Reports independently, with no constraint crossing service boundaries.
        var orphanAnalysisId = Guid.NewGuid();
        var report = Report.Create(orphanAnalysisId, "svc-a, svc-b", "no circuit breaker between services", "implement circuit breaker pattern");

        await _db.Reports.AddAsync(report);
        var ex = await Record.ExceptionAsync(() => _db.SaveChangesAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task AddReport_WithFeedback_ShouldPersistFeedback()
    {
        var analysisId = Guid.NewGuid();
        var report = Report.Create(analysisId, "load balancer, app servers, rds", "no redundancy on app tier", "add auto-scaling group", "Overall solid, minor resilience gaps.");

        await _db.Reports.AddAsync(report);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var found = await _db.Reports.FirstOrDefaultAsync(r => r.AnalysisId == analysisId);

        Assert.NotNull(found);
        Assert.Equal("Overall solid, minor resilience gaps.", found.Feedback);
    }

    public void Dispose() => _db.Dispose();
}
