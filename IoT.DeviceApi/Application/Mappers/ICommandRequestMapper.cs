using IoT.Shared.Requests;

namespace IoT.DeviceApi.Application.Mappers;

public interface ICommandRequestMapper
{
    CommandMessage Map(string deviceId, CommandRequest request);
}