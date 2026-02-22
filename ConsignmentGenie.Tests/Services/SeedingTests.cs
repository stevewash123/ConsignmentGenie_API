using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class SeedingTests : IDisposable
{
    private readonly ConsignmentGenieContext _context;
    private readonly AuthService _authService;
    private readonly SeedDataService _seedDataService;

    public SeedingTests()
    {
        // Create in-memory database with seeding
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ConsignmentGenieContext(options);

        // Ensure database is created
        _context.Database.EnsureCreated();

        // Create a demo organization first (required for seeding)
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Demo Consignment Shop",
            VerticalType = VerticalType.Consignment,
            Subdomain = "demo-shop",
            CreatedAt = DateTime.UtcNow
        };
        _context.Organizations.Add(organization);
        _context.SaveChanges();

        // Setup SeedDataService
        var mockLogger = new Mock<ILogger<SeedDataService>>();
        _seedDataService = new SeedDataService(_context, mockLogger.Object);

        // Run seeding
        _seedDataService.SeedDemoDataAsync().Wait();

        // Setup AuthService
        var mockConfiguration = new Mock<IConfiguration>();
        var mockAuthLogger = new Mock<ILogger<AuthService>>();
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["Key"]).Returns("ConsignmentGenie_Super_Secret_Key_2024_32_Characters_Long!");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

        var mockSlugService = new Mock<ISlugService>();
        mockSlugService.Setup(x => x.GenerateUniqueOrganizationSlugAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
                      .ReturnsAsync("test-slug");

        _authService = new AuthService(_context, mockConfiguration.Object, mockAuthLogger.Object, mockSlugService.Object);
    }

    [Fact]
    public async Task SeedData_CreatesOrganization()
    {
        // Act
        var organization = await _context.Organizations.FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(organization);
        Assert.Equal("Winnie's Attic", organization.Name); // Updated by seeding
        Assert.Equal(VerticalType.Consignment, organization.VerticalType);
        Assert.Equal("demo-shop", organization.Subdomain);
    }

    [Fact]
    public async Task SeedData_Creates8Users()
    {
        // Act
        var userCount = await _context.Users.CountAsync();

        // Assert
        Assert.Equal(11, userCount); // SeedDataService creates 8 consignor users + 3 additional users
    }

    [Fact]
    public async Task SeedData_ConsignorUserCanLogin()
    {
        // Arrange - Use the first seeded consignor
        var firstConsignor = await _context.Consignors.Include(c => c.User).FirstAsync();
        var loginRequest = new LoginRequest
        {
            Email = firstConsignor.Email,
            Password = "provider123" // Password from SeedDataService
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(firstConsignor.Email, result.Email);
        Assert.Equal(UserRole.Consignor, result.Role);
        Assert.Equal("Winnie's Attic", result.OrganizationName);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task SeedData_SecondConsignorUserCanLogin()
    {
        // Arrange - Use the second seeded consignor
        var secondConsignor = await _context.Consignors.Include(c => c.User).Skip(1).FirstAsync();
        var loginRequest = new LoginRequest
        {
            Email = secondConsignor.Email,
            Password = "provider123"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondConsignor.Email, result.Email);
        Assert.Equal(UserRole.Consignor, result.Role);
    }

    [Fact]
    public async Task SeedData_CreatesConsignorEntity()
    {
        // Act - Get Jane Doe specifically since seed order might change
        var consignor = await _context.Consignors
            .Include(p => p.User)
            .FirstOrDefaultAsync(c => c.Email == "jane.doe@email.com");

        // Assert
        Assert.NotNull(consignor);
        Assert.Equal("Jane", consignor.Name); // Jane Doe from SeedDataService
        Assert.Equal("Doe", consignor);
        Assert.Equal("jane.doe@email.com", consignor.Email);
        Assert.Equal(0.5000m, consignor.CommissionRate); // 50% from SeedDataService
        Assert.Equal(ConsignorStatus.Active, consignor.Status);
        Assert.NotNull(consignor.User);
        Assert.Equal(UserRole.Consignor, consignor.User.Role);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}