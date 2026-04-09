using fiap_hackaton.Domain.Entities.Analysis;

namespace fiap_hackaton.Tests.Unit.Domain;

public class ReportTests
{
    [Fact]
    public void Create_ShouldSetAllPropertiesCorrectly()
    {
        var analysisId = Guid.NewGuid();

        var report = Report.Create(analysisId, "components text", "risks text", "recommendations text");

        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal(analysisId, report.AnalysisId);
        Assert.Equal("components text", report.Components);
        Assert.Equal("risks text", report.Risks);
        Assert.Equal("recommendations text", report.Recommendations);
        Assert.True(report.GeneratedAt <= DateTime.UtcNow);
    }
}
