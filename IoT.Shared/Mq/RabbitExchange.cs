namespace IoT.Shared.Mq;

public class RabbitExchange
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = "direct";
    public bool Durable { get; set; } = true;
}