namespace IoT.DeviceApp.Application.Services;

using Grpc.Core;
using IoT.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public class DeviceGatewayClientWrapper(DeviceGateway.DeviceGatewayClient client, Func<string> getJwtToken = null) : IDeviceGatewayClient
{
    private readonly Func<string> _getJwtToken = getJwtToken ?? (() => string.Empty);

    /// <summary>
    /// ConnectDevice with JWT automatically applied
    /// </summary>
    public async Task<string> ConnectDeviceAsync(string deviceId, string certPem, CancellationToken cancellationToken = default)
    {
        var request = new DeviceConnectRequest
        {
            DeviceId = deviceId,
            Certificate = certPem
        };

        var response = await client.ConnectDeviceAsync(request, cancellationToken: cancellationToken);
        return response.JwtToken;
    }

    /// <summary>
    /// Stream telemetry from device
    /// </summary>
    public async IAsyncEnumerable<TelemetryAck> StreamTelemetryAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var call = client.StreamTelemetry(cancellationToken: cancellationToken);       

        // DeviceWorker handles the loop; wrapper only exposes gRPC stream
        // Meanwhile, yield acks as they arrive
        await foreach (var ack in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return ack;
        }
    }

    /// <summary>
    /// Receive commands from gateway
    /// </summary>
    public async IAsyncEnumerable<DeviceCommand> ReceiveCommandsAsync(string deviceId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new DeviceIdentity { DeviceId = deviceId };
        using var call = client.ReceiveCommands(request, cancellationToken: cancellationToken);

        await foreach (var cmd in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return cmd;
        }
    }

    public Task<AsyncDuplexStreamingCall<Telemetry, TelemetryAck>> CreateTelemetryDuplexCallAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(client.StreamTelemetry(cancellationToken: cancellationToken));
    }
}

