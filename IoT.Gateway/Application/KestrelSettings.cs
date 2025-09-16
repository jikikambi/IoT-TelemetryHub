namespace IoT.Gateway.Application;

public class KestrelSettings
{
    public string CertAuthority { get; set; } = default!;
    public string CertPath { get; set; } = default!;
    public string CertPassword { get; set; } = default!;
    public string ExpectedIssuer { get; set; } = default!;
}