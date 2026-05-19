using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Tests.Helpers;
using fiap_hackaton.Tests.Integration.Fixtures;

namespace fiap_hackaton.Tests.Integration.Api;

public class AnalysesEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AnalysesEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ─── POST /api/v1/analyses ────────────────────────────────────────────────

    [Fact]
    public async Task PostAnalyses_WithValidImage_ShouldReturn201()
    {
        var content = TestHelpers.CreateFileContent("diagram.png", "image/png", 1024);

        var response = await _client.PostAsync("/api/v1/analyses", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.AnalysisId);
        Assert.Equal("Received", body.Status);
    }

    [Fact]
    public async Task PostAnalyses_WithValidPdf_ShouldReturn201()
    {
        var content = TestHelpers.CreateFileContent("arch.pdf", "application/pdf", 2048);

        var response = await _client.PostAsync("/api/v1/analyses", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostAnalyses_WithEmptyFile_ShouldReturn400()
    {
        var content = TestHelpers.CreateFileContent("empty.png", "image/png", 0);

        var response = await _client.PostAsync("/api/v1/analyses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAnalyses_WithFileTooLarge_ShouldReturn400()
    {
        var content = TestHelpers.CreateFileContent("huge.png", "image/png", 11 * 1024 * 1024);

        var response = await _client.PostAsync("/api/v1/analyses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAnalyses_WithUnsupportedContentType_ShouldReturn400()
    {
        var content = TestHelpers.CreateFileContent("virus.exe", "application/octet-stream", 512);

        var response = await _client.PostAsync("/api/v1/analyses", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─── GET /api/v1/analyses/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetAnalysis_WithExistingId_ShouldReturn200()
    {
        var id = await CreateAnalysisInDbAsync();

        var response = await _client.GetAsync($"/api/v1/analyses/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalysisResponse>(JsonOpts);
        Assert.Equal(id, body!.Id);
        Assert.Equal("Received", body.Status);
    }

    [Fact]
    public async Task GetAnalysis_WithNonExistentId_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/v1/analyses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── GET /api/v1/analyses/{id}/status ────────────────────────────────────

    [Fact]
    public async Task GetStatus_WithExistingId_ShouldReturn200WithStatus()
    {
        var id = await CreateAnalysisInDbAsync();

        var response = await _client.GetAsync($"/api/v1/analyses/{id}/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StatusResponse>(JsonOpts);
        Assert.Equal("Received", body!.Status);
    }

    [Fact]
    public async Task GetStatus_WithNonExistentId_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/v1/analyses/{Guid.NewGuid()}/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── GET /api/v1/analyses/{id}/report (endpoint legado da api) ───────────
    // Este endpoint ainda existe na api e usa AppDbContext.
    // Em produção, novos reports são gravados em worker_db pelo analysis-worker
    // e servidos pelo report-service em GET /api/v1/reports/{id}.
    // Estes testes validam o contrato do endpoint da api em isolamento,
    // inserindo Reports diretamente no AppDbContext (InMemory).
    // Veja ReportEndpointTests para os testes do report-service.

    [Fact]
    public async Task GetReport_WithProcessedAnalysis_ShouldReturn200()
    {
        var id = await CreateProcessedAnalysisInDbAsync();

        var response = await _client.GetAsync($"/api/v1/analyses/{id}/report");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportResponse>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal("component-a, component-b", body.Components);
        Assert.Equal("no redundancy", body.Risks);
        Assert.Equal("add replica", body.Recommendations);
    }

    [Fact]
    public async Task GetReport_WithUnprocessedAnalysis_ShouldReturn400()
    {
        var id = await CreateAnalysisInDbAsync(); // status = Received

        var response = await _client.GetAsync($"/api/v1/analyses/{id}/report");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_WithNonExistentId_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/v1/analyses/{Guid.NewGuid()}/report");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── Correlation ID ───────────────────────────────────────────────────────

    [Fact]
    public async Task AllResponses_ShouldContainCorrelationIdHeader()
    {
        var response = await _client.GetAsync($"/api/v1/analyses/{Guid.NewGuid()}");

        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task Request_WithCorrelationId_ShouldEchoItBack()
    {
        var correlationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/analyses/{Guid.NewGuid()}");
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").First());
    }

    // ─── Health ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHealth_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateAnalysisInDbAsync()
    {
        using var db     = _factory.CreateDbContext();
        var analysis     = Analysis.Create("test.png", "image/png", "/tmp/test.png").Value;
        await db.Analyses.AddAsync(analysis);
        await db.SaveChangesAsync();
        return analysis.Id;
    }

    private async Task<Guid> CreateProcessedAnalysisInDbAsync()
    {
        using var db = _factory.CreateDbContext();
        var analysis = Analysis.Create("test.png", "image/png", "/tmp/test.png").Value;
        var report   = Report.Create(analysis.Id, "component-a, component-b", "no redundancy", "add replica");
        analysis.MarkAsProcessed(report);
        await db.Analyses.AddAsync(analysis);
        await db.SaveChangesAsync();
        return analysis.Id;
    }

    // ─── Response DTOs (anonymous deserialization) ────────────────────────────

    private record CreateResponse(Guid AnalysisId, string Status, DateTime CreatedAt);
    private record AnalysisResponse(Guid Id, string FileName, string ContentType, string Status, DateTime CreatedAt, DateTime UpdatedAt, string? ErrorMessage);
    private record StatusResponse(Guid Id, string Status, DateTime UpdatedAt, string? ErrorMessage);
    private record ReportResponse(Guid AnalysisId, string Components, string Risks, string Recommendations, DateTime GeneratedAt);
}
