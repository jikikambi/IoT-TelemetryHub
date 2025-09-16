using IoT.Shared.Requests;

namespace IoT.DeviceApi.Application.Mappers;

public class CommandRequestMapper : ICommandRequestMapper
{
    public CommandMessage Map(string deviceId, CommandRequest request)
    {
        return new CommandMessage
        {
            CommandId = Guid.NewGuid().ToString(),
            DeviceId = deviceId,
            Name = request.Command,
            Payload = request.Payload,
            SentAt = DateTimeOffset.UtcNow
        };
    }
}
