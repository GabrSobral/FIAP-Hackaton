using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraStorage(this IServiceCollection services)
    {
        services.AddScoped<IFileStorage, LocalFileStorage>();
        return services;
    }
}
