using Grpc.Core;
using IoT.Contracts;
using IoT.Gateway.Application.Mappers;
using IoT.Gateway.Extensions;
using IoT.Shared.Helper;
using IoT.Shared.Messaging;
using IoT.Shared.Requests;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace IoT.Gateway.Application.Services;

public class DeviceGatewayService(ILogger<DeviceGatewayService> logger,
    IOptions<JwtSettings> jwtOptions,
    IConnection connection,
    IMessagePublisher publisher,
    IDeviceCommandMapper mapper,
    JsonSerializerOptions jsonOptions,
    ICertLoader certLoader) : DeviceGateway.DeviceGatewayBase
{
    private JwtSettings _jwtSettings = jwtOptions.Value;

    private static readonly ConcurrentDictionary<string, DateTime> _certValidationCache = new();

    // 1 ConnectDevice: validate device certificate & issue JWT
    public override async Task<DeviceConnectResponse> ConnectDevice(DeviceConnectRequest request, ServerCallContext context)
    {

        bool isValid = await ValidateDeviceCertificateAsync(request.DeviceId);

        if (!isValid)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid certificate"));
        }

        // Generate JWT
        _jwtSettings = jwtOptions.Value;
        var jwt = JwtTokenHelper.GenerateToken(
            request.DeviceId,
            $"device.{request.DeviceId}.set",
            _jwtSettings.Secret,
            _jwtSettings.ExpiryMinutes
        );

        return new DeviceConnectResponse
        {
            JwtToken = jwt,
            Message = "Connected successfully"
        };
    }

    // 2 StreamTelemetry: JWT enforced
    public override async Task StreamTelemetry(IAsyncStreamReader<Telemetry> requestStream, IServerStreamWriter<TelemetryAck> responseStream, ServerCallContext context)
    {
        // Extract deviceId from JWT claims
        var deviceId = context.GetHttpContext()?.User?.FindFirst("deviceId")?.Value;

        if (string.IsNullOrEmpty(deviceId))
            throw new RpcException(new Status(StatusCode.Unauthenticated, $"for {deviceId} JWT missing or invalid"));

        await foreach (var telemetry in requestStream.ReadAllAsync())
        {
            // Publish telemetry using your existing publisher
            var evt = new TelemetryEvent(
                MessageId: Guid.NewGuid().ToString(),
                OccurredAt: DateTimeOffset.Parse(telemetry.IsoTimestamp),
                DeviceId: telemetry.DeviceId,
                Type: telemetry.Type,
                PayloadJson: telemetry.PayloadJson ?? "{}"
            );
            await publisher.PublishAsync("telemetry.fanout", string.Empty, evt);

            // Acknowledge each telemetry message
            await responseStream.WriteAsync(new TelemetryAck
            {
                Message = "Received",
                Success = true
            });
        }
    }

    // 3️ ReceiveCommands: unchanged, RabbitMQ streaming
    public override async Task ReceiveCommands(DeviceIdentity request, IServerStreamWriter<DeviceCommand> responseStream, ServerCallContext context)
    {
        var deviceId = request.DeviceId;
        var queue = $"commands.{deviceId}";

        using var model = connection.CreateModel();
        model.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);

        var consumer = new EventingBasicConsumer(model);
        var tcs = new TaskCompletionSource();

        // Wire up Received event with async handler
        consumer.Received += (sender, ea) =>
        {
            _ = HandleCommandReceived(sender, ea, responseStream, model);
        };

        model.BasicConsume(queue, autoAck: false, consumer: consumer);

        // Complete Task when client disconnects
        context.CancellationToken.Register(() => tcs.TrySetResult());
        await tcs.Task;
    }

    private async Task HandleCommandReceived(
    object? sender,
    BasicDeliverEventArgs ea,
    IServerStreamWriter<DeviceCommand> responseStream,
    IModel channel)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            // Deserialize into typed CommandMessage
            var cmdMessage = JsonSerializer.Deserialize<CommandMessage>(json, jsonOptions);

            if (cmdMessage != null)
            {
                // Map to gRPC generated type for streaming
                var grpcCmd = mapper.Map(cmdMessage, jsonOptions);

                await responseStream.WriteAsync(grpcCmd);
                channel.BasicAck(ea.DeliveryTag, false);
            }
            else
            {
                channel.BasicNack(ea.DeliveryTag, false, false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error delivering command to device {DeviceId}", sender);
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    // Device certificate validation now uses CertLoader
    private async Task<bool> ValidateDeviceCertificateAsync(string deviceId)
    {
        try
        {
            var deviceCert = certLoader.LoadDeviceCert(deviceId);

            // 1. Quick cache check
            if (IsCachedValid(deviceCert.Thumbprint))
                return true;

            // 1. Check expiration
            if (DateTime.UtcNow < deviceCert.NotBefore || DateTime.UtcNow > deviceCert.NotAfter)
                return false;

            // 2. Check deviceId matches certificate CN
            var cn = deviceCert.GetNameInfo(X509NameType.SimpleName, false);
            if (!string.Equals(cn, deviceId, StringComparison.OrdinalIgnoreCase))
                return false;

            // 3. Verify certificate chain asynchronously
            return await Task.Run(() =>
            {
                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online; // or Offline
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                var rootCert = certLoader.LoadRootCA();
                chain.ChainPolicy.ExtraStore.Add(rootCert);

                if (!chain.Build(deviceCert)) return false;

                // 5. Cache valid cert
                CacheValidCert(deviceCert.Thumbprint);
                return true;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Certificate validation failed: {ex.Message}");
            return false;
        }
    }

    private static bool IsCachedValid(string thumbprint)
    {
        if (_certValidationCache.TryGetValue(thumbprint, out var expiry))
        {
            if (expiry > DateTime.UtcNow)
                return true;

            _certValidationCache.TryRemove(thumbprint, out _);
        }
        return false;
    }

    private static void CacheValidCert(string thumbprint)
    {
        _certValidationCache[thumbprint] = DateTime.UtcNow.AddHours(1);
    }
}