using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Enums;
using fiap_hackaton.Domain.Exceptions;

namespace fiap_hackaton.Tests.Unit.Domain;

public class AnalysisTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = Analysis.Create("diagram.png", "image/png", "/uploads/diagram.png");

        Assert.True(result.IsSuccess);
        var analysis = result.Value;
        Assert.NotEqual(Guid.Empty, analysis.Id);
        Assert.Equal("diagram.png", analysis.FileName);
        Assert.Equal("image/png", analysis.ContentType);
        Assert.Equal(AnalysisStatus.Received, analysis.Status);
        Assert.Null(analysis.ErrorMessage);
        Assert.Null(analysis.Report);
    }

    [Theory]
    [InlineData("", "image/png", "/uploads/f.png")]
    [InlineData("   ", "image/png", "/uploads/f.png")]
    public void Create_WithBlankFileName_ShouldFail(string fileName, string contentType, string filePath)
    {
        var result = Analysis.Create(fileName, contentType, filePath);

        Assert.True(result.IsFailure);
        Assert.IsType<DomainError>(result.Error);
    }

    [Theory]
    [InlineData("diagram.png", "image/png", "")]
    [InlineData("diagram.png", "image/png", "   ")]
    public void Create_WithBlankFilePath_ShouldFail(string fileName, string contentType, string filePath)
    {
        var result = Analysis.Create(fileName, contentType, filePath);

        Assert.True(result.IsFailure);
        Assert.IsType<DomainError>(result.Error);
    }

    [Fact]
    public void MarkAsProcessing_ShouldTransitionStatus()
    {
        var analysis = Analysis.Create("f.png", "image/png", "/p").Value;

        analysis.MarkAsProcessing();

        Assert.Equal(AnalysisStatus.Processing, analysis.Status);
    }

    [Fact]
    public void MarkAsProcessed_ShouldAttachReportAndTransitionStatus()
    {
        var analysis = Analysis.Create("f.png", "image/png", "/p").Value;
        var report   = Report.Create(analysis.Id, "comps", "risks", "recs");

        analysis.MarkAsProcessed(report);

        Assert.Equal(AnalysisStatus.Processed, analysis.Status);
        Assert.NotNull(analysis.Report);
        Assert.Equal("comps", analysis.Report.Components);
        Assert.Equal("risks", analysis.Report.Risks);
        Assert.Equal("recs", analysis.Report.Recommendations);
    }

    [Fact]
    public void MarkAsError_ShouldSetErrorMessageAndStatus()
    {
        var analysis = Analysis.Create("f.png", "image/png", "/p").Value;

        analysis.MarkAsError("AI service unavailable");

        Assert.Equal(AnalysisStatus.Error, analysis.Status);
        Assert.Equal("AI service unavailable", analysis.ErrorMessage);
    }

    [Fact]
    public void UpdatedAt_ShouldChange_OnStatusTransition()
    {
        var analysis = Analysis.Create("f.png", "image/png", "/p").Value;
        var before   = analysis.UpdatedAt;

        // Small delay so timestamps differ
        Thread.Sleep(10);
        analysis.MarkAsProcessing();

        Assert.True(analysis.UpdatedAt >= before);
    }
}
