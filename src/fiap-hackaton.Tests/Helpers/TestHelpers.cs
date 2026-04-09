using Microsoft.AspNetCore.Http;
using Moq;

namespace fiap_hackaton.Tests.Helpers;

/// <summary>Helper methods used across test classes.</summary>
public static class TestHelpers
{
    /// <summary>Creates a multipart/form-data request body containing a fake file.</summary>
    public static MultipartFormDataContent CreateFileContent(
        string fileName    = "diagram.png",
        string contentType = "image/png",
        int    sizeBytes   = 1024)
    {
        var content = new MultipartFormDataContent();
        var bytes   = new byte[sizeBytes];
        new Random().NextBytes(bytes);

        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    /// <summary>Creates a mock IFormFile.</summary>
    public static IFormFile MockFormFile(
        string name        = "diagram.png",
        string contentType = "image/png",
        long   size        = 1024)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(name);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(size);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[Math.Max(0, size)]));
        return mock.Object;
    }
}
