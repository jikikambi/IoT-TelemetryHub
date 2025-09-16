using Grpc.Core;
using IoT.Contracts;
using System.Collections.Concurrent;

namespace IoT.DeviceApp.IntegrationTests.Application.Services;

public class FakeDeviceGatewayService(Action<DeviceCommand>? onCommandProcessed = null) : DeviceGateway.DeviceGatewayBase
{
    private readonly ConcurrentQueue<DeviceCommand> _commands = new();
    private readonly ConcurrentQueue<Telemetry> _telemetryQueue = new();    
    private readonly ConcurrentBag<Telemetry> _receivedTelemetry = [];
    public IReadOnlyCollection<Telemetry> ReceivedTelemetry => [.. _receivedTelemetry];
    public Action<Telemetry>? OnTelemetryReceived { get; set; }
    public List<DeviceCommand> ProcessedCommands { get; } = [];
    public readonly ConcurrentBag<TelemetryAck> ReceivedAcks = [];

    // Preload fake commands into the queue
    public void EnqueueCommand(DeviceCommand command) => _commands.Enqueue(command);
    public void EnqueueTelemetry(Telemetry telemetry) => _telemetryQueue.Enqueue(telemetry);
    public bool TryDequeueCommand(out DeviceCommand cmd) => _commands.TryDequeue(out cmd);
    public bool TryDequeueTelemetry(out Telemetry telemetry) => _telemetryQueue.TryDequeue(out telemetry);

    public override Task<DeviceConnectResponse> ConnectDevice(DeviceConnectRequest request, ServerCallContext context)
    {
        // Always accept and return a dummy JWT
        return Task.FromResult(new DeviceConnectResponse
        {
            JwtToken = "fake-jwt-token",
            Message = $"Connected device {request.DeviceId}"
        });
    }

    public override async Task StreamTelemetry(
        IAsyncStreamReader<Telemetry> requestStream,
        IServerStreamWriter<TelemetryAck> responseStream,
        ServerCallContext context)
    {
        await foreach (var telemetry in requestStream.ReadAllAsync())
        {
            _receivedTelemetry.Add(telemetry);

            // Immediately acknowledge all telemetry
            await responseStream.WriteAsync(new TelemetryAck
            {
                Success = true,
                Message = $"Ack {telemetry.Type} from {telemetry.DeviceId}"
            });
        }
    }

    public override async Task ReceiveCommands(
        DeviceIdentity request,
        IServerStreamWriter<DeviceCommand> responseStream,
        ServerCallContext context)
    {
        // Stream preloaded commands to the device
        while (!context.CancellationToken.IsCancellationRequested)
        {
            while (_commands.TryDequeue(out var cmd))
            {
                ProcessedCommands.Add(cmd);
                onCommandProcessed?.Invoke(cmd);

                await responseStream.WriteAsync(cmd);
            }

            await Task.Delay(200, context.CancellationToken); // prevent busy loop
        }
    }
}
  