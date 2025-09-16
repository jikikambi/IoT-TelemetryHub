using IoT.DeviceApi.Application.Mappers;
using IoT.Shared;
using IoT.Telemetry.Persistence;
using Microsoft.OpenApi.Models;

namespace IoT.DeviceApi;

public static class ServiceCollectionExtensions
{
    public static WebApplication RegisterServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "IoT Constellation Service API",
                Version = "v1",
                Description = "Handles IoT Constellations",
            });
        });

        builder.Services.AddRabbitMQ(builder.Configuration);
        builder.Services.AddPersistenceServices(builder.Configuration);
        builder.Services.RegisterMappers();

        return builder.Build();
    }

    public static WebApplication SetUpMiddleWare(this WebApplication webApp)
    {
        if (webApp.Environment.IsDevelopment())
        {
            webApp.UseSwagger();
            webApp.UseSwaggerUI();
        }

        // Check if running in a container
        var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        if (!inContainer)
        {
            webApp.UseHttpsRedirection();
        }

        webApp.RegisterEndpoints();

        return webApp;
    }

    public static void RegisterMappers(this IServiceCollection services)
    {
        services.AddScoped<ICommandRequestMapper,  CommandRequestMapper>();
    }
}
