using System.Net;
using System.Text.Json;
using fiap_hackaton.Domain.Exceptions;

namespace fiap_hackaton.Presentation.Middlewares;

public sealed class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception error)
        {
            logger.LogError(error, "Unhandled exception: {Message}", error.Message);

            var response = context.Response;
            response.ContentType = "application/json";
            response.StatusCode  = error switch
            {
                AnalysisNotFoundException => (int)HttpStatusCode.NotFound,
                DomainError               => (int)HttpStatusCode.BadRequest,
                _                         => (int)HttpStatusCode.InternalServerError
            };

            var body = JsonSerializer.Serialize(new
            {
                title         = error.GetType().Name,
                status        = response.StatusCode,
                occurredAt    = DateTime.UtcNow,
                error         = error.Message,
                correlationId = context.TraceIdentifier
            });

            await response.WriteAsync(body);
        }
    }
}
