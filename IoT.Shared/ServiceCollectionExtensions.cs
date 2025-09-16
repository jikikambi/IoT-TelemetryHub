using IoT.Shared.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoT.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCertificateLoader(this IServiceCollection services)
    {
       return services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<CertLoader>>();
            return new CertLoader(config, Path.Combine(AppContext.BaseDirectory, "Certs"));
        });
    }
}