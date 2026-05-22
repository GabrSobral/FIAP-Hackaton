using System.Net;
using System.Text;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Infrastructure.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using report_service;

namespace fiap_hackaton.Tests.Integration.ReportService;

/// <summary>
/// Tests for the GET /api/v1/reports/{id}/pdf endpoint.
/// Mirrors the ReportEndpointTests pattern: minimal WebApplication + UseTestServer.
/// QuestPDF falls back to its bundled font when Inter is not registered — valid PDF is still produced.
/// </summary>
public class PdfEndpointTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client  = null!;
    private string _dbName      = null!;

    public async Task InitializeAsync()
    {
        _dbName = $"PdfEndpointDb_{Guid.NewGuid()}";

        QuestPDF.Settings.License = LicenseType.Community;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddDbContext<WorkerDbContext>(o =>
            o.UseInMemoryDatabase(_dbName));

        builder.Services.AddCors(o =>
            o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        _app = builder.Build();
        _app.UseCors();

        _app.MapGet("/api/v1/reports/{id:guid}/pdf", async (Guid id, WorkerDbContext db, CancellationToken ct) =>
        {
            var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.AnalysisId == id, ct);
            if (report is null)
                return Results.NotFound(new { error = "Report not found." });

            var pdf = await Task.Run(() => PdfReportGenerator.Generate(report), ct);
            return Results.File(pdf, contentType: "application/pdf", fileDownloadName: $"report-{id}.pdf");
        });

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> SeedAsync(
        string components = "api, db",
        string risks = "no circuit breaker",
        string recs = "add retries",
        string feedback = "")
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
        var analysisId = Guid.NewGuid();
        var report = string.IsNullOrEmpty(feedback)
            ? Report.Create(analysisId, components, risks, recs)
            : Report.Create(analysisId, components, risks, recs, feedback);
        await db.Reports.AddAsync(report);
        await db.SaveChangesAsync();
        return analysisId;
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReportPdf_ExistingReport_Returns200()
    {
        var id = await SeedAsync();
        var response = await _client.GetAsync($"/api/v1/reports/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReportPdf_ContentType_IsPdf()
    {
        var id = await SeedAsync();
        var response = await _client.GetAsync($"/api/v1/reports/{id}/pdf");
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetReportPdf_ContentDisposition_IsAttachment()
    {
        var id = await SeedAsync();
        var response = await _client.GetAsync($"/api/v1/reports/{id}/pdf");
        var cd = response.Content.Headers.ContentDisposition;
        Assert.NotNull(cd);
        Assert.Equal("attachment", cd!.DispositionType);
        Assert.Contains(id.ToString(), cd.FileName ?? string.Empty);
    }

    [Fact]
    public async Task GetReportPdf_Body_StartsWithPdfMagicBytes()
    {
        var id = await SeedAsync();
        var response = await _client.GetAsync($"/api/v1/reports/{id}/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes[..4]));
    }

    [Fact]
    public async Task GetReportPdf_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReportPdf_WithFeedback_Returns200()
    {
        var id = await SeedAsync(feedback: "Good overall architecture.");
        var response = await _client.GetAsync($"/api/v1/reports/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReportPdf_WithoutFeedback_Returns200()
    {
        var id = await SeedAsync(feedback: "");
        var response = await _client.GetAsync($"/api/v1/reports/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReportPdf_WithMarkdownContent_ProducesValidPdf()
    {
        var id = await SeedAsync(
            components: "## API Gateway\n- nginx\n- port 80\n\n## Backend\n- ASP.NET Core\n- **Clean Architecture**",
            risks: "### High Risk\n1. No circuit breaker\n2. Single database\n\n> Consider adding replicas",
            recs: "Add `retry policy` and circuit breaker.\n\n```yaml\ncircuit-breaker: enabled\n```");
        var response = await _client.GetAsync($"/api/v1/reports/{id}/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes[..4]));
    }
}
