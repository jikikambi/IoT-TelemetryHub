using Grpc.Core;
using IoT.Contracts;
using System.Threading.Channels;

namespace IoT.DeviceApp.IntegrationTests.Application.Helpers;

public partial class FakeAsyncDuplexTelemetryCall
{
    private class FakeAsyncStreamReader(ChannelReader<TelemetryAck> reader) : IAsyncStreamReader<TelemetryAck>
    {
        public TelemetryAck Current { get; private set; } = null!;
        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (await reader.WaitToReadAsync(cancellationToken))
            {
                if (reader.TryRead(out var item))
                {
                    Current = item;
                    return true;
                }
            }
            return false;
        }
    }
}
