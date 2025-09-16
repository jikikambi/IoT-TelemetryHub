using IoT.Telemetry.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IoT.Telemetry.Persistence;

public static class ServiceCollectionExtensions
{
    public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        // Pick connection string dynamically
        var sqlConnection = env switch
        {
            "Development" => configuration.GetConnectionString("TelemetryDb_Dev")!,
            _ => configuration.GetConnectionString("TelemetryDb")!
        };

        services.AddDbContext<TelemetryDbContext>(options =>
            options.UseSqlServer(sqlConnection, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("IoT.Telemetry.Persistence");
                sqlOptions.EnableRetryOnFailure();
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
    }
}