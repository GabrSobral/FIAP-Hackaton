using fiap_hackaton.Domain.Interfaces;
using fiap_hackaton.Domain.Interfaces.Repositories;
using fiap_hackaton.Infrastructure.Database.Repositories;
using fiap_hackaton.Infrastructure.Database.Utils;
using Microsoft.EntityFrameworkCore;

namespace fiap_hackaton.Infrastructure.Database;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        services.AddScoped<IAnalysisLogRepository, AnalysisLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException("Connection string 'PostgreSql' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddWorkerDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WorkerDb")
            ?? throw new InvalidOperationException("Connection string 'WorkerDb' is not configured.");

        services.AddDbContext<WorkerDbContext>(options =>
            options.UseNpgsql(connectionString,
                o => o.MigrationsAssembly("fiap-hackaton")
                       .MigrationsHistoryTable("__WorkerMigrationsHistory")));

        return services;
    }
}
