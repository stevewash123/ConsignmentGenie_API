using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class ShopperAuthServiceTests : IDisposable
{
    private readonly ConsignmentGenieContext _context;
    private readonly ShopperAuthService _shopperAuthService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Organization _testOrganization;

    public ShopperAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ConsignmentGenieContext(options);

        // Mock configuration
        _mockConfiguration = new Mock<IConfiguration>();
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("ConsignmentGenie_Test_Secret_Key_32_Characters_Long!");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        _shopperAuthService = new ShopperAuthService(_context, _mockConfiguration.Object);

        // Create test organization
        _testOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Consignment Shop",
            Slug = "test-shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(_testOrganization);
        _context.SaveChanges();
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsSuccessfulResult()
    {
        // Arrange
        var request = new ShopperRegisterRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "password123",
            Phone = "555-1234",
            EmailNotifications = true
        };

        // Act
        var result = await _shopperAuthService.RegisterAsync(request, _testOrganization.Slug);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.Profile);
        Assert.Equal("John Doe", result.Profile.FullName);
        Assert.Equal("john@example.com", result.Profile.Email);
        Assert.True(result.Profile.EmailNotifications);

        // Verify user was created
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        Assert.NotNull(user);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.Equal(ApprovalStatus.Approved, user.ApprovalStatus);

        // Verify shopper was created
        var shopper = await _context.Shoppers.FirstOrDefaultAsync(s => s.Email == request.Email);
        Assert.NotNull(shopper);
        Assert.Equal(_testOrganization.Id, shopper.OrganizationId);
        Assert.Equal(user.Id, shopper.UserId);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidStoreSlug_ReturnsFailure()
    {
        // Arrange
        var request = new ShopperRegisterRequest
        {
            FullName = "John Doe",
            Email = "john@example.com",
            Password = "password123"
        };

        // Act
        var result = await _shopperAuthService.RegisterAsync(request, "invalid-slug");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Store not found.", result.ErrorMessage);
        Assert.Null(result.Token);
        Assert.Null(result.Profile);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id,
            ApprovalStatus = ApprovalStatus.Approved
        };
        _context.Users.Add(existingUser);

        var existingShopper = new Shopper
        {
            OrganizationId = _testOrganization.Id,
            UserId = existingUser.Id,
            FullName = "Existing User",
            Email = "existing@example.com"
        };
        _context.Shoppers.Add(existingShopper);
        await _context.SaveChangesAsync();

        var request = new ShopperRegisterRequest
        {
            FullName = "John Doe",
            Email = "existing@example.com",
            Password = "password123"
        };

        // Act
        var result = await _shopperAuthService.RegisterAsync(request, _testOrganization.Slug);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("An account with this email already exists for this store.", result.ErrorMessage);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessfulResult()
    {
        // Arrange
        var user = new User
        {
            Email = "shopper@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id,
            ApprovalStatus = ApprovalStatus.Approved
        };
        _context.Users.Add(user);

        var shopper = new Shopper
        {
            OrganizationId = _testOrganization.Id,
            UserId = user.Id,
            FullName = "Test Shopper",
            Email = "shopper@example.com",
            EmailNotifications = true
        };
        _context.Shoppers.Add(shopper);
        await _context.SaveChangesAsync();

        var request = new ShopperLoginRequest
        {
            Email = "shopper@example.com",
            Password = "password123",
            RememberMe = false
        };

        // Act
        var result = await _shopperAuthService.LoginAsync(request, _testOrganization.Slug);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.Profile);
        Assert.Equal("Test Shopper", result.Profile.FullName);
        Assert.Equal("shopper@example.com", result.Profile.Email);

        // Verify last login was updated
        var updatedShopper = await _context.Shoppers.FindAsync(shopper.Id);
        Assert.NotNull(updatedShopper?.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var request = new ShopperLoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        var result = await _shopperAuthService.LoginAsync(request, _testOrganization.Slug);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "shopper@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id
        };
        _context.Users.Add(user);

        var shopper = new Shopper
        {
            OrganizationId = _testOrganization.Id,
            UserId = user.Id,
            FullName = "Test Shopper",
            Email = "shopper@example.com"
        };
        _context.Shoppers.Add(shopper);
        await _context.SaveChangesAsync();

        var request = new ShopperLoginRequest
        {
            Email = "shopper@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _shopperAuthService.LoginAsync(request, _testOrganization.Slug);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithRememberMe_ReturnsLongerExpiry()
    {
        // Arrange
        var user = new User
        {
            Email = "shopper@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id
        };
        _context.Users.Add(user);

        var shopper = new Shopper
        {
            OrganizationId = _testOrganization.Id,
            UserId = user.Id,
            FullName = "Test Shopper",
            Email = "shopper@example.com"
        };
        _context.Shoppers.Add(shopper);
        await _context.SaveChangesAsync();

        var request = new ShopperLoginRequest
        {
            Email = "shopper@example.com",
            Password = "password123",
            RememberMe = true
        };

        // Act
        var result = await _shopperAuthService.LoginAsync(request, _testOrganization.Slug);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.ExpiresAt);
        // Token should expire in about 30 days (allow some tolerance)
        var expectedExpiry = DateTime.UtcNow.AddDays(30);
        Assert.True(Math.Abs((result.ExpiresAt.Value - expectedExpiry).TotalHours) < 1);
    }

    #endregion

    #region CreateGuestSessionAsync Tests

    [Fact]
    public async Task CreateGuestSessionAsync_WithValidRequest_ReturnsGuestSession()
    {
        // Arrange
        var request = new GuestSessionRequest
        {
            Email = "guest@example.com",
            FullName = "Guest User",
            Phone = "555-9999"
        };

        // Act
        var result = await _shopperAuthService.CreateGuestSessionAsync(request, _testOrganization.Slug);

        // Assert
        Assert.NotNull(result.SessionToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        // Verify guest checkout was created
        var guestCheckout = await _context.GuestCheckouts
            .FirstOrDefaultAsync(g => g.SessionToken == result.SessionToken);
        Assert.NotNull(guestCheckout);
        Assert.Equal(_testOrganization.Id, guestCheckout.OrganizationId);
        Assert.Equal("guest@example.com", guestCheckout.Email);
        Assert.Equal("Guest User", guestCheckout.FullName);
    }

    [Fact]
    public async Task CreateGuestSessionAsync_WithInvalidStoreSlug_ThrowsException()
    {
        // Arrange
        var request = new GuestSessionRequest
        {
            Email = "guest@example.com",
            FullName = "Guest User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _shopperAuthService.CreateGuestSessionAsync(request, "invalid-slug"));
    }

    #endregion

    #region Profile Management Tests

    [Fact]
    public async Task GetShopperProfileAsync_WithValidIds_ReturnsProfile()
    {
        // Arrange
        var user = new User
        {
            Email = "shopper@example.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id
        };
        _context.Users.Add(user);

        var shopper = new Shopper
        {
            OrganizationId = _testOrganization.Id,
            UserId = user.Id,
            FullName = "Test Shopper",
            Email = "shopper@example.com",
            Phone = "555-1234",
            EmailNotifications = true,
            ShippingAddress1 = "123 Main St",
            ShippingCity = "Test City",
            ShippingState = "TS",
            ShippingZip = "12345"
        };
        _context.Shoppers.Add(shopper);
        await _context.SaveChangesAsync();

        // Act
        var result = await _shopperAuthService.GetShopperProfileAsync(user.Id, _testOrganization.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Shopper", result.FullName);
        Assert.Equal("shopper@example.com", result.Email);
        Assert.Equal("555-1234", result.Phone);
        Assert.True(result.EmailNotifications);
        Assert.NotNull(result.ShippingAddress);
        Assert.Equal("123 Main St", result.ShippingAddress.Address1);
        Assert.Equal("Test City", result.ShippingAddress.City);
    }

    [Fact]
    public async Task UpdateShopperProfileAsync_WithValidData_UpdatesProfile()
    {
        // Arrange
        var user = new User
        {
            Email = "shopper@example.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id
        };
        _context.Users.Add(user);

        var shopper = new Shopper
        {
            OrganizationId = _testOrganization.Id,
            UserId = user.Id,
            FullName = "Old Name",
            Email = "shopper@example.com"
        };
        _context.Shoppers.Add(shopper);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateShopperProfileRequest
        {
            FullName = "New Name",
            Phone = "555-9999",
            EmailNotifications = false,
            ShippingAddress = new AddressDto
            {
                Address1 = "456 Oak St",
                City = "New City",
                State = "NC",
                Zip = "54321"
            }
        };

        // Act
        var result = await _shopperAuthService.UpdateShopperProfileAsync(user.Id, _testOrganization.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.FullName);
        Assert.Equal("555-9999", result.Phone);
        Assert.False(result.EmailNotifications);
        Assert.Equal("456 Oak St", result.ShippingAddress?.Address1);
        Assert.Equal("New City", result.ShippingAddress?.City);

        // Verify database was updated
        var updatedShopper = await _context.Shoppers.FindAsync(shopper.Id);
        Assert.Equal("New Name", updatedShopper?.FullName);
        Assert.Equal("456 Oak St", updatedShopper?.ShippingAddress1);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Email = "shopper@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123"
        };

        // Act
        var result = await _shopperAuthService.ChangePasswordAsync(user.Id, request);

        // Assert
        Assert.True(result);

        // Verify password was changed
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword123", updatedUser?.PasswordHash));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Email = "shopper@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            Role = UserRole.Customer,
            OrganizationId = _testOrganization.Id
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "wrongpassword",
            NewPassword = "newpassword123"
        };

        // Act
        var result = await _shopperAuthService.ChangePasswordAsync(user.Id, request);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region JWT Token Tests

    [Fact]
    public void GenerateShopperJwtToken_WithValidParameters_ReturnsValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var shopperId = Guid.NewGuid();
        var email = "shopper@example.com";
        var organizationId = _testOrganization.Id;
        var storeSlug = "test-shop";

        // Act
        var token = _shopperAuthService.GenerateShopperJwtToken(userId, shopperId, email, organizationId, storeSlug);

        // Assert
        Assert.NotNull(token);
        Assert.True(token.Length > 0);

        // Verify token contains expected claims (basic validation)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        Assert.Contains(jsonToken.Claims, c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        Assert.Contains(jsonToken.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == email);
        Assert.Contains(jsonToken.Claims, c => c.Type == "ShopperId" && c.Value == shopperId.ToString());
        Assert.Contains(jsonToken.Claims, c => c.Type == "OrganizationId" && c.Value == organizationId.ToString());
        Assert.Contains(jsonToken.Claims, c => c.Type == "StoreSlug" && c.Value == storeSlug);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}