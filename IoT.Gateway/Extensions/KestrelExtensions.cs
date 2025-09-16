using IoT.Gateway.Application;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Cryptography.X509Certificates;

namespace IoT.Gateway.Extensions;

public static class KestrelExtensions
{
    public static WebApplicationBuilder ConfigureGatewayKestrel(this WebApplicationBuilder builder)
    {
        // Bind config section to settings class
        var kestrelSettings = new KestrelSettings();
        builder.Configuration.GetSection("KestrelSettings").Bind(kestrelSettings);

        // Build absolute path (relative paths should resolve from app base dir)
        var certPath = Path.Combine(AppContext.BaseDirectory, kestrelSettings.CertPath);
        var caCertPath = Path.Combine(AppContext.BaseDirectory, kestrelSettings.CertAuthority);

        if (!File.Exists(certPath))
        {
            throw new FileNotFoundException($"Certificate not found at path: {certPath}");
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            // Enable h2c (HTTP/2 over plaintext) for dev
            options.ListenLocalhost(80, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
            //options.ApplyGatewayKestrel(caCertPath, certPath, kestrelSettings.CertPassword, kestrelSettings.ExpectedIssuer);
        });

        return builder;
    }

    private static void ApplyGatewayKestrel(
    this KestrelServerOptions options,
    string caCertPath,
    string certPath,
    string certPassword,
    string expectedIssuer)
    {
        options.ListenAnyIP(7019, listenOptions =>
        {
            listenOptions.UseHttps(certPath, certPassword, httpsOptions =>
            {
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                httpsOptions.CheckCertificateRevocation = false;

                // Properly load CA cert (PEM assumed)
                //var caPem = File.ReadAllText(certAuthorityPath);
                //using var caCert = X509Certificate2.CreateFromPem(caPem);
                //var caCertPath = Path.Combine(AppContext.BaseDirectory, "Certs", "ca.crt");
                var caCert = new X509Certificate2(caCertPath);

                httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
                {
                    if (cert == null) return false;

                    chain.ChainPolicy.ExtraStore.Add(caCert);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    bool isValid = chain.Build(cert);

                    // Require that our CA is in the chain
                    return isValid && chain.ChainElements
                        .Cast<X509ChainElement>()
                        .Any(e => e.Certificate.Thumbprint == caCert.Thumbprint);
                };
            });
        });
    }
}