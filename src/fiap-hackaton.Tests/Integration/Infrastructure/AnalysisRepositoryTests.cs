using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Infrastructure.Database;
using fiap_hackaton.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace fiap_hackaton.Tests.Integration.Infrastructure;

public class AnalysisRepositoryTests : IDisposable
{
    private readonly AppDbContext      _dbContext;
    private readonly AnalysisRepository _sut;

    public AnalysisRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RepoTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);
        _sut       = new AnalysisRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAnalysis()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p").Value;

        await _sut.AddAsync(analysis);
        await _dbContext.SaveChangesAsync();

        var persisted = await _dbContext.Analyses.FindAsync(analysis.Id);
        Assert.NotNull(persisted);
        Assert.Equal("arch.png", persisted.FileName);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnAnalysis()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p").Value;
        await _dbContext.Analyses.AddAsync(analysis);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(analysis.Id);

        Assert.NotNull(result);
        Assert.Equal(analysis.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithReportAsync_ShouldIncludeReport()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p").Value;
        var report   = Report.Create(analysis.Id, "comps", "risks", "recs");
        analysis.MarkAsProcessed(report);

        await _dbContext.Analyses.AddAsync(analysis);
        await _dbContext.SaveChangesAsync();

        // Detach to force a real DB round-trip
        _dbContext.ChangeTracker.Clear();

        var result = await _sut.GetByIdWithReportAsync(analysis.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.Report);
        Assert.Equal("comps", result.Report.Components);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistStatusChange()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p").Value;
        await _dbContext.Analyses.AddAsync(analysis);
        await _dbContext.SaveChangesAsync();

        analysis.MarkAsProcessing();
        await _sut.UpdateAsync(analysis);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var updated = await _dbContext.Analyses.FindAsync(analysis.Id);
        Assert.Equal("Processing", updated!.Status.ToString());
    }

    public void Dispose() => _dbContext.Dispose();
}
