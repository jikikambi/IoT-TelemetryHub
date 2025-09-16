using IoT.DeviceApi;

var builder = WebApplication.CreateBuilder(args);

var app = builder
    .RegisterServices()
    .SetUpMiddleWare();

await app.RunAsync();