namespace IoT.Shared.Requests;

public record CommandRequest(string Command, object? Payload = null);

public class CommandMessage
{
    public string CommandId { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public object Payload { get; set; } = new { };
    public DateTimeOffset SentAt { get; set; }
}