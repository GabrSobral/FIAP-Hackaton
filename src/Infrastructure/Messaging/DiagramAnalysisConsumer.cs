using System.Text;
using System.Text.Json;
using fiap_hackaton.Domain.Entities.Analysis;
using fiap_hackaton.Domain.Events;
using fiap_hackaton.Domain.Interfaces;
using fiap_hackaton.Domain.Interfaces.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace fiap_hackaton.Infrastructure.Messaging;

/// <summary>
/// Background worker that consumes the 'diagram-analysis' queue from RabbitMQ,
/// calls the AI service to analyse each diagram, and persists the generated report.
///
/// Flow per message:
///   1. Deserialise DiagramUploadedEvent
///   2. Load Analysis from DB, mark status → PROCESSING
///   3. Call IAiAnalysisService.AnalyzeAsync(...)
///   4. Attach Report, mark status → PROCESSED
///   5. On any error → mark status → ERROR
/// </summary>
public class DiagramAnalysisConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiagramAnalysisConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string QueueName    = "diagram-analysis";
    private const string RoutingKey   = "diagram-analysis";

    public DiagramAnalysisConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DiagramAnalysisConsumer> logger)
    {
        _scopeFactory  = scopeFactory;
        _configuration = configuration;
        _logger        = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ConnectAsync(stoppingToken);
            _logger.LogInformation("DiagramAnalysisConsumer started. Listening on queue '{Queue}'.", QueueName);

            var consumer = new AsyncEventingBasicConsumer(_channel!);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel!.BasicConsumeAsync(
                queue:       QueueName,
                autoAck:     false,
                consumer:    consumer,
                cancellationToken: stoppingToken);

            // Keep alive until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("DiagramAnalysisConsumer stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DiagramAnalysisConsumer failed to start. Queue processing is disabled.");
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var messageBody = Encoding.UTF8.GetString(ea.Body.ToArray());
        _logger.LogInformation("Message received from queue '{Queue}': {Body}", QueueName, messageBody);

        DiagramUploadedEvent? @event;
        try
        {
            @event = JsonSerializer.Deserialize<DiagramUploadedEvent>(messageBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialise message. Sending to dead-letter.");
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (@event is null)
        {
            _logger.LogWarning("Received null event. Discarding message.");
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            await ProcessEventAsync(@event);
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing analysis {AnalysisId}.", @event.AnalysisId);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task ProcessEventAsync(DiagramUploadedEvent @event)
    {
        using var scope = _scopeFactory.CreateScope();
        var analysisRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRepository>();
        var aiService    = scope.ServiceProvider.GetRequiredService<IAiAnalysisService>();
        var logRepo      = scope.ServiceProvider.GetRequiredService<IAnalysisLogRepository>();
        var unitOfWork   = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var analysis = await analysisRepo.GetByIdAsync(@event.AnalysisId);

        if (analysis is null)
        {
            _logger.LogWarning("Analysis {AnalysisId} not found. Skipping.", @event.AnalysisId);
            return;
        }

        // Mark as PROCESSING
        analysis.MarkAsProcessing();
        await analysisRepo.UpdateAsync(analysis);
        await unitOfWork.SaveChangesAsync();

        await LogProgressAsync(_logger, logRepo, unitOfWork, @event.AnalysisId, "INFO",
            "Diagram loaded, starting AI analysis");

        try
        {
            _logger.LogInformation("Analysis {AnalysisId}: Sending diagram to AI service.", @event.AnalysisId);

            await LogProgressAsync(_logger, logRepo, unitOfWork, @event.AnalysisId, "info",
                $"Calling AI service ({@event.FileName})");

            var t0 = DateTime.UtcNow;
            var aiResult = await aiService.AnalyzeAsync(
                @event.FilePath,
                @event.FileName,
                @event.ContentType);
            var elapsed = (DateTime.UtcNow - t0).TotalSeconds;

            await LogProgressAsync(_logger, logRepo, unitOfWork, @event.AnalysisId, "info",
                $"AI analysis completed in {elapsed:F0}s — generating report");

            var report = Report.Create(
                @event.AnalysisId,
                aiResult.Components,
                aiResult.Risks,
                aiResult.Recommendations,
                aiResult.Feedback);

            analysis.MarkAsProcessed(report);
            await analysisRepo.UpdateAsync(analysis);
            await unitOfWork.SaveChangesAsync();

            await LogProgressAsync(_logger, logRepo, unitOfWork, @event.AnalysisId, "info",
                "Report generated successfully");

            _logger.LogInformation("Analysis {AnalysisId} completed successfully. Status → PROCESSED.", @event.AnalysisId);
        }
        catch (Exception ex)
        {
            await LogProgressAsync(_logger, logRepo, unitOfWork, @event.AnalysisId, "error",
                $"Analysis failed: {ex.Message}");

            analysis.MarkAsError(ex.Message);
            await analysisRepo.UpdateAsync(analysis);
            await unitOfWork.SaveChangesAsync();

            _logger.LogError(ex, "AI processing failed for analysis {AnalysisId}.", @event.AnalysisId);
        }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port     = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
        };

        var exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "fiap-hackaton-exchange";

        _connection = await factory.CreateConnectionAsync(ct);
        _channel    = await _connection.CreateChannelAsync(cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(
            exchange:   exchangeName,
            type:       ExchangeType.Topic,
            durable:    true,
            autoDelete: false,
            cancellationToken: ct);

        await _channel.QueueDeclareAsync(
            queue:      QueueName,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue:      QueueName,
            exchange:   exchangeName,
            routingKey: RoutingKey,
            cancellationToken: ct);

        // One message at a time — fair dispatch
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);
    }

    /// <summary>
    /// Logs the current analysis processing step to both DB and ILogger.
    /// </summary>
    private static async Task LogProgressAsync(
        ILogger logger,
        IAnalysisLogRepository logRepo,
        IUnitOfWork unitOfWork,
        Guid analysisId,
        string level,
        string message,
        CancellationToken ct = default)
    {
        logger.LogInformation("Analysis {AnalysisId}: [{Level}] {Message}", analysisId, level, message);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        await logRepo.AddAsync(new AnalysisLog(analysisId, level, message), timeoutCts.Token);
        await unitOfWork.SaveChangesAsync(timeoutCts.Token);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel?.IsOpen == true) await _channel.CloseAsync(cancellationToken);
        if (_connection?.IsOpen == true) await _connection.CloseAsync(cancellationToken);
    }
}
