using FakeItEasy;
using FluentAssertions;
using IoT.Contracts;
using IoT.DeviceApp.Application;
using IoT.DeviceApp.Application.Services;
using IoT.DeviceApp.IntegrationTests.Application.Helpers;
using IoT.Shared.Helper;
using IoT.Shared.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoT.DeviceApp.IntegrationTests.Application.Services;

public class DeviceWorkerIntegrationTests
{
    [Fact]
    public async Task DeviceWorker_ShouldProcessCommands_AndSendTelemetry_Deterministically()
    {
        // Arrange
        var fakeService = new FakeDeviceGatewayService();
        var fakeClient = new FakeDeviceGatewayClient(fakeService);

        var settings = CreateDeviceAppSettings();

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
                                  .CreateLogger<DeviceWorker>();

        var fakeCertLoader = A.Fake<ICertLoader>();
        A.CallTo(() => fakeCertLoader.LoadDeviceCert("device-001"))
            .Returns(TestCertificateHelper.CreateTestCertificate("device-001"));

        var worker = new DeviceWorker(logger, settings, fakeCertLoader, fakeClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Signals to complete the test
        var commandsProcessedTask = TestWaitHelper.WaitUntilAsync(
            () => fakeClient.ProcessedCommands.Count >= 2, cts.Token);

        var telemetrySentTask = TestWaitHelper.WaitUntilAsync(
            () => fakeClient.SentTelemetry.Count >= 2, cts.Token);

        // Enqueue commands
        fakeService.EnqueueCommand(new DeviceCommand { Name = "cmd1", PayloadJson = "{}" });
        fakeService.EnqueueCommand(new DeviceCommand { Name = "cmd2", PayloadJson = "{}" });

        // Enqueue telemetry
        fakeService.EnqueueTelemetry(new Telemetry { Type = "temperature", PayloadJson = "22" });
        fakeService.EnqueueTelemetry(new Telemetry { Type = "temperature", PayloadJson = "23" });

        // Act
        await worker.StartAsync(cts.Token);

        // Wait deterministically for both telemetry and commands
        await Task.WhenAll(commandsProcessedTask, telemetrySentTask);

        // Assert commands
        fakeClient.ProcessedCommands.Select(c => c.Name).Should().Contain(["cmd1", "cmd2"]);

        // Assert telemetry
        fakeClient.SentTelemetry.Should().HaveCount(2);
        fakeClient.SentTelemetry.All(t => t.Type == "temperature").Should().BeTrue();

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DeviceWorker_ShouldSendTelemetry_ReceiveAcks_AndProcessCommands_Deterministically()
    {
        // Arrange
        var fakeService = new FakeDeviceGatewayService();
        var fakeClient = new FakeDeviceGatewayClient(fakeService);
        IOptions<DeviceAppSettings> settings = CreateDeviceAppSettings();

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
                                  .CreateLogger<DeviceWorker>();

        var fakeCertLoader = A.Fake<ICertLoader>();
        A.CallTo(() => fakeCertLoader.LoadDeviceCert("device-001"))
            .Returns(TestCertificateHelper.CreateTestCertificate("device-001"));

        var worker = new DeviceWorker(logger, settings, fakeCertLoader, fakeClient);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Signals to complete the test
        var commandsProcessedTask = TestWaitHelper.WaitUntilAsync(
            () => fakeClient.ProcessedCommands.Count >= 1, cts.Token, 100);

        var telemetrySentTask = TestWaitHelper.WaitUntilAsync(
            () => fakeClient.SentTelemetry.Count >= 1, cts.Token, 100);

        var ackReceivedTask = TestWaitHelper.WaitUntilAsync(
            () => fakeClient.ReceivedAcks.Count >= 1, cts.Token, 100);

        // Enqueue a command
        fakeService.EnqueueCommand(new DeviceCommand { Name = "reboot", PayloadJson = "{}" });

        // Act
        await worker.StartAsync(cts.Token);

        // Wait deterministically for telemetry, ack, and command
        await Task.WhenAll(commandsProcessedTask, telemetrySentTask);//, ackReceivedTask);

        // Assert command processed
        fakeClient.ProcessedCommands.Should().ContainSingle(c => c.Name == "reboot");

        // Assert telemetry was sent
        fakeClient.SentTelemetry.Should().NotBeEmpty();
        fakeClient.SentTelemetry.All(t => t.Type == "temperature").Should().BeTrue();


        // Assert ack was received
        // Assert ack was received
        fakeClient.ReceivedAcks.Should().NotBeEmpty();
        fakeClient.ReceivedAcks.All(a => a.Success).Should().BeTrue();

        await worker.StopAsync(CancellationToken.None);
    }

    private static IOptions<DeviceAppSettings> CreateDeviceAppSettings()
    {
        return Options.Create(new DeviceAppSettings
        {
            DeviceId = "device-001",
            TelemetryType = "temperature",
            TelemetryInterval = TimeSpan.FromMilliseconds(200) // fast for test
        });
    }
}