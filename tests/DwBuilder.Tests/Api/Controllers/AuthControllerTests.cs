using DwBuilder.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DwBuilder.Tests.Api.Controllers;

/// <summary>
/// Unit tests for AuthController.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var userStoreMock = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("ThisIsAVeryLongSecretKeyForTestingPurposesOnly12345678901234567890");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("60");
        
        _loggerMock = new Mock<ILogger<AuthController>>();
        
        _controller = new AuthController(_userManagerMock.Object, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var user = new IdentityUser { UserName = "testuser", Email = "test@example.com" };
        var request = new LoginRequest("testuser", "Password123!");

        _userManagerMock
            .Setup(um => um.FindByNameAsync("testuser"))
            .ReturnsAsync(user);
        
        _userManagerMock
            .Setup(um => um.CheckPasswordAsync(user, "Password123!"))
            .ReturnsAsync(true);
        
        _userManagerMock
            .Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().NotBeNull();
        
        var response = okResult.Value as LoginResponse;
        response.Should().NotBeNull();
        response!.Token.Should().NotBeNullOrEmpty();
        response.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("nonexistent", "Password123!");
        
        _userManagerMock
            .Setup(um => um.FindByNameAsync("nonexistent"))
            .ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = new IdentityUser { UserName = "testuser" };
        var request = new LoginRequest("testuser", "WrongPassword");

        _userManagerMock
            .Setup(um => um.FindByNameAsync("testuser"))
            .ReturnsAsync(user);
        
        _userManagerMock
            .Setup(um => um.CheckPasswordAsync(user, "WrongPassword"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@example.com", "Password123!");
        
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<IdentityUser>(), "Password123!"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        
        _userManagerMock.Verify(um => um.CreateAsync(
            It.Is<IdentityUser>(u => u.UserName == "newuser" && u.Email == "new@example.com"), 
            "Password123!"), Times.Once);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@example.com", "weak");
        
        var errors = new[]
        {
            new IdentityError { Description = "Password must be at least 8 characters" }
        };
        
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<IdentityUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("existinguser", "new@example.com", "Password123!");
        
        var errors = new[]
        {
            new IdentityError { Description = "Username 'existinguser' is already taken." }
        };
        
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}
