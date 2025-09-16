namespace IoT.Shared.Mq;

public class RabbitQueue
{
    public string Name { get; set; } = default!;
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public List<RabbitBinding> Bindings { get; set; } = [];
}