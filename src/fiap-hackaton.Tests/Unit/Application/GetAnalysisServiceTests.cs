using fiap_hackaton.Application.Services.Analyses.GetAnalysis;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiap_hackaton.Tests.Unit.Application;

public class GetAnalysisServiceTests
{
    private readonly Mock<IAnalysisRepository> _repoMock = new();
    private readonly GetAnalysisService        _sut;

    public GetAnalysisServiceTests()
    {
        _sut = new GetAnalysisService(NullLogger<GetAnalysisService>.Instance, _repoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingId_ShouldReturnAnalysis()
    {
        var analysis = Analysis.Create("arch.png", "image/png", "/p/arch.png").Value;
        _repoMock.Setup(r => r.GetByIdAsync(analysis.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var result = await _sut.ExecuteAsync(new Request(analysis.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(analysis.Id, result.Value.Id);
        Assert.Equal("arch.png", result.Value.FileName);
        Assert.Equal("Received", result.Value.Status);
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
}
