using fiap_hackaton.Infrastructure.Ai;
using fiap_hackaton.Infrastructure.Database;
using fiap_hackaton.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfraDatabase(context.Configuration);
        services.AddWorkerDatabase(context.Configuration);
        services.AddInfraAi(context.Configuration);
        services.AddHostedService<DiagramAnalysisConsumer>();
    })
    .Build();

// Apply WorkerDbContext migrations on startup (creates Reports table in worker_db)
using (var scope = host.Services.CreateScope())
{
    var workerDb = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();
    await workerDb.Database.MigrateAsync();
}

await host.RunAsync();
