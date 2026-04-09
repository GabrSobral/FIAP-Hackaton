using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventPublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>();
            try
            {
                return RabbitMqEventPublisher.CreateAsync(logger, configuration).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var nullLogger = sp.GetRequiredService<ILogger<NullEventPublisher>>();
                nullLogger.LogWarning(ex,
                    "Could not connect to RabbitMQ at startup. Using NullEventPublisher as fallback.");
                return new NullEventPublisher(nullLogger);
            }
        });

        // Background worker that consumes the 'diagram-analysis' queue
        services.AddHostedService<DiagramAnalysisConsumer>();

        return services;
    }
}
