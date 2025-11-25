using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class SeedingTests : IDisposable
{
    private readonly ConsignmentGenieContext _context;
    private readonly AuthService _authService;

    public SeedingTests()
    {
        // Create in-memory database with seeding
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ConsignmentGenieContext(options);

        // Ensure database is created with seeded data
        _context.Database.EnsureCreated();

        // Setup AuthService
        var mockConfiguration = new Mock<IConfiguration>();
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("ConsignmentGenie_Super_Secret_Key_2024_32_Characters_Long!");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        _authService = new AuthService(_context, mockConfiguration.Object);
    }

    [Fact]
    public async Task SeedData_CreatesOrganization()
    {
        // Act
        var organization = await _context.Organizations.FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(organization);
        Assert.Equal("Demo Consignment Shop", organization.Name);
        Assert.Equal(VerticalType.Consignment, organization.VerticalType);
        Assert.Equal("demo-shop", organization.Subdomain);
    }

    [Fact]
    public async Task SeedData_Creates4Users()
    {
        // Act
        var userCount = await _context.Users.CountAsync();

        // Assert
        Assert.Equal(4, userCount);
    }

    [Fact]
    public async Task SeedData_AdminUserCanLogin()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "admin@microsaasbuilders.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin@microsaasbuilders.com", result.Email);
        Assert.Equal(UserRole.Owner, result.Role);
        Assert.Equal("Demo Consignment Shop", result.OrganizationName);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task SeedData_ShopOwnerUserCanLogin()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "owner1@microsaasbuilders.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("owner1@microsaasbuilders.com", result.Email);
        Assert.Equal(UserRole.Owner, result.Role);
    }

    [Fact]
    public async Task SeedData_ProviderUserCanLogin()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "provider1@microsaasbuilders.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("provider1@microsaasbuilders.com", result.Email);
        Assert.Equal(UserRole.Provider, result.Role);
    }

    [Fact]
    public async Task SeedData_CustomerUserCanLogin()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "customer1@microsaasbuilders.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("customer1@microsaasbuilders.com", result.Email);
        Assert.Equal(UserRole.Customer, result.Role);
    }

    [Fact]
    public async Task SeedData_CreatesProviderEntity()
    {
        // Act
        var provider = await _context.Providers
            .Include(p => p.User)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(provider);
        Assert.Equal("Demo", provider.FirstName);
        Assert.Equal("Artist", provider.LastName);
        Assert.Equal("provider1@microsaasbuilders.com", provider.Email);
        Assert.Equal(0.6000m, provider.CommissionRate);
        Assert.Equal(ProviderStatus.Active, provider.Status);
        Assert.NotNull(provider.User);
        Assert.Equal(UserRole.Provider, provider.User.Role);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}