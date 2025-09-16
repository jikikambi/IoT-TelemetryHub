using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using IoT.Shared.Helper;

namespace IoT.Shared.UnitTests.Helper
{
    public class CertLoaderHappyPathTests
    {
        [Fact]
        public void CertLoader_ShouldLoadAllCertificates_WithCorrectPasswords()
        {
            // Arrange
            var tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var caDir = Path.Combine(tempRoot, "CA");
            var gatewayDir = Path.Combine(tempRoot, "Gateway");
            var deviceDir = Path.Combine(tempRoot, "Devices", "device-001");

            Directory.CreateDirectory(caDir);
            Directory.CreateDirectory(gatewayDir);
            Directory.CreateDirectory(deviceDir);

            var basePassword = "passW0rd";

            // -----------------------------
            // 1. Create dummy CA certificate
            // -----------------------------
            var caCertPath = Path.Combine(caDir, "ca.crt");
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest("CN=PulseNet Dev CA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var caCert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
                File.WriteAllBytes(caCertPath, caCert.Export(X509ContentType.Cert));
            }

            // -----------------------------
            // 2. Create dummy Gateway PFX
            // -----------------------------
            var gatewayPfx = Path.Combine(gatewayDir, "gateway.pfx");
            CreateDummyPfx(gatewayPfx, basePassword + "123");

            // -----------------------------
            // 3. Create dummy Device PFX
            // -----------------------------
            var devicePfx = Path.Combine(deviceDir, "device-001.pfx");
            CreateDummyPfx(devicePfx, basePassword + "001");

            // -----------------------------
            // 4. Configuration
            // -----------------------------
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("CertPasswords:BasePassword", basePassword) })
                .Build();

            var loader = new CertLoader(config, tempRoot);

            // -----------------------------
            // Act
            // -----------------------------
            var loadedCa = loader.LoadRootCA();
            var loadedGateway = loader.LoadGatewayCert();
            var loadedDevice = loader.LoadDeviceCert("device-001");

            // -----------------------------
            // Assert
            // -----------------------------
            loadedCa.Should().NotBeNull();
            loadedGateway.Should().NotBeNull();
            loadedDevice.Should().NotBeNull();

            loadedGateway.HasPrivateKey.Should().BeTrue();
            loadedDevice.HasPrivateKey.Should().BeTrue();
        }

        /// <summary>
        /// Helper to create a dummy PFX file for testing without requiring key store permissions.
        /// </summary>
        private static void CreateDummyPfx(string path, string password)
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=Dummy", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

            // Export to PFX in-memory
            var pfxBytes = cert.Export(X509ContentType.Pfx, password);
            File.WriteAllBytes(path, pfxBytes);
        }
    }
}
