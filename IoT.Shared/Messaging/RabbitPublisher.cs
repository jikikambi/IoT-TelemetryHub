using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace IoT.Shared.Messaging;

public class RabbitPublisher(IConnection connection, 
    IModel model,
    JsonSerializerOptions jsonOptions) : IMessagePublisher, IDisposable
{

    public Task PublishAsync<T>(string exchange, string routingKey, T body, CancellationToken ct = default)
    {
        var props = model.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;

        var json = JsonSerializer.Serialize(body, jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        model.BasicPublish(exchange, routingKey, props, bytes);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { model?.Close(); } catch { }
        try { connection?.Close(); } catch { }

        model?.Dispose();
        connection?.Dispose();
    }
}