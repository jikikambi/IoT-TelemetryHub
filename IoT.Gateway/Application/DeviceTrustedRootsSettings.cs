namespace IoT.Gateway.Application;

public class DeviceTrustedRootsSettings
{
    // Key = DeviceId, Value = path to certificate
    public Dictionary<string, string> DeviceRoots { get; set; } = [];
}