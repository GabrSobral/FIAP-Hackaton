using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Infrastructure.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace fiap_hackaton.Tests.Integration.ReportService;

/// <summary>
/// Tests for the GET /api/v1/reports/{id} endpoint served by report-service.
/// Since report-service is a separate process, we spin up a minimal WebApplication
/// that mirrors its endpoint logic and WorkerDbContext registration.
/// </summary>
public class ReportEndpointTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private string _dbName = null!;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync()
    {
        _dbName = $"ReportServiceDb_{Guid.NewGuid()}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddDbContext<WorkerDbContext>(o =>
            o.UseInMemoryDatabase(_dbName));

        builder.Services.AddCors(o =>
            o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        _app = builder.Build();
        _app.UseCors();

        _app.MapGet("/api/v1/reports/{id:guid}", async (Guid id, WorkerDbContext db, CancellationToken ct) =>
        {
            var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.AnalysisId == id, ct);
            if (report is null)
                return Results.NotFound(new { error = "Report not found or not yet generated." });

            return Results.Ok(new
            {
                analysisId      = report.AnalysisId,
                components      = report.Components,
                risks           = report.Risks,
                recommendations = report.Recommendations,
                feedback        = report.Feedback,
                generatedAt     = report.GeneratedAt
            });
        });

        _app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "report-service" }));

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedReportAsync(string components = "api, db", string risks = "single point of failure on database", string recs = "add read replica")
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db        = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
        var analysisId = Guid.NewGuid();
        var report     = Report.Create(analysisId, components, risks, recs);
        await db.Reports.AddAsync(report);
        await db.SaveChangesAsync();
        return analysisId;
    }

    // ── GET /api/v1/reports/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetReport_WithExistingReport_ShouldReturn200WithAllFields()
    {
        var analysisId = await SeedReportAsync(
            components: "api-gateway, service-a, postgres",
            risks:      "no circuit breaker on service-a calls",
            recs:       "add circuit breaker and retry policy");

        var response = await _client.GetAsync($"/api/v1/reports/{analysisId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ReportResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(analysisId,                             body.AnalysisId);
        Assert.Equal("api-gateway, service-a, postgres",     body.Components);
        Assert.Equal("no circuit breaker on service-a calls", body.Risks);
        Assert.Equal("add circuit breaker and retry policy", body.Recommendations);
    }

    [Fact]
    public async Task GetReport_WithNonExistentAnalysisId_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_ReturnsAnalysisId_MatchingRequest()
    {
        var analysisId = await SeedReportAsync();

        var response = await _client.GetAsync($"/api/v1/reports/{analysisId}");
        var body     = await response.Content.ReadFromJsonAsync<ReportResponse>(JsonOpts);

        Assert.Equal(analysisId, body!.AnalysisId);
    }

    [Fact]
    public async Task GetReport_WithFeedback_ShouldIncludeFeedbackInResponse()
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db        = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
        var analysisId = Guid.NewGuid();
        var report     = Report.Create(analysisId, "load balancer, app, rds", "no auto-scaling configured", "enable auto-scaling group", "Good overall structure.");
        await db.Reports.AddAsync(report);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/v1/reports/{analysisId}");
        var body     = await response.Content.ReadFromJsonAsync<ReportResponse>(JsonOpts);

        Assert.Equal("Good overall structure.", body!.Feedback);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private record ReportResponse(
        Guid     AnalysisId,
        string   Components,
        string   Risks,
        string   Recommendations,
        string   Feedback,
        DateTime GeneratedAt);
}
