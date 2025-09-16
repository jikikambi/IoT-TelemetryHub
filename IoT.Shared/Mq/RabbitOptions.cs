namespace IoT.Shared.Mq;

public class RabbitOptions
{
    public string Uri { get; set; } = default!;
    public List<RabbitExchange> Exchanges { get; set; } = [];
    public List<RabbitQueue> Queues { get; set; } = [];
}