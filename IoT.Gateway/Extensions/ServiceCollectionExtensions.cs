using IoT.Gateway.Application;
using IoT.Gateway.Application.Mappers;
using IoT.Shared;
using Microsoft.OpenApi.Models;

namespace IoT.Gateway.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the generic host to load appsettings.json and appsettings.{Environment}.json automatically.
        /// </summary>
        public static WebApplicationBuilder UseEnvironmentAppSettings(this WebApplicationBuilder builder)
        {
            var env = builder.Environment;

            builder.Configuration.Sources.Clear();
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Console.WriteLine($"Environment: {env.EnvironmentName}");

            return builder;
        }

        /// <summary>
        /// Register all common Gateway services for WebApplicationBuilder (gRPC, Swagger, RabbitMQ, etc.)
        /// </summary>
        public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddGrpc();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "IoT Gateway Service API",
                    Version = "v1",
                    Description = "gRPC Device Gateway",
                });
            });

            builder.Services
                .AddRabbitMQ(builder.Configuration)
                .RegisterCertificateLoader()
                .RegisterGatewaySettings(builder.Configuration)
                .RegisterMappers();

            return builder;
        }

        /// <summary>
        /// Register DI mappers
        /// </summary>
        public static IServiceCollection RegisterMappers(this IServiceCollection services)
        {
            services.AddScoped<IDeviceCommandMapper, DeviceCommandMapper>();
            return services;
        }

        /// <summary>
        /// Register JWT and DeviceTrustedRoots settings
        /// </summary>
        public static IServiceCollection RegisterGatewaySettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.Configure<DeviceTrustedRootsSettings>(configuration.GetSection("DeviceTrustedRoots"));
            return services;
        }
    }
}
