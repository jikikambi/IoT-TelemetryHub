using IoT.Gateway.Application.Services;

namespace IoT.Gateway.Extensions;

public static class GatewayEndpoints
{
    public static void RegisterEndpoints(this WebApplication webApp)
    {
        webApp.MapGet("/", () => "gRPC Device Gateway running");

        webApp.MapGrpcService<DeviceGatewayService>();
    }
}
