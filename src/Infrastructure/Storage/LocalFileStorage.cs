using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Storage;

public class LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger) : IFileStorage
{
    private readonly string _basePath =
        configuration["FileStorage:BasePath"] ?? Path.Combine(Path.GetTempPath(), "fiap-hackaton-uploads");

    public async Task<string> SaveAsync(
        Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_basePath);

        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath       = Path.Combine(_basePath, uniqueFileName);

        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        logger.LogInformation("File saved: {FilePath}", filePath);
        return filePath;
    }

    public Task<Stream?> GetAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(File.OpenRead(filePath));
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
