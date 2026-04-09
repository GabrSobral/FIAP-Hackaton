using fiap_hackaton.Domain.Core;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces.Repositories;

namespace fiap_hackaton.Application.Services.Analyses.GetAnalysis;

public class GetAnalysisService(
    ILogger<GetAnalysisService> logger,
    IAnalysisRepository analysisRepository)
{
    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching analysis ID: {AnalysisId}", request.AnalysisId);

        var analysis = await analysisRepository.GetByIdAsync(request.AnalysisId, cancellationToken);

        if (analysis is null)
            return Result.Failure<Response>(new AnalysisNotFoundException(request.AnalysisId));

        return Result.Success(new Response(
            analysis.Id,
            analysis.FileName,
            analysis.ContentType,
            analysis.Status.ToString(),
            analysis.CreatedAt,
            analysis.UpdatedAt,
            analysis.ErrorMessage));
    }
}
