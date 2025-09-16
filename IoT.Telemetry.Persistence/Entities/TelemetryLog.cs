namespace IoT.Telemetry.Persistence.Entities;

public class TelemetryLog
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}
