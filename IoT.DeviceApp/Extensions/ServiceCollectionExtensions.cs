using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using IoT.Contracts;
using IoT.DeviceApp.Application;
using IoT.DeviceApp.Application.Services;
using IoT.Shared.Helper;
using Microsoft.Extensions.Options;

namespace IoT.DeviceApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure DeviceAppSettings
        services.Configure<DeviceAppSettings>(configuration.GetSection("DeviceAppSettings"));

        // JWT token storage for interceptor
        string jwtToken = "";

        // Register the raw gRPC client with JWT interceptor
        services.AddSingleton(sp =>
        {
            var deviceAppSettings = sp.GetRequiredService<IOptions<DeviceAppSettings>>().Value;

            // SSL handler
            var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.Equals("Development", System.StringComparison.OrdinalIgnoreCase) == true;
            var httpHandler = isDevelopment
                ? new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }
                : new HttpClientHandler();

            var channel = GrpcChannel.ForAddress(deviceAppSettings.GatewayUri, new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });

            var callInvoker = channel.Intercept(new JwtInterceptor(() => jwtToken));

            return new DeviceGateway.DeviceGatewayClient(callInvoker);
        });

        // Wrap gRPC client in interface for DI / unit testing
        services.AddSingleton<IDeviceGatewayClient>(sp =>
        {
            var client = sp.GetRequiredService<DeviceGateway.DeviceGatewayClient>();
            return new DeviceGatewayClientWrapper(client, () => jwtToken);
        });

        // Register CertLoader
        services.AddSingleton<ICertLoader>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            return new CertLoader(config);
        });
    }
}
