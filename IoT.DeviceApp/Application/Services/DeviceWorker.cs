using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using IoT.Contracts;
using IoT.Shared.Helper;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IoT.DeviceApp.Application.Services;

public class DeviceWorker(ILogger<DeviceWorker> logger,
    IOptions<DeviceAppSettings> options,
    ICertLoader certLoader,
    IDeviceGatewayClient gatewayClient) : BackgroundService
{
    // JWT storage
    private string _jwtToken = "";
    private DateTime _jwtExpiry = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var deviceAppSettings = options.Value;
        var deviceId = deviceAppSettings.DeviceId;

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new InvalidOperationException("DeviceId is not configured.");

        // Load device certificate directly via CertLoader
        var deviceCert = certLoader.LoadDeviceCert(deviceId);

        // For gRPC, we usually need the cert in PEM/base64 format
        var certPem = ExportToPem(deviceCert);

        // Connect and get JWT
        _jwtToken = await gatewayClient.ConnectDeviceAsync(deviceId, certPem, stoppingToken);

        SetJwt(_jwtToken);
        logger.LogInformation("Connected. JWT expires at: {Expiry}", _jwtExpiry);

        // Run all background loops
        var tasks = new List<Task>
        {
             // Step 2: Start background JWT refresh loop
            JwtRefreshLoop(deviceId, deviceCert, stoppingToken),
             // Step 3: Start telemetry streaming
            TelemetryLoop(deviceId, deviceAppSettings.TelemetryType, stoppingToken),
             // Step 4: Start receiving commands
            ReceiveCommandsLoop(deviceId, stoppingToken)
        };

        await Task.WhenAll(tasks); // graceful shutdown
    }

    private static string ExportToPem(X509Certificate2 cert)
    {
        var builder = new StringBuilder();
        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");
        return builder.ToString();
    }

    private void SetJwt(string token)
    {
        _jwtToken = token;
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        _jwtExpiry = jwt.ValidTo; // UTC
    }

    private async Task JwtRefreshLoop(string deviceId, X509Certificate2 cert, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            if (_jwtExpiry < now.AddMinutes(5))
            {
                try
                {
                    var certPem = ExportToPem(cert);
                    _jwtToken = await gatewayClient.ConnectDeviceAsync(deviceId, certPem, cancellationToken: ct);
                    SetJwt(_jwtToken);
                    logger.LogInformation("JWT refreshed successfully. Expires at {Expiry}", _jwtExpiry);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to refresh JWT");
                }
            }
            // Adaptive delay: check more often as expiry approaches
            var delayMs = Math.Max((_jwtExpiry - now).TotalMilliseconds / 2, 1000);
            await Task.Delay((int)delayMs, ct);
        }
    }

    // Creates telemetry and writes it into the duplex stream.
    private async Task TelemetryLoop(string deviceId, string telemetryType, CancellationToken ct)
    {
        using var call = await gatewayClient.CreateTelemetryDuplexCallAsync(ct);

        // Task for sending telemetry
        var sendTask = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                var telemetry = new Telemetry
                {
                    DeviceId = deviceId,
                    Type = telemetryType,
                    IsoTimestamp = now.ToString("o"),
                    PayloadJson = "{\"value\":22.5}"
                };

                await call.RequestStream.WriteAsync(telemetry, ct);
                await Task.Delay(options.Value.TelemetryInterval, ct);
            }

            await call.RequestStream.CompleteAsync();
        }, ct);

        // Read acknowledgements
        await foreach (var ack in call.ResponseStream.ReadAllAsync(ct))
        {
            logger.LogInformation("Telemetry acknowledged: {Success}", ack.Success);
        }

        await sendTask;
    }

    // Reads commands and logs them.
    private async Task ReceiveCommandsLoop(string deviceId, CancellationToken ct)
    {
        await foreach (var cmd in gatewayClient.ReceiveCommandsAsync(deviceId, ct))
        {
            logger.LogInformation("Received command: {Name}, Payload: {Payload}", cmd.Name, cmd.PayloadJson);
        }
    }
}