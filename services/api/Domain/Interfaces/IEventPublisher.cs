namespace fiap_hackaton.Domain.Interfaces;

/// <summary>
/// Abstraction for publishing domain events to an async message broker.
/// Concrete implementation uses RabbitMQ Topic Exchange.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default) where T : class;
}
