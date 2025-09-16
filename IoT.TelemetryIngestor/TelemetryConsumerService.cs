using IoT.Shared.Messaging;
using IoT.Shared.Mq;
using IoT.Telemetry.Persistence.Entities;
using IoT.Telemetry.Persistence.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace IoT.TelemetryIngestor;

public class TelemetryConsumerService(
    ILogger<TelemetryConsumerService> logger,
    IConnection connection,
    IModel model,
    JsonSerializerOptions jsonOptions,
    IServiceProvider serviceProvider,
    RabbitOptions rabbitOptions
) : BackgroundService
{
    private readonly List<EventingBasicConsumer> _consumers = new();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consumer setup (queue/exchange already declared in DI AddRabbitMQ)
        // Consume all queues defined in RabbitOptions
        foreach (var queue in rabbitOptions.Queues)
        {
            var consumer = new EventingBasicConsumer(model);
            consumer.Received += async (sender, e) =>
                await HandleMessageAsync(e, stoppingToken).ConfigureAwait(false);

            model.BasicConsume(
                queue: queue.Name,
                autoAck: false,
                consumer: consumer);

            _consumers.Add(consumer);
            logger.LogInformation("Started consuming queue: {QueueName}", queue.Name);
        }

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs e, CancellationToken ct)
    {
        try
        {
            var json = Encoding.UTF8.GetString(e.Body.ToArray());
            var telemetry = JsonSerializer.Deserialize<TelemetryEvent>(json, jsonOptions);

            if (telemetry == null)
            {
                logger.LogWarning("Received null telemetry message.");
                model.BasicAck(e.DeliveryTag, multiple: false);
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ITelemetryRepository>();

            var payloadJson = telemetry.PayloadJson is null
                ? "{}"
                : JsonSerializer.Serialize(telemetry.PayloadJson, jsonOptions);

            var log = new TelemetryLog
            {
                DeviceId = telemetry.DeviceId,
                Type = telemetry.Type,
                PayloadJson = payloadJson,
                Timestamp = DateTime.UtcNow
            };

            await repository.AddAsync(log, ct).ConfigureAwait(false);

            logger.LogInformation("Telemetry processed: {@Telemetry}", telemetry);

            model.BasicAck(e.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message. Sending Nack.");
            model.BasicNack(e.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("TelemetryConsumerService stopping...");

        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITelemetryRepository>();

        // Ensure all buffered logs are flushed before shutdown
        try { await repository.FlushAsync(cancellationToken); } catch { /* ignore */ }

        try { model?.Close(); } catch (Exception ex) { logger.LogWarning(ex, "Failed to close model."); }
        try { connection?.Close(); } catch (Exception ex) { logger.LogWarning(ex, "Failed to close connection."); }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        model?.Dispose();
        connection?.Dispose();
        base.Dispose();
    }
}