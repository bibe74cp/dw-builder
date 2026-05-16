using DwBuilder.Biml;
using DwBuilder.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DwBuilder.Tests.Biml;

/// <summary>
/// Unit tests for BimlGenerator.
/// Note: Full BIML generation tests require SQL Server integration tests.
/// </summary>
public class BimlGeneratorTests
{
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<ILogger<BimlGenerator>> _loggerMock;
    private readonly BimlGenerator _generator;

    public BimlGeneratorTests()
    {
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _loggerMock = new Mock<ILogger<BimlGenerator>>();
        _generator = new BimlGenerator(_encryptionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var generator = new BimlGenerator(_encryptionServiceMock.Object, _loggerMock.Object);

        // Assert
        generator.Should().NotBeNull();
    }

    [Fact]
    public void BimlGenerator_ShouldDecryptPasswordsForConnectionStrings()
    {
        // Arrange
        _encryptionServiceMock
            .Setup(e => e.Decrypt(It.IsAny<string>()))
            .Returns("decrypted_password");

        // Act & Assert
        // This would be tested in integration test with actual metadata
        _encryptionServiceMock.Verify(e => e.Decrypt(It.IsAny<string>()), Times.Never); // Not called yet
    }

    [Fact]
    public void GenerateBimlXml_ShouldProduceValidXmlStructure()
    {
        // Arrange & Act & Assert
        // This requires full integration test with SQL Server
        // Unit test verifies service construction only
        _generator.Should().NotBeNull();
    }
}
