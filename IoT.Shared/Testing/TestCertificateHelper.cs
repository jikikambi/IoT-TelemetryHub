using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IoT.Shared.Testing;

public static class TestCertificateHelper
{
    public static X509Certificate2 CreateTestCertificate(string deviceId)
    {
        using var rsa = RSA.Create(2048);

        var certRequest = new CertificateRequest(
            $"CN=localhost, O={deviceId}", // match OpenSSL subject
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Self-signed cert valid for 1 year
        var cert = certRequest.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));

        return cert;
    }
}