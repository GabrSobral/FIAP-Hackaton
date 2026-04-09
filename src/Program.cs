using fiap_hackaton.Application;
using fiap_hackaton.Infrastructure.Ai;
using fiap_hackaton.Infrastructure.Database;
using fiap_hackaton.Infrastructure.Messaging;
using fiap_hackaton.Infrastructure.Storage;
using fiap_hackaton.Presentation;
using fiap_hackaton.Presentation.Endpoints;
using fiap_hackaton.Presentation.Middlewares;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfraAi(builder.Configuration)
    .AddInfraDatabase(builder.Configuration)
    .AddInfraMessaging(builder.Configuration)
    .AddInfraStorage()
    .AddPresentation();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAntiforgery();
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending EF Core migrations automatically on startup
// so Docker deployments work without manual 'dotnet ef database update'.
// Guard with IsRelational() so integration tests using InMemory are unaffected.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors();

// Correlation ID must be first so all downstream logs and responses carry it
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.MapAnalysesEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

// Expose Program to integration test project
public partial class Program { }
