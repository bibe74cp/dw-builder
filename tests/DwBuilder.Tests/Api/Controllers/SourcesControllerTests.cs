using DwBuilder.Api.Controllers;
using DwBuilder.Core.DTOs.Sources;
using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DwBuilder.Tests.Api.Controllers;

/// <summary>
/// Unit tests for SourcesController.
/// </summary>
public class SourcesControllerTests
{
    private readonly Mock<ISourceRepository> _sourceRepositoryMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<ISourceConnectionService> _sourceConnectionServiceMock;
    private readonly Mock<ILogger<SourcesController>> _loggerMock;
    private readonly SourcesController _controller;

    public SourcesControllerTests()
    {
        _sourceRepositoryMock = new Mock<ISourceRepository>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _sourceConnectionServiceMock = new Mock<ISourceConnectionService>();
        _loggerMock = new Mock<ILogger<SourcesController>>();
        
        _controller = new SourcesController(
            _sourceRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _sourceConnectionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllActiveSources()
    {
        // Arrange
        var sources = new List<Source>
        {
            new() { Id = 1, Name = "Source1", ServerName = "server1", DatabaseName = "db1", LandingSchema = "s1", IsActive = true },
            new() { Id = 2, Name = "Source2", ServerName = "server2", DatabaseName = "db2", LandingSchema = "s2", IsActive = true }
        };

        _sourceRepositoryMock
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSources = okResult.Value.Should().BeAssignableTo<IEnumerable<SourceDto>>().Subject;
        returnedSources.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnSource()
    {
        // Arrange
        var source = new Source
        {
            Id = 1,
            Name = "TestSource",
            ServerName = "localhost",
            DatabaseName = "TestDB",
            LandingSchema = "landing",
            IsActive = true
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<SourceDto>().Subject;
        dto.Name.Should().Be("TestSource");
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Source?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ShouldCreateSourceAndEncryptPassword()
    {
        // Arrange
        var request = new CreateSourceRequest
        {
            Name = "NewSource",
            ServerName = "server1",
            DatabaseName = "db1",
            LandingSchema = "landing",
            ConnectionUser = "user",
            ConnectionPassword = "PlainPassword123"
        };

        var encryptedPassword = "IV:EncryptedData";
        _encryptionServiceMock
            .Setup(e => e.Encrypt("PlainPassword123"))
            .Returns(encryptedPassword);

        _sourceRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Source>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Source s, CancellationToken ct) =>
            {
                s.Id = 1;
                return s;
            });

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        _encryptionServiceMock.Verify(e => e.Encrypt("PlainPassword123"), Times.Once);
        _sourceRepositoryMock.Verify(r => r.CreateAsync(
            It.Is<Source>(s => s.ConnectionPasswordEncrypted == encryptedPassword),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutPassword_ShouldNotEncrypt()
    {
        // Arrange
        var request = new CreateSourceRequest
        {
            Name = "NewSource",
            ServerName = "server1",
            DatabaseName = "db1",
            LandingSchema = "landing",
            ConnectionUser = null,
            ConnectionPassword = null
        };

        _sourceRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Source>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Source s, CancellationToken ct) =>
            {
                s.Id = 1;
                return s;
            });

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        _encryptionServiceMock.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Never);
        _sourceRepositoryMock.Verify(r => r.CreateAsync(
            It.Is<Source>(s => s.ConnectionPasswordEncrypted == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithValidId_ShouldUpdateSource()
    {
        // Arrange
        var existingSource = new Source
        {
            Id = 1,
            Name = "OldName",
            ServerName = "oldserver",
            DatabaseName = "olddb",
            LandingSchema = "old_landing",
            IsActive = true
        };

        var request = new UpdateSourceRequest
        {
            Name = "UpdatedName",
            ServerName = "newserver",
            DatabaseName = "newdb",
            LandingSchema = "new_landing"
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSource);

        _sourceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Source>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        _sourceRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Source>(s => s.Name == "UpdatedName" && s.ServerName == "newserver"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpdateSourceRequest
        {
            Name = "UpdatedName",
            ServerName = "server",
            DatabaseName = "db",
            LandingSchema = "landing"
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Source?)null);

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_WithValidId_ShouldSoftDeleteSource()
    {
        // Arrange
        var existingSource = new Source
        {
            Id = 1,
            Name = "ToDelete",
            ServerName = "server",
            DatabaseName = "db",
            LandingSchema = "landing",
            IsActive = true
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSource);

        _sourceRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Source>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _sourceRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Source>(s => s.IsActive == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
