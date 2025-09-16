namespace IoT.DeviceApp.Application;

public class DeviceAppSettings
{
    public string DeviceId { get; set; } = default!;
    public string GatewayUri { get; set; } = default!;
    public string TelemetryType { get; set; } = default!;
    /// <summary>
    /// Interval between telemetry messages. Default = 5s (production).
    /// </summary>
    public TimeSpan TelemetryInterval { get; set; } = TimeSpan.FromSeconds(5);
}
