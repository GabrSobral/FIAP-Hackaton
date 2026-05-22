using fiap_hackaton.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using report_service;

// Register Inter font from embedded resources before first document is generated
QuestPDF.Settings.License = LicenseType.Community;
foreach (var weight in new[] { "Regular", "Medium", "SemiBold", "Bold" })
{
    using var fontStream = typeof(PdfReportGenerator).Assembly
        .GetManifestResourceStream($"report_service.Fonts.Inter-{weight}.ttf");
    if (fontStream is not null) FontManager.RegisterFont(fontStream);
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWorkerDatabase(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

// Apply WorkerDbContext migrations on startup (idempotent)
using (var scope = app.Services.CreateScope())
{
    var workerDb = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
    await workerDb.Database.MigrateAsync();
}

app.MapGet("/api/v1/reports/{id:guid}", async (Guid id, WorkerDbContext db, CancellationToken ct) =>
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

app.MapGet("/api/v1/reports/{id:guid}/pdf", async (Guid id, WorkerDbContext db, CancellationToken ct) =>
{
    var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.AnalysisId == id, ct);
    if (report is null)
        return Results.NotFound(new { error = "Report not found or not yet generated." });

    // QuestPDF is CPU-bound — offload to thread pool to free the I/O thread
    var pdfBytes = await Task.Run(() => PdfReportGenerator.Generate(report), ct);

    return Results.File(pdfBytes, contentType: "application/pdf",
        fileDownloadName: $"report-{id}.pdf");
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "report-service" }));

app.Run();
