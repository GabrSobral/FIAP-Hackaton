using System.Text.Json;
using CA  = fiap_hackaton.Application.Services.Analyses.CreateAnalysis;
using GA  = fiap_hackaton.Application.Services.Analyses.GetAnalysis;
using GAS = fiap_hackaton.Application.Services.Analyses.GetAnalysisStatus;
using GAR = fiap_hackaton.Application.Services.Analyses.GetAnalysisReport;
using LA  = fiap_hackaton.Application.Services.Analyses.ListAnalyses;

namespace fiap_hackaton.Presentation.Endpoints;

public static class AnalysesEndpoints
{
    public static IEndpointRouteBuilder MapAnalysesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/analyses")
            .WithTags("Analyses");

        // GET /api/v1/analyses
        group.MapGet("/", ListAnalyses)
            .WithName("ListAnalyses")
            .WithSummary("List all analyses ordered by creation date (newest first)")
            .Produces<LA.Response>();

        // GET /api/v1/analyses/stream  — Server-Sent Events
        group.MapGet("/stream", StreamAnalyses)
            .WithName("StreamAnalyses")
            .WithSummary("SSE stream: pushes the full analyses list every 2 s");

        // POST /api/v1/analyses
        group.MapPost("/", UploadDiagram)
            .DisableAntiforgery()
            .WithName("CreateAnalysis")
            .WithSummary("Upload an architecture diagram (image or PDF) for AI analysis")
            .Produces<CA.Response>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // GET /api/v1/analyses/{id}
        group.MapGet("/{id:guid}", GetAnalysis)
            .WithName("GetAnalysis")
            .WithSummary("Get analysis details by ID")
            .Produces<GA.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // GET /api/v1/analyses/{id}/status
        group.MapGet("/{id:guid}/status", GetAnalysisStatus)
            .WithName("GetAnalysisStatus")
            .WithSummary("Get current processing status of an analysis")
            .Produces<GAS.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // GET /api/v1/analyses/{id}/report
        group.MapGet("/{id:guid}/report", GetAnalysisReport)
            .WithName("GetAnalysisReport")
            .WithSummary("Get the AI-generated report for a completed analysis")
            .Produces<GAR.Response>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAnalyses(
        LA.ListAnalysesService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(cancellationToken);

        if (result.IsFailure)
            throw result.Error!;

        return Results.Ok(result.Value);
    }

    private static async Task StreamAnalyses(
        HttpContext context,
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.ContentType  = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection   = "keep-alive";

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var scope   = scopeFactory.CreateAsyncScope();
                var service             = scope.ServiceProvider.GetRequiredService<LA.ListAnalysesService>();
                var result              = await service.ExecuteAsync(cancellationToken);

                if (result.IsSuccess)
                {
                    var json = JsonSerializer.Serialize(result.Value, JsonSerializerOptions.Web);
                    await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await Task.Delay(2000, cancellationToken);
        }
    }

    private static async Task<IResult> UploadDiagram(
        IFormFile file,
        CA.CreateAnalysisService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(new CA.Request(file), cancellationToken);

        if (result.IsFailure)
            throw result.Error!;

        return Results.Created($"/api/v1/analyses/{result.Value.AnalysisId}", result.Value);
    }

    private static async Task<IResult> GetAnalysis(
        Guid id,
        GA.GetAnalysisService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(new GA.Request(id), cancellationToken);

        if (result.IsFailure)
            throw result.Error!;

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAnalysisStatus(
        Guid id,
        GAS.GetAnalysisStatusService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(new GAS.Request(id), cancellationToken);

        if (result.IsFailure)
            throw result.Error!;

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAnalysisReport(
        Guid id,
        GAR.GetAnalysisReportService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(new GAR.Request(id), cancellationToken);

        if (result.IsFailure)
            throw result.Error!;

        return Results.Ok(result.Value);
    }
}
