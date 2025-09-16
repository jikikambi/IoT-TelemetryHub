using Grpc.Core;
using IoT.Contracts;
using System.Threading.Channels;

namespace IoT.DeviceApp.IntegrationTests.Application.Helpers;

public class FakeClientStreamWriter(Channel<Telemetry> channel) : IClientStreamWriter<Telemetry>
{
    public WriteOptions? WriteOptions { get; set; }

    public Task CompleteAsync()
    {
        channel.Writer.Complete();
        return Task.CompletedTask;
    }

    public Task WriteAsync(Telemetry message, CancellationToken ct = default) => channel.Writer.WriteAsync(message, ct).AsTask();

    public Task WriteAsync(Telemetry message) => channel.Writer.WriteAsync(message).AsTask();
}

