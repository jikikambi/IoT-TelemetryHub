using IoT.Shared;
using IoT.Telemetry.Persistence;
using IoT.TelemetryIngestor;
using System.Text.Json;

var builder = Host.CreateDefaultBuilder(args)    
    .ConfigureServices((ctx, services) =>
    {
        services.AddHostedService<TelemetryConsumerService>()
        .AddRabbitMQ(ctx.Configuration)
        .AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        })
        .AddPersistenceServices(ctx.Configuration);
    })
    .ConfigureLogging(l => l.AddConsole());
await builder.RunConsoleAsync();