using fiap_hackaton.Domain.Core;
using fiap_hackaton.Domain.Interfaces.Repositories;

namespace fiap_hackaton.Application.Services.Analyses.ListAnalyses;

public class ListAnalysesService(
    ILogger<ListAnalysesService> logger,
    IAnalysisRepository analysisRepository)
{
    public async Task<Result<Response>> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Listing all analyses");

        var analyses = await analysisRepository.GetAllAsync(cancellationToken);

        var items = analyses
            .Select(a => new AnalysisItem(
                a.Id,
                a.FileName,
                a.ContentType,
                a.Status.ToString(),
                a.CreatedAt,
                a.UpdatedAt,
                a.ErrorMessage))
            .ToList();

        return Result.Success<Response>(new Response(items));
    }
}
