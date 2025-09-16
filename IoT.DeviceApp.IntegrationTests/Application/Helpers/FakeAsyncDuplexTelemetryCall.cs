using Grpc.Core;
using IoT.Contracts;
using System.Threading.Channels;

namespace IoT.DeviceApp.IntegrationTests.Application.Helpers;

public partial class FakeAsyncDuplexTelemetryCall
{
    public readonly Channel<Telemetry> Requests = System.Threading.Channels.Channel.CreateUnbounded<Telemetry>();
    public readonly Channel<TelemetryAck> Responses = System.Threading.Channels.Channel.CreateUnbounded<TelemetryAck>();

    public AsyncDuplexStreamingCall<Telemetry, TelemetryAck> CreateCall()
    {
        var writer = new FakeClientStreamWriter(Requests);
        var reader = new FakeAsyncStreamReader(Responses.Reader);

        return new AsyncDuplexStreamingCall<Telemetry, TelemetryAck>(
            writer,
            reader,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { }
        );
    }
}