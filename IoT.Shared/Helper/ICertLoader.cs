using System.Security.Cryptography.X509Certificates;

namespace IoT.Shared.Helper;

public interface ICertLoader
{
    X509Certificate2 LoadRootCA();
    X509Certificate2 LoadGatewayCert();
    X509Certificate2 LoadDeviceCert(string deviceName);
}
