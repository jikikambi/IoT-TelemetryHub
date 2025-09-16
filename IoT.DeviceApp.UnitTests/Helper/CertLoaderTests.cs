using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using IoT.Shared.Helper;
using Microsoft.Extensions.Configuration;

namespace IoT.DeviceApp.UnitTests.Helper;

public class CertLoaderTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void LoadRootCA_ShouldThrow_WhenMissing()
    {
        // Arrange
        var config = A.Fake<IConfiguration>();
        var certLoader = new CertLoader(config, rootCertPath: Path.GetTempPath());

        // Act
        Action act = () => certLoader.LoadRootCA();

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*CA certificate not found*");
    }

    [Fact]
    public void LoadGatewayCert_ShouldThrow_WhenMissing()
    {
        // Arrange
        var config = A.Fake<IConfiguration>();
        A.CallTo(() => config.GetSection("CertPasswords")["BasePassword"]).Returns("secret");
        var certLoader = new CertLoader(config, rootCertPath: Path.GetTempPath());

        // Act
        Action act = () => certLoader.LoadGatewayCert();

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Gateway certificate not found*");
    }

    [Fact]
    public void LoadDeviceCert_ShouldThrow_WhenInvalidName()
    {
        // Arrange
        var config = A.Fake<IConfiguration>();
        A.CallTo(() => config.GetSection("CertPasswords")["BasePassword"]).Returns("secret");
        var certLoader = new CertLoader(config, rootCertPath: Path.GetTempPath());

        // Act
        Action act = () => certLoader.LoadDeviceCert("bad-device");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid device name: bad-device*");
    }

    [Fact]
    public void LoadDeviceCert_ShouldThrow_WhenFileMissing()
    {
        // Arrange
        var config = A.Fake<IConfiguration>();
        A.CallTo(() => config.GetSection("CertPasswords")["BasePassword"]).Returns("secret");
        var certLoader = new CertLoader(config, rootCertPath: Path.GetTempPath());

        // Act
        Action act = () => certLoader.LoadDeviceCert("device-001");

        // Assert
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Device certificate not found*");
    }
}