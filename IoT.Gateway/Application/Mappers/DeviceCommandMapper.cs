using IoT.Contracts;
using IoT.Shared.Requests;
using System.Text.Json;

namespace IoT.Gateway.Application.Mappers;

public class DeviceCommandMapper : IDeviceCommandMapper
{
    public DeviceCommand Map(CommandMessage message, JsonSerializerOptions jsonOptions)
    {
        return new DeviceCommand
        {
            CommandId = message.CommandId,
            Name = message.Name,
            PayloadJson = JsonSerializer.Serialize(message.Payload, jsonOptions)
        };
    }
}