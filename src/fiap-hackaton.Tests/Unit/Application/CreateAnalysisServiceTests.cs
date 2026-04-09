using fiap_hackaton.Application.Services.Analyses.CreateAnalysis;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces;
using fiap_hackaton.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace fiap_hackaton.Tests.Unit.Application;

public class CreateAnalysisServiceTests
{
    private readonly Mock<IAnalysisRepository> _repoMock       = new();
    private readonly Mock<IFileStorage>        _storageMock    = new();
    private readonly Mock<IEventPublisher>     _publisherMock  = new();
    private readonly Mock<IUnitOfWork>         _unitOfWorkMock = new();
    private readonly CreateAnalysisService     _sut;

    public CreateAnalysisServiceTests()
    {
        _sut = new CreateAnalysisService(
            NullLogger<CreateAnalysisService>.Instance,
            _repoMock.Object,
            _storageMock.Object,
            _publisherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidImageFile_ShouldReturnSuccess()
    {
        var file = MakeFile("diagram.png", "image/png", 1024);
        _storageMock.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/diagram.png");

        var result = await _sut.ExecuteAsync(new Request(file), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Received", result.Value.Status);
        Assert.NotEqual(Guid.Empty, result.Value.AnalysisId);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<fiap_hackaton.Domain.Entities.Analysis.Analysis>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<fiap_hackaton.Domain.Events.DiagramUploadedEvent>(), "diagram-analysis", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidPdf_ShouldReturnSuccess()
    {
        var file = MakeFile("arch.pdf", "application/pdf", 2048);
        _storageMock.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/arch.pdf");

        var result = await _sut.ExecuteAsync(new Request(file), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyFile_ShouldReturnFailure()
    {
        var file = MakeFile("empty.png", "image/png", 0);

        var result = await _sut.ExecuteAsync(new Request(file), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<DomainError>(result.Error);
        Assert.Contains("empty", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithFileTooLarge_ShouldReturnFailure()
    {
        var file = MakeFile("big.png", "image/png", 11 * 1024 * 1024); // 11 MB

        var result = await _sut.ExecuteAsync(new Request(file), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<DomainError>(result.Error);
        Assert.Contains("maximum", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsupportedContentType_ShouldReturnFailure()
    {
        var file = MakeFile("script.exe", "application/octet-stream", 512);

        var result = await _sut.ExecuteAsync(new Request(file), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<DomainError>(result.Error);
        Assert.Contains("not supported", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotPublishEvent_WhenFileStorageFails()
    {
        var file = MakeFile("diagram.png", "image/png", 1024);
        _storageMock.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk full"));

        await Assert.ThrowsAsync<IOException>(() =>
            _sut.ExecuteAsync(new Request(file), CancellationToken.None));

        _publisherMock.Verify(p =>
            p.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Helpers
    private static IFormFile MakeFile(string name, string contentType, long size)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(name);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(size);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[Math.Max(0, size)]));
        return mock.Object;
    }
}
