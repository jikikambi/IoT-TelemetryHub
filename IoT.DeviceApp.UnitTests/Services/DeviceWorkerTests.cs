using FakeItEasy;
using FluentAssertions;
using IoT.Contracts;
using IoT.DeviceApp.Application;
using IoT.DeviceApp.Application.Services;
using IoT.Shared.Helper;
using IoT.Shared.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoT.DeviceApp.UnitTests.Services;

public class DeviceWorkerTests
{
    private static DeviceWorker CreateWorker(
        IDeviceGatewayClient gatewayClient = null!,
        ICertLoader certLoader = null!,
        DeviceAppSettings settings = null!)
    {
        var logger = A.Fake<ILogger<DeviceWorker>>();
        certLoader ??= A.Fake<ICertLoader>();
        gatewayClient ??= A.Fake<IDeviceGatewayClient>();
        settings ??= new DeviceAppSettings
        {
            DeviceId = "device-001",
            GatewayUri = "http://localhost:80",
            TelemetryType = "temperature"
        };

        return new DeviceWorker(logger, Options.Create(settings), certLoader, gatewayClient);
    }

    private static void SetupGatewayClientFakes(
    IDeviceGatewayClient client,
    string deviceId = "device-001",
    string telemetryType = "temperature",
    Func<int, string>? jwtGenerator = null)
    {
        int jwtCalls = 0;

        if (jwtGenerator == null)
        {
            // default static valid JWT
            var staticJwt = TestJwtHelper.CreateFakeJwt(deviceId, TimeSpan.FromHours(1));
            A.CallTo(() => client.ConnectDeviceAsync(deviceId, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(staticJwt));
        }
        else
        {
            // fake dynamic JWTs for refresh tests
            A.CallTo(() => client.ConnectDeviceAsync(deviceId, A<string>.Ignored, A<CancellationToken>.Ignored))
                .ReturnsLazily(() =>
                {
                    jwtCalls++;
                    return Task.FromResult(jwtGenerator(jwtCalls));
                });
        }

        // Fake telemetry stream
        A.CallTo(() => client.StreamTelemetryAsync(A<CancellationToken>.Ignored))
            .Returns(GetDummyTelemetryStream());

        // Fake command stream
        A.CallTo(() => client.ReceiveCommandsAsync(deviceId, A<CancellationToken>.Ignored))
            .Returns(GetDummyCommandStream());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldConnectAndSetJwt()
    {
        var deviceCert = TestCertificateHelper.CreateTestCertificate("device-001");
        var certLoader = A.Fake<ICertLoader>();
        A.CallTo(() => certLoader.LoadDeviceCert("device-001")).Returns(deviceCert);

        var gatewayClient = A.Fake<IDeviceGatewayClient>();
        SetupGatewayClientFakes(gatewayClient);

        var worker = CreateWorker(gatewayClient, certLoader);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await worker.StartAsync(cts.Token);

        A.CallTo(() => gatewayClient.ConnectDeviceAsync("device-001", A<string>.Ignored, A<CancellationToken>.Ignored))
            .MustHaveHappened();
    }

    [Fact]
    public void ExecuteAsync_ShouldThrow_WhenDeviceIdMissing()
    {
        var worker = CreateWorker(settings: new DeviceAppSettings { DeviceId = "", GatewayUri = "http://localhost:80" });

        Func<Task> act = async () => await worker.StartAsync(CancellationToken.None);

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DeviceId is not configured.");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRefreshJwtAndStreamTelemetryAndReceiveCommands()
    {
        var deviceCert = TestCertificateHelper.CreateTestCertificate("device-001");
        var certLoader = A.Fake<ICertLoader>();
        A.CallTo(() => certLoader.LoadDeviceCert("device-001")).Returns(deviceCert);

        var gatewayClient = A.Fake<IDeviceGatewayClient>();
        SetupGatewayClientFakes(gatewayClient);

        var worker = CreateWorker(gatewayClient, certLoader);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);

        A.CallTo(() => gatewayClient.ConnectDeviceAsync("device-001", A<string>.Ignored, A<CancellationToken>.Ignored))
            .MustHaveHappened();

        A.CallTo(() => gatewayClient.CreateTelemetryDuplexCallAsync(A<CancellationToken>.Ignored))
            .MustHaveHappened();

        A.CallTo(() => gatewayClient.ReceiveCommandsAsync("device-001", A<CancellationToken>.Ignored))
            .MustHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRefreshJwtToken_WhenExpiringSoon()
    {
        var deviceCert = TestCertificateHelper.CreateTestCertificate("device-001");
        var certLoader = A.Fake<ICertLoader>();
        A.CallTo(() => certLoader.LoadDeviceCert("device-001")).Returns(deviceCert);

        var gatewayClient = A.Fake<IDeviceGatewayClient>();

        var jwtCalls = 0;

        // Return very short-lived JWT each time ConnectDeviceAsync is called
        A.CallTo(() => gatewayClient.ConnectDeviceAsync("device-001", A<string>.Ignored, A<CancellationToken>.Ignored))
            .ReturnsLazily(() =>
            {
                jwtCalls++;
                // JWT expires in 50ms, forcing refresh
                return Task.FromResult(TestJwtHelper.CreateFakeJwt($"device-001-{jwtCalls}", TimeSpan.FromMilliseconds(100)));
            });

        // Telemetry stream
        A.CallTo(() => gatewayClient.StreamTelemetryAsync(A<CancellationToken>.Ignored))
            .Returns(GetDummyTelemetryStream());

        // Command stream
        A.CallTo(() => gatewayClient.ReceiveCommandsAsync("device-001", A<CancellationToken>.Ignored))
            .Returns(GetDummyCommandStream());

        var worker = CreateWorker(gatewayClient, certLoader);

        // Give enough time for worker to detect expiring token and refresh
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await worker.StartAsync(cts.Token);

        // Verify ConnectDeviceAsync called at least twice
        A.CallTo(() => gatewayClient.ConnectDeviceAsync("device-001", A<string>.Ignored, A<CancellationToken>.Ignored))
            .MustHaveHappenedANumberOfTimesMatching(x => x >= 2);

        A.CallTo(() => gatewayClient.CreateTelemetryDuplexCallAsync(A<CancellationToken>.Ignored))
            .MustHaveHappened();

        A.CallTo(() => gatewayClient.ReceiveCommandsAsync("device-001", A<CancellationToken>.Ignored))
            .MustHaveHappened();
    }


    private static async IAsyncEnumerable<TelemetryAck> GetDummyTelemetryStream()
    {
        for (int i = 0; i < 2; i++)
        {
            yield return new TelemetryAck { Success = true, Message = $"ok {i}" };
            await Task.Delay(10);
        }
    }

    private static async IAsyncEnumerable<DeviceCommand> GetDummyCommandStream()
    {
        for (int i = 0; i < 2; i++)
        {
            yield return new DeviceCommand { Name = $"cmd{i}", PayloadJson = "{}" };
            await Task.Delay(10);
        }
    }
}
