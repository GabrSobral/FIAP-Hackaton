using System.Text;
using System.Text.Json;
using fiap_hackaton.Domain.Interfaces;
using RabbitMQ.Client;

namespace fiap_hackaton.Infrastructure.Messaging;

/// <summary>
/// Publishes domain events to RabbitMQ via Topic Exchange.
/// The AI Processing Service subscribes with the 'diagram-analysis' binding key.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName;

    private RabbitMqEventPublisher(
        ILogger<RabbitMqEventPublisher> logger,
        IConnection connection,
        IChannel channel,
        string exchangeName)
    {
        _logger = logger;
        _connection = connection;
        _channel = channel;
        _exchangeName = exchangeName;
    }

    public static async Task<RabbitMqEventPublisher> CreateAsync(
        ILogger<RabbitMqEventPublisher> logger,
        IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port     = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
        };

        var exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "fiap-hackaton-exchange";

        var connection = await factory.CreateConnectionAsync();
        var channel    = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange:   exchangeName,
            type:       ExchangeType.Topic,
            durable:    true,
            autoDelete: false);

        return new RabbitMqEventPublisher(logger, connection, channel, exchangeName);
    }

    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken cancellationToken = default)
        where T : class
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

        var properties = new BasicProperties
        {
            ContentType  = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
        };

        await _channel.BasicPublishAsync(
            exchange:        _exchangeName,
            routingKey:      routingKey,
            mandatory:       false,
            basicProperties: properties,
            body:            body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Event published — RoutingKey: {RoutingKey}, Type: {EventType}",
            routingKey, typeof(T).Name);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel.IsOpen) await _channel.CloseAsync();
        if (_connection.IsOpen) await _connection.CloseAsync();
    }
}
