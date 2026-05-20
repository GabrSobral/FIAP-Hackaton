using fiap_hackaton.Domain.Interfaces;

namespace fiap_hackaton.Infrastructure.Messaging;

/// <summary>
/// No-op publisher used as fallback when RabbitMQ is unavailable (e.g. local dev without Docker).
/// </summary>
public class NullEventPublisher(ILogger<NullEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
        where T : class
    {
        logger.LogWarning(
            "NullEventPublisher: event {EventType} with routing key '{RoutingKey}' was NOT published (no RabbitMQ connection).",
            typeof(T).Name, routingKey);

        return Task.CompletedTask;
    }
}
