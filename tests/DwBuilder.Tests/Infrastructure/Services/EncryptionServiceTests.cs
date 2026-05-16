using DwBuilder.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DwBuilder.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for EncryptionService.
/// </summary>
public class EncryptionServiceTests
{
    private const string ValidBase64Key = "dGhpc2lzYTMyYnl0ZWtleWZvcnRlc3Rpbmc="; // 32-byte key in base64

    private IConfiguration CreateMockConfiguration(string? encryptionKey)
    {
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["Encryption:Key"]).Returns(encryptionKey);
        return configurationMock.Object;
    }

    [Fact]
    public void Constructor_WithValidKey_ShouldInitializeSuccessfully()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);

        // Act
        var service = new EncryptionService(configuration);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = CreateMockConfiguration(null);

        // Act
        var act = () => new EncryptionService(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encryption key is not configured*");
    }

    [Fact]
    public void Constructor_WithEmptyKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = CreateMockConfiguration(string.Empty);

        // Act
        var act = () => new EncryptionService(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encryption key is not configured*");
    }

    [Fact]
    public void Constructor_WithInvalidBase64Key_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = CreateMockConfiguration("not-valid-base64!");

        // Act
        var act = () => new EncryptionService(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a valid base64 string*");
    }

    [Fact]
    public void Constructor_WithWrongKeySizeKey_ShouldThrowInvalidOperationException()
    {
        // Arrange (16-byte key instead of 32-byte)
        var configuration = CreateMockConfiguration(Convert.ToBase64String(new byte[16]));

        // Act
        var act = () => new EncryptionService(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must be exactly 32 bytes*");
    }

    [Fact]
    public void Encrypt_WithValidPlainText_ShouldReturnEncryptedString()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);
        var plainText = "MySecretPassword123!";

        // Act
        var encrypted = service.Encrypt(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().Contain(":");
        encrypted.Should().NotBe(plainText);
    }

    [Fact]
    public void Encrypt_WithNullPlainText_ShouldReturnEmptyString()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);

        // Act
        var encrypted = service.Encrypt(null!);

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_WithEmptyPlainText_ShouldReturnEmptyString()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);

        // Act
        var encrypted = service.Encrypt(string.Empty);

        // Assert
        encrypted.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WithValidEncryptedText_ShouldReturnOriginalPlainText()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);
        var plainText = "MySecretPassword123!";
        var encrypted = service.Encrypt(plainText);

        // Act
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_WithNullEncryptedText_ShouldReturnEmptyString()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);

        // Act
        var decrypted = service.Decrypt(null!);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WithEmptyEncryptedText_ShouldReturnEmptyString()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);

        // Act
        var decrypted = service.Decrypt(string.Empty);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_ShouldPreserveOriginalText()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);
        var plainText = "ConnectionPassword!@#$%^&*()_+{}|:<>?[];',./";

        // Act
        var encrypted = service.Encrypt(plainText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_MultipleCalls_ShouldProduceDifferentCipherTexts()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);
        var plainText = "MySecretPassword";

        // Act
        var encrypted1 = service.Encrypt(plainText);
        var encrypted2 = service.Encrypt(plainText);

        // Assert
        encrypted1.Should().NotBe(encrypted2, "IV should be randomized");
        service.Decrypt(encrypted1).Should().Be(plainText);
        service.Decrypt(encrypted2).Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_ShouldProduceCorrectFormat()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);
        var plainText = "TestPassword";

        // Act
        var encrypted = service.Encrypt(plainText);
        var parts = encrypted.Split(':');

        // Assert
        parts.Should().HaveCount(2, "format should be 'IV:CipherText'");
        parts[0].Should().NotBeNullOrEmpty("IV part should not be empty");
        parts[1].Should().NotBeNullOrEmpty("CipherText part should not be empty");
    }

    [Fact]
    public void Decrypt_WithInvalidFormat_ShouldThrowException()
    {
        // Arrange
        var configuration = CreateMockConfiguration(ValidBase64Key);
        var service = new EncryptionService(configuration);
        var invalidEncrypted = "invalid-format-without-colon";

        // Act
        var act = () => service.Decrypt(invalidEncrypted);

        // Assert
        act.Should().Throw<Exception>();
    }
}
