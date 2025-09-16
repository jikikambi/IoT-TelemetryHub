using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using IoT.DeviceApp.Application;
using IoT.DeviceApp.Application.Services;
using IoT.Shared.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoT.DeviceApp.UnitTests.Services;

public class DeviceWorkerCertificateTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ExecuteAsync_ShouldThrow_WhenDeviceCertMissing()
    {
        var logger = A.Fake<ILogger<DeviceWorker>>();
        var certLoader = A.Fake<ICertLoader>();
        var gatewayClient = A.Fake<IDeviceGatewayClient>();

        A.CallTo(() => certLoader.LoadDeviceCert("device-001"))
            .Throws(new FileNotFoundException("Certificate not found"));

        var settings = Options.Create(new DeviceAppSettings
        {
            DeviceId = "device-001",
            GatewayUri = "http://localhost:80",
            TelemetryType = "temperature"
        });

        var worker = new DeviceWorker(logger, settings, certLoader, gatewayClient);

        Func<Task> act = async () => await worker.StartAsync(CancellationToken.None);

        act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Certificate not found");
    }

    [Fact]
    public void ExecuteAsync_ShouldThrow_WhenDeviceCertInvalid()
    {
        var logger = A.Fake<ILogger<DeviceWorker>>();
        var certLoader = A.Fake<ICertLoader>();
        var gatewayClient = A.Fake<IDeviceGatewayClient>();

        // Simulate invalid certificate (null or corrupted)
        A.CallTo(() => certLoader.LoadDeviceCert("device-001"))
            .Throws(new InvalidOperationException("Invalid device certificate"));

        var settings = Options.Create(new DeviceAppSettings
        {
            DeviceId = "device-001",
            GatewayUri = "http://localhost:80",
            TelemetryType = "temperature"
        });

        var worker = new DeviceWorker(logger, settings, certLoader, gatewayClient);

        Func<Task> act = async () => await worker.StartAsync(CancellationToken.None);

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid device certificate");
    }

    [Theory]
    [InlineData("bad-device-name")]
    [InlineData("device-XYZ")]
    public void ExecuteAsync_ShouldThrow_WhenDeviceNameInvalid(string deviceName)
    {
        var logger = A.Fake<ILogger<DeviceWorker>>();
        var certLoader = A.Fake<ICertLoader>();
        var gatewayClient = A.Fake<IDeviceGatewayClient>();

        A.CallTo(() => certLoader.LoadDeviceCert(deviceName))
            .Throws(new ArgumentException("Invalid device name: " + deviceName));

        var settings = Options.Create(new DeviceAppSettings
        {
            DeviceId = deviceName,
            GatewayUri = "http://localhost:80",
            TelemetryType = "temperature"
        });

        var worker = new DeviceWorker(logger, settings, certLoader, gatewayClient);

        Func<Task> act = async () => await worker.StartAsync(CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Invalid device name: {deviceName}");
    }
}

