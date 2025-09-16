namespace IoT.Shared.Mq;

public class RabbitBinding
{
    public string Exchange { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
}