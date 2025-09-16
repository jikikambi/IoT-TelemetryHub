using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace IoT.Shared.Helper;

public class CertLoader(IConfiguration configuration, string rootCertPath = null!, X509KeyStorageFlags? keyStorageFlags = null) : ICertLoader
{
    private readonly string _rootCertPath = rootCertPath ?? Path.Combine(AppContext.BaseDirectory, "Certs");
    private readonly string _basePassword = configuration.GetSection("CertPasswords")["BasePassword"]
                        ?? throw new InvalidOperationException("Base password not configured in User Secrets or environment.");
    private readonly X509KeyStorageFlags _keyStorageFlags = keyStorageFlags ?? X509KeyStorageFlags.EphemeralKeySet;

    public X509Certificate2 LoadRootCA()
    {
        var path = Path.Combine(_rootCertPath, "CA", "ca.crt");
        if (!File.Exists(path))
            throw new FileNotFoundException($"CA certificate not found at {path}");

        return new X509Certificate2(File.ReadAllBytes(path));
    }

    public X509Certificate2 LoadGatewayCert()
    {
        var gatewayPath = Path.Combine(_rootCertPath, "Gateway", "gateway.pfx");
        if (!File.Exists(gatewayPath))
            throw new FileNotFoundException($"Gateway certificate not found at {gatewayPath}");

        var password = _basePassword + "123";
        return new X509Certificate2(gatewayPath, password, _keyStorageFlags);
    }

    public X509Certificate2 LoadDeviceCert(string deviceName)
    {
        // Validate device name first
        if (!Regex.IsMatch(deviceName, @"^device-\d{3}$"))
            throw new ArgumentException($"Invalid device name: {deviceName}. Must be 'device-###'");

        var parts = deviceName.Split('-');

        if (parts.Length != 2 || !int.TryParse(parts[1], out int deviceNumber))
            throw new ArgumentException($"Invalid device name: {deviceName}. Must be 'device-###'");

        var deviceFolder = Path.Combine(_rootCertPath, "Devices", deviceName);
        var pfxPath = Path.Combine(deviceFolder, $"{deviceName}.pfx");

        if (!File.Exists(pfxPath))
            throw new FileNotFoundException($"Device certificate not found at {pfxPath}");

        var password = _basePassword + deviceNumber.ToString("D3");
        return new X509Certificate2(pfxPath, password, _keyStorageFlags);
    }
}