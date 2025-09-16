namespace IoT.Shared.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T body, CancellationToken ct = default);
}