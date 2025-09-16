using IoT.DeviceApp.Application.Services;
using IoT.DeviceApp.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.RegisterServices(builder.Configuration);
builder.Services.AddHostedService<DeviceWorker>();

var host = builder.Build();
host.Run();