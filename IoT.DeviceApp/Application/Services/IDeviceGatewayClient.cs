using Grpc.Core;
using IoT.Contracts;
using System.Runtime.CompilerServices;

namespace IoT.DeviceApp.Application.Services;

public interface IDeviceGatewayClient
{
    Task<string> ConnectDeviceAsync(string deviceId, string certPem, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TelemetryAck> StreamTelemetryAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);
    IAsyncEnumerable<DeviceCommand> ReceiveCommandsAsync(string deviceId, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    Task<AsyncDuplexStreamingCall<Telemetry, TelemetryAck>> CreateTelemetryDuplexCallAsync(CancellationToken cancellationToken = default);
}
