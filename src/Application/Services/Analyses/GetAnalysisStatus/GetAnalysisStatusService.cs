using fiap_hackaton.Domain.Core;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces.Repositories;

namespace fiap_hackaton.Application.Services.Analyses.GetAnalysisStatus;

public class GetAnalysisStatusService(
    ILogger<GetAnalysisStatusService> logger,
    IAnalysisRepository analysisRepository,
    IAnalysisLogRepository logRepository)
{
    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching status for analysis ID: {AnalysisId}", request.AnalysisId);

        var analysis = await analysisRepository.GetByIdAsync(request.AnalysisId, cancellationToken);

        if (analysis is null)
            return Result.Failure<Response>(new AnalysisNotFoundException(request.AnalysisId));

        var logs = logRepository.GetByAnalysisId(request.AnalysisId)
            .Select(l => new LogEntry(l.Timestamp, l.Level, l.Message))
            .ToArray();

        return Result.Success(new Response(
            analysis.Id,
            analysis.Status.ToString(),
            analysis.UpdatedAt,
            analysis.ErrorMessage,
            logs));
    }
}
