using fiap_hackaton.Domain.Interfaces;
using fiap_hackaton.Infrastructure.Database;
using fiap_hackaton.Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace fiap_hackaton.Tests.Integration.Fixtures;

/// <summary>
/// Replaces production infrastructure with test doubles:
///  - AppDbContext       → EF Core InMemory (unique DB per factory instance)
///  - IEventPublisher    → NullEventPublisher (no RabbitMQ)
///  - IHostedService     → all removed (no background workers)
///
/// The dual-provider problem (Npgsql + InMemory both registered) is solved by:
///  1. Removing DbContextOptions&lt;AppDbContext&gt; and AppDbContext descriptors.
///  2. Removing EF Core's internal IDbContextOptionsConfiguration&lt;AppDbContext&gt;
///     (which holds the Npgsql lambda) via reflection, since it is an internal type.
///  3. Re-registering AppDbContext with InMemory via pre-built options bypassing the
///     EF Core options-builder pipeline entirely.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // ── 1. Completely replace EF Core / Npgsql DbContext ─────────────────
            ReplaceDbContext(services, _dbName);

            // ── 2. Replace event publisher with no-op ─────────────────────────────
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(
                new NullEventPublisher(NullLogger<NullEventPublisher>.Instance));

            // ── 3. Remove background workers that need external infra ─────────────
            services.RemoveAll<IHostedService>();
        });
    }

    private static void ReplaceDbContext(IServiceCollection services, string dbName)
    {
        // Remove the standard EF Core registrations
        services.RemoveAll<AppDbContext>();
        services.RemoveAll<DbContextOptions<AppDbContext>>();

        // EF Core 7+ internally registers an IDbContextOptionsConfiguration<TContext>
        // per AddDbContext() call (holds the options lambda — e.g. UseNpgsql).
        // If that service is still present, our second AddDbContext() call below will
        // accumulate BOTH providers in the final DbContextOptions → exception.
        // Since the interface is internal to Microsoft.EntityFrameworkCore, we remove
        // those descriptors via reflection.
        try
        {
            var efAssembly  = typeof(DbContext).Assembly;
            var openCfgType = efAssembly.GetTypes().FirstOrDefault(
                t => t.IsInterface && t.IsGenericTypeDefinition
                  && t.Name == "IDbContextOptionsConfiguration`1");

            if (openCfgType is not null)
            {
                var closedCfgType   = openCfgType.MakeGenericType(typeof(AppDbContext));
                var staleDescriptors = services
                    .Where(d => d.ServiceType == closedCfgType)
                    .ToList();
                foreach (var d in staleDescriptors)
                    services.Remove(d);
            }
        }
        catch
        {
            // Reflection failed — fall back to direct pre-built options
        }

        // Pre-build options with ONLY the InMemory provider so no extension conflict
        // can occur regardless of what EF Core's service pipeline does.
        var inMemoryOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        services.AddSingleton<DbContextOptions<AppDbContext>>(_ => inMemoryOptions);
        services.AddScoped<AppDbContext>(sp =>
            new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));
    }

    /// <summary>Creates a fresh scoped DbContext for test arrange/assert steps.</summary>
    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
