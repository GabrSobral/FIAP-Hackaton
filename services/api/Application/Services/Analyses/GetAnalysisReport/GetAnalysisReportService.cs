using fiap_hackaton.Domain.Core;
using fiap_hackaton.Domain.Enums;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces.Repositories;

namespace fiap_hackaton.Application.Services.Analyses.GetAnalysisReport;

public class GetAnalysisReportService(
    ILogger<GetAnalysisReportService> logger,
    IAnalysisRepository analysisRepository)
{
    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching report for analysis ID: {AnalysisId}", request.AnalysisId);

        var analysis = await analysisRepository.GetByIdWithReportAsync(request.AnalysisId, cancellationToken);

        if (analysis is null)
            return Result.Failure<Response>(new AnalysisNotFoundException(request.AnalysisId));

        if (analysis.Status != AnalysisStatus.Processed || analysis.Report is null)
            return Result.Failure<Response>(
                new DomainError($"Report is not available yet. Current status: '{analysis.Status}'."));

        return Result.Success(new Response(
            analysis.Id,
            analysis.Report.Components,
            analysis.Report.Risks,
            analysis.Report.Recommendations,
            analysis.Report.Feedback,
            analysis.Report.GeneratedAt));
    }
}
