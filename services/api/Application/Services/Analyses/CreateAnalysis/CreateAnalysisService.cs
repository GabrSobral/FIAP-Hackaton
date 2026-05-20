using fiap_hackaton.Domain.Core;
using fiap_hackaton.Domain.Events;
using fiap_hackaton.Domain.Exceptions;
using fiap_hackaton.Domain.Interfaces;
using fiap_hackaton.Domain.Interfaces.Repositories;

namespace fiap_hackaton.Application.Services.Analyses.CreateAnalysis;

public class CreateAnalysisService(
    ILogger<CreateAnalysisService> logger,
    IAnalysisRepository analysisRepository,
    IFileStorage fileStorage,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork)
{
    private static readonly long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf"];

    public async Task<Result<Response>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting analysis creation for file: {FileName}", request.File.FileName);

        if (request.File.Length == 0)
            return Result.Failure<Response>(new DomainError("File is empty."));

        if (request.File.Length > MaxFileSizeBytes)
            return Result.Failure<Response>(
                new DomainError($"File exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB."));

        if (!AllowedContentTypes.Contains(request.File.ContentType))
            return Result.Failure<Response>(
                new DomainError($"File type '{request.File.ContentType}' is not supported. " +
                                $"Allowed types: {string.Join(", ", AllowedContentTypes)}."));

        await using var stream = request.File.OpenReadStream();
        var filePath = await fileStorage.SaveAsync(
            stream, request.File.FileName, request.File.ContentType, cancellationToken);

        var analysisResult = Domain.Entities.Analysis.Analysis.Create(
            request.File.FileName, request.File.ContentType, filePath);

        if (analysisResult.IsFailure)
        {
            logger.LogError(analysisResult.Error, "Failed to create analysis entity.");
            return Result.Failure<Response>(analysisResult.Error!);
        }

        var analysis = analysisResult.Value;

        await analysisRepository.AddAsync(analysis, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(
            new DiagramUploadedEvent(
                analysis.Id,
                analysis.FilePath,
                analysis.FileName,
                analysis.ContentType,
                DateTime.UtcNow),
            "diagram-analysis",
            cancellationToken);

        logger.LogInformation("Analysis {AnalysisId} created. Event published to 'diagram-analysis' queue.", analysis.Id);

        return Result.Success(new Response(analysis.Id, analysis.Status.ToString(), analysis.CreatedAt));
    }
}
