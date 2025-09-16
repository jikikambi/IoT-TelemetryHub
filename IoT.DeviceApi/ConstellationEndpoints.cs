using IoT.DeviceApi.Application.Mappers;
using IoT.Shared.Messaging;
using IoT.Shared.Requests;

namespace IoT.DeviceApi;

public static class ConstellationEndpoints
{
    public static void RegisterEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => "Device API running");
        app.MapPost("/device/{id}/commands", async (string id, CommandRequest request, IMessagePublisher bus, ICommandRequestMapper mapper) =>
        {
            var mappedCmd = mapper.Map(id, request); 

            var routingKey = $"device.{id}.set";
            await bus.PublishAsync("device.commands", routingKey, mappedCmd);
            return Results.Accepted($"/devices/{id}/commands/{mappedCmd.CommandId}", mappedCmd);
        });
    }
}
