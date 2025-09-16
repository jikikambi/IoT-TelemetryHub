using Grpc.Core;
using IoT.Contracts;
using IoT.DeviceApp.IntegrationTests.Application.Services;

namespace IoT.DeviceApp.IntegrationTests.Infrastructure;

public sealed class GrpcTestServer : IAsyncDisposable
{
    private readonly Server _server;
    private readonly FakeDeviceGatewayService _fakeService;

    public string Host { get; }
    public int Port { get; }
    public string Address => $"http://{Host}:{Port}";

    public FakeDeviceGatewayService FakeService => _fakeService;

    public GrpcTestServer(int port = 50051, string host = "localhost")
    {
        Host = host;
        Port = port;
        _fakeService = new FakeDeviceGatewayService();

        _server = new Server
        {
            Services = { DeviceGateway.BindService(_fakeService) },
            Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
        };
    }

    public Task StartAsync()
    {
        _server.Start();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _server.ShutdownAsync();
    }
}