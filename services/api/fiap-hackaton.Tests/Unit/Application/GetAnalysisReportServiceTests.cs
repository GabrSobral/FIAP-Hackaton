using fiap_hackaton.Application.Services.Analyses.GetAnalysisReport;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiap_hackaton.Tests.Unit.Application;

public class GetAnalysisReportServiceTests
{
    private readonly Mock<IAnalysisRepository> _repoMock = new();
    private readonly GetAnalysisReportService  _sut;

    public GetAnalysisReportServiceTests()
    {
        _sut = new GetAnalysisReportService(NullLogger<GetAnalysisReportService>.Instance, _repoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithProcessedAnalysis_ShouldReturnReport()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p").Value;
        var report   = Report.Create(analysis.Id, "svc-a, db-b", "spof in db-b", "add replica");
        analysis.MarkAsProcessed(report);

        _repoMock.Setup(r => r.GetByIdWithReportAsync(analysis.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _sut.ExecuteAsync(new Request(analysis.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("svc-a, db-b", result.Value.Components);
        Assert.Equal("spof in db-b", result.Value.Risks);
        Assert.Equal("add replica", result.Value.Recommendations);
    }

    [Fact]
    public async Task ExecuteAsync_WithNotProcessedAnalysis_ShouldReturnFailure()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p").Value;
        // Status is still Received — no report yet
        _repoMock.Setup(r => r.GetByIdWithReportAsync(analysis.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _sut.ExecuteAsync(new Request(analysis.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<DomainError>(result.Error);
        Assert.Contains("not available", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdWithReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Analysis?)null);

        var result = await _sut.ExecuteAsync(new Request(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<AnalysisNotFoundException>(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithErrorAnalysis_ShouldReturnFailure()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p").Value;
        analysis.MarkAsError("AI timed out");

        _repoMock.Setup(r => r.GetByIdWithReportAsync(analysis.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _sut.ExecuteAsync(new Request(analysis.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<DomainError>(result.Error);
    }
}
