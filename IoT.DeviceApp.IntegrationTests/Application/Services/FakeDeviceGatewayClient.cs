using Grpc.Core;
using IoT.Contracts;
using IoT.DeviceApp.Application.Services;
using IoT.DeviceApp.IntegrationTests.Application.Helpers;
using IoT.Shared.Testing;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace IoT.DeviceApp.IntegrationTests.Application.Services;

public class FakeDeviceGatewayClient(FakeDeviceGatewayService service) : IDeviceGatewayClient
{

    public readonly ConcurrentBag<DeviceCommand> ProcessedCommands = [];
    public readonly ConcurrentBag<Telemetry> SentTelemetry = [];
    public readonly ConcurrentBag<TelemetryAck> ReceivedAcks = [];

    public Task<string> ConnectDeviceAsync(string deviceId, string certPem, CancellationToken cancellationToken = default)
    {
        // Simulate successful connection
        return Task.FromResult(TestJwtHelper.CreateFakeJwt(deviceId));
    }

    public async IAsyncEnumerable<TelemetryAck> StreamTelemetryAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var call = await CreateTelemetryDuplexCallAsync(cancellationToken);

        while (await call.ResponseStream.MoveNext(cancellationToken))
        {
            var ack = call.ResponseStream.Current;
            yield return ack;
        }
    }

    // Dequeues commands and adds them to ProcessedCommands (ConcurrentBag).
    public async IAsyncEnumerable<DeviceCommand> ReceiveCommandsAsync(
        string deviceId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (service.TryDequeueCommand(out var cmd))
            {
                ProcessedCommands.Add(cmd);
                yield return cmd;
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    public Task<AsyncDuplexStreamingCall<Telemetry, TelemetryAck>> CreateTelemetryDuplexCallAsync(
    CancellationToken cancellationToken = default)
    {
        var callWrapper = new FakeAsyncDuplexTelemetryCall();

        // Optionally: simulate processing incoming telemetry
        _ = Task.Run(async () =>
        {
            await foreach (var telemetry in callWrapper.Requests.Reader.ReadAllAsync(cancellationToken))
            {
                SentTelemetry.Add(telemetry); 
                service.OnTelemetryReceived?.Invoke(telemetry);

                var ack = new TelemetryAck { Success = true, Message = $"Received Ack  {telemetry.Type}  from {telemetry.DeviceId}" };
                ReceivedAcks.Add(ack);
                await callWrapper.Responses.Writer.WriteAsync(ack, cancellationToken);
            }

        }, cancellationToken);

        return Task.FromResult(callWrapper.CreateCall());
    }

}
