using IoT.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

var app = builder
    .UseEnvironmentAppSettings()
    .ConfigureGatewayKestrel()
    .RegisterServices()
    .Build();    
    
app.SetUpMiddleWare();

await app.RunAsync();