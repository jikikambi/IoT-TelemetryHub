using AutoFixture;
using FluentAssertions;
using IoT.Shared.Helper;
using Microsoft.Extensions.Configuration;

namespace IoT.Shared.UnitTests.Helper;

public class CertLoaderTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Constructor_ShouldThrow_WhenBasePasswordNotConfigured()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act
        var act = () => new CertLoader(config, Path.Combine(Path.GetTempPath(), "fake"));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Base password not configured in User Secrets or environment.");
    }

    [Fact]
    public void LoadGatewayCert_ShouldThrow_WhenFileMissing()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string>("CertPasswords:BasePassword", "passW0rd")])
            .Build();

        var loader = new CertLoader(config, tempDir);

        // Act
        var act = () => loader.LoadGatewayCert();

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*gateway.pfx*");
    }

    [Fact]
    public void LoadDeviceCert_ShouldThrow_WhenDeviceFileMissing()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var deviceDir = Path.Combine(tempDir, "Devices", "device-001");
        Directory.CreateDirectory(deviceDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string>("CertPasswords:BasePassword", "passW0rd")])
            .Build();

        var loader = new CertLoader(config, tempDir);

        // Act
        var act = () => loader.LoadDeviceCert("device-001");

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*device-001.pfx*");
    }

    [Theory]
    [InlineData("bad-device-name")] // not even close
    [InlineData("device-12")]       // too short
    [InlineData("device-1234")]     // too long
    public void LoadDeviceCert_ShouldThrow_WhenDeviceNameInvalid(string invalidDeviceName)
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string>("CertPasswords:BasePassword", "passW0rd")])
            .Build();

        var loader = new CertLoader(config, tempDir);

        // Act
        var act = () => loader.LoadDeviceCert(invalidDeviceName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*device-###*");
    }

    [Fact]
    public void LoadRootCA_ShouldThrow_WhenFileMissing()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string>("CertPasswords:BasePassword", "passW0rd")])
            .Build();

        var loader = new CertLoader(config, tempDir);

        // Act
        var act = () => loader.LoadRootCA();

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*ca.crt*");
    }    
}