using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;

    public AuthServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        // Setup configuration
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("ConsignmentGenie_Super_Secret_Key_2024_32_Characters_Long!");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");

        _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        _authService = new AuthService(_context, _mockConfiguration.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            OrganizationName = "Test Organization",
            VerticalType = VerticalType.Consignment
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(UserRole.Owner, result.Role);
        Assert.Equal("Test Organization", result.OrganizationName);
        Assert.NotEmpty(result.Token);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsNull()
    {
        // Arrange
        var organization = new Organization
        {
            Name = "Existing Org",
            VerticalType = VerticalType.Consignment
        };
        _context.Organizations.Add(organization);

        var existingUser = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = UserRole.Owner,
            OrganizationId = organization.Id
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            OrganizationName = "Test Organization"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var organization = new Organization
        {
            Name = "Test Org",
            VerticalType = VerticalType.Consignment
        };
        _context.Organizations.Add(organization);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Owner,
            OrganizationId = organization.Id
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(UserRole.Owner, result.Role);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateJwtToken_ValidInput_ReturnsToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var role = "ShopOwner";
        var organizationId = Guid.NewGuid();

        // Act
        var token = _authService.GenerateJwtToken(userId, email, role, organizationId);

        // Assert
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT should have dots separating header, payload, signature
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}