namespace fiap_hackaton.Presentation.Middlewares;

/// <summary>
/// Reads X-Correlation-Id from the incoming request (or generates one).
/// Injects it into the HttpContext.TraceIdentifier and echoes it back on all responses.
/// This ensures every log entry and every response carries the same correlation ID.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(HeaderName, correlationId);
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
