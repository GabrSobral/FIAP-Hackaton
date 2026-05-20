using fiap_hackaton.Domain.Core;
using fiap_hackaton.Domain.Enums;
using fiap_hackaton.Domain.Exceptions;

namespace fiap_hackaton.Domain.Entities.Analysis;

public class Analysis : Entity
{
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public AnalysisStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Report? Report { get; private set; }

    private Analysis() { } // EF Core

    private Analysis(string fileName, string contentType, string filePath)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        ContentType = contentType;
        FilePath = filePath;
        Status = AnalysisStatus.Received;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Result<Analysis> Create(string fileName, string contentType, string filePath)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<Analysis>(new DomainError("File name is required."));

        if (string.IsNullOrWhiteSpace(filePath))
            return Result.Failure<Analysis>(new DomainError("File path is required."));

        return Result.Success(new Analysis(fileName, contentType, filePath));
    }

    public void MarkAsProcessing()
    {
        Status = AnalysisStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessed(Report report)
    {
        Status = AnalysisStatus.Processed;
        Report = report;
        UpdatedAt = DateTime.UtcNow;
    }

    // Status-only overload; Report is inserted separately by infrastructure to avoid
    // EF Core batching UPDATE Analyses + INSERT Reports in a single Npgsql round-trip.
    public void MarkAsProcessed()
    {
        Status = AnalysisStatus.Processed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsError(string errorMessage)
    {
        Status = AnalysisStatus.Error;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}
