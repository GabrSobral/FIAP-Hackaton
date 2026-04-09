using fiap_hackaton.Application.Services.Analyses.GetAnalysisStatus;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Enums;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiap_hackaton.Tests.Unit.Application;

public class GetAnalysisStatusServiceTests
{
    private readonly Mock<IAnalysisRepository> _repoMock = new();
    private readonly GetAnalysisStatusService  _sut;

    public GetAnalysisStatusServiceTests()
    {
        _sut = new GetAnalysisStatusService(NullLogger<GetAnalysisStatusService>.Instance, _repoMock.Object);
    }

    [Theory]
    [InlineData(AnalysisStatus.Received)]
    [InlineData(AnalysisStatus.Processing)]
    [InlineData(AnalysisStatus.Processed)]
    [InlineData(AnalysisStatus.Error)]
    public async Task ExecuteAsync_ShouldReturnCorrectStatus(AnalysisStatus expectedStatus)
    {
        var analysis = Analysis.Create("f.png", "image/png", "/p").Value;
        ApplyStatus(analysis, expectedStatus);

        _repoMock.Setup(r => r.GetByIdAsync(analysis.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _sut.ExecuteAsync(new Request(analysis.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedStatus.ToString(), result.Value.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Analysis?)null);

        var result = await _sut.ExecuteAsync(new Request(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<AnalysisNotFoundException>(result.Error);
    }

    private static void ApplyStatus(Analysis analysis, AnalysisStatus status)
    {
        switch (status)
        {
            case AnalysisStatus.Processing:
                analysis.MarkAsProcessing();
                break;
            case AnalysisStatus.Processed:
                analysis.MarkAsProcessed(Report.Create(analysis.Id, "c", "r", "r"));
                break;
            case AnalysisStatus.Error:
                analysis.MarkAsError("error");
                break;
        }
    }
}
