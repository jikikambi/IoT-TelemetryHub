using IoT.Contracts;
using IoT.Shared.Requests;
using System.Text.Json;

namespace IoT.Gateway.Application.Mappers;

public interface IDeviceCommandMapper 
{
    DeviceCommand Map(CommandMessage message, JsonSerializerOptions jsonOptions);
}