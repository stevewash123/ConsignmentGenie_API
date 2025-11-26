using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class SlugServiceTests : IDisposable
{
    private readonly ConsignmentGenieContext _context;
    private readonly SlugService _slugService;

    public SlugServiceTests()
    {
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ConsignmentGenieContext(options);
        _slugService = new SlugService(_context);
    }

    #region GenerateSlug Tests

    [Fact]
    public void GenerateSlug_WithValidInput_ReturnsProperSlug()
    {
        // Arrange
        var input = "Main Street Consignment";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("main-street-consignment", result);
    }

    [Fact]
    public void GenerateSlug_WithSpecialCharacters_RemovesSpecialCharacters()
    {
        // Arrange
        var input = "Jane's Vintage & Antique Shop!";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("janes-vintage-and-antique-shop", result);
    }

    [Fact]
    public void GenerateSlug_WithMultipleSpaces_ReplacesWithSingleHyphen()
    {
        // Arrange
        var input = "Multiple    Spaces   Here";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("multiple-spaces-here", result);
    }

    [Fact]
    public void GenerateSlug_WithLeadingTrailingSpaces_TrimsSpaces()
    {
        // Arrange
        var input = "   Trimmed Shop   ";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("trimmed-shop", result);
    }

    [Fact]
    public void GenerateSlug_WithConsecutiveHyphens_ReplacesWithSingleHyphen()
    {
        // Arrange
        var input = "Shop--With--Hyphens";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("shop-with-hyphens", result);
    }

    [Fact]
    public void GenerateSlug_WithEmptyString_ReturnsShop()
    {
        // Arrange
        var input = "";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void GenerateSlug_WithWhitespaceOnly_ReturnsShop()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void GenerateSlug_WithNumbers_PreservesNumbers()
    {
        // Arrange
        var input = "Shop 123 Main Street";

        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal("shop-123-main-street", result);
    }

    [Theory]
    [InlineData("Café & Bistro", "cafe-and-bistro")]
    [InlineData("Mom's Place", "moms-place")]
    [InlineData("The #1 Shop", "the-1-shop")]
    [InlineData("100% Vintage", "100-vintage")]
    [InlineData("Shop@Main.com", "shopmaincom")]
    public void GenerateSlug_WithVariousInputs_GeneratesExpectedSlugs(string input, string expected)
    {
        // Act
        var result = _slugService.GenerateSlug(input);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsValidSlug Tests

    [Theory]
    [InlineData("main-street-consignment", true)]
    [InlineData("shop", true)]
    [InlineData("vintage-shop-123", true)]
    [InlineData("a", true)]
    [InlineData("a-b", true)]
    [InlineData("123", true)]
    public void IsValidSlug_WithValidSlugs_ReturnsTrue(string slug, bool expected)
    {
        // Act
        var result = _slugService.IsValidSlug(slug);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("-shop", false)]
    [InlineData("shop-", false)]
    [InlineData("shop--store", false)]
    [InlineData("UPPERCASE", false)]
    [InlineData("shop_store", false)]
    [InlineData("shop.store", false)]
    [InlineData("shop@store", false)]
    [InlineData("shop store", false)]
    public void IsValidSlug_WithInvalidSlugs_ReturnsFalse(string slug, bool expected)
    {
        // Act
        var result = _slugService.IsValidSlug(slug);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region GenerateUniqueOrganizationSlugAsync Tests

    [Fact]
    public async Task GenerateUniqueOrganizationSlugAsync_WithUniqueSlug_ReturnsOriginalSlug()
    {
        // Arrange
        var shopName = "Unique Shop Name";

        // Act
        var result = await _slugService.GenerateUniqueOrganizationSlugAsync(shopName);

        // Assert
        Assert.Equal("unique-shop-name", result);
    }

    [Fact]
    public async Task GenerateUniqueOrganizationSlugAsync_WithExistingSlug_ReturnsNumberedSlug()
    {
        // Arrange
        var shopName = "Test Shop";
        var existingOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            Slug = "test-shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(existingOrg);
        await _context.SaveChangesAsync();

        // Act
        var result = await _slugService.GenerateUniqueOrganizationSlugAsync(shopName);

        // Assert
        Assert.Equal("test-shop-1", result);
    }

    [Fact]
    public async Task GenerateUniqueOrganizationSlugAsync_WithMultipleExistingSlugs_ReturnsNextAvailableNumber()
    {
        // Arrange
        var shopName = "Popular Shop";
        var organizations = new[]
        {
            new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Popular Shop",
                Slug = "popular-shop",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Popular Shop 1",
                Slug = "popular-shop-1",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Popular Shop 2",
                Slug = "popular-shop-2",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Organizations.AddRange(organizations);
        await _context.SaveChangesAsync();

        // Act
        var result = await _slugService.GenerateUniqueOrganizationSlugAsync(shopName);

        // Assert
        Assert.Equal("popular-shop-3", result);
    }

    [Fact]
    public async Task GenerateUniqueOrganizationSlugAsync_WithExcludedOrganization_IgnoresExcludedId()
    {
        // Arrange
        var shopName = "Test Shop";
        var existingOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            Slug = "test-shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(existingOrg);
        await _context.SaveChangesAsync();

        // Act - Exclude the existing organization (simulating an update scenario)
        var result = await _slugService.GenerateUniqueOrganizationSlugAsync(shopName, existingOrg.Id);

        // Assert
        Assert.Equal("test-shop", result); // Should return original slug since we're excluding the existing one
    }

    [Fact]
    public async Task GenerateUniqueOrganizationSlugAsync_WithEmptyShopName_ReturnsUniqueShopSlug()
    {
        // Arrange
        var shopName = "";

        // Act
        var result = await _slugService.GenerateUniqueOrganizationSlugAsync(shopName);

        // Assert
        Assert.Equal("shop", result);
    }

    [Fact]
    public async Task GenerateUniqueOrganizationSlugAsync_WithShopSlugExists_ReturnsNumberedShopSlug()
    {
        // Arrange
        var existingOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Shop",
            Slug = "shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(existingOrg);
        await _context.SaveChangesAsync();

        // Act
        var result = await _slugService.GenerateUniqueOrganizationSlugAsync("");

        // Assert
        Assert.Equal("shop-1", result);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}