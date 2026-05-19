using fiap_hackaton.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "report-service" }));

app.Run();
