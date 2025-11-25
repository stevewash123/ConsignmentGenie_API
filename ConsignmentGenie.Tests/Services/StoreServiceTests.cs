using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class StoreServiceTests : IDisposable
{
    private readonly Mock<ILogger<StoreService>> _mockLogger;
    private readonly StoreService _storeService;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Guid _organizationId;
    private readonly string _storeSlug = "test-store";

    public StoreServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<StoreService>>();
        _storeService = new StoreService(_context, _mockLogger.Object);

        _organizationId = Guid.NewGuid();
        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        var organization = new Organization
        {
            Id = _organizationId,
            Name = "Test Store",
            Slug = _storeSlug,
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };

        var items = new List<Item>
        {
            new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Title = "Test Item 1",
                Description = "A great test item",
                Price = 25.99m,
                Status = ItemStatus.Available,
                Category = "Electronics",
                Brand = "TestBrand",
                ListedDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5))
            },
            new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Title = "Test Item 2",
                Description = "Another test item",
                Price = 45.50m,
                Status = ItemStatus.Available,
                Category = "Clothing",
                Brand = "AnotherBrand",
                ListedDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-3))
            },
            new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Title = "Sold Item",
                Price = 15.00m,
                Status = ItemStatus.Sold,
                Category = "Electronics",
                ListedDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10))
            }
        };

        _context.Organizations.Add(organization);
        _context.Items.AddRange(items);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStoreInfoAsync_ValidSlug_ReturnsStoreInfo()
    {
        // Act
        var result = await _storeService.GetStoreInfoAsync(_storeSlug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_storeSlug, result.Slug);
        Assert.Equal("Test Store", result.Name);
        Assert.Equal(0.085m, result.TaxRate);
        Assert.True(result.ShippingEnabled);
    }

    [Fact]
    public async Task GetStoreInfoAsync_InvalidSlug_ReturnsNull()
    {
        // Act
        var result = await _storeService.GetStoreInfoAsync("non-existent-store");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetItemsAsync_NoFilters_ReturnsAllAvailableItems()
    {
        // Arrange
        var queryParams = new ItemQueryParams { Page = 1, PageSize = 10 };

        // Act
        var (items, totalCount) = await _storeService.GetItemsAsync(_storeSlug, queryParams);

        // Assert
        Assert.Equal(2, totalCount); // Should only return available items
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.True(item.IsAvailable));
    }

    [Fact]
    public async Task GetItemsAsync_CategoryFilter_ReturnsFilteredItems()
    {
        // Arrange
        var queryParams = new ItemQueryParams
        {
            Category = "Electronics",
            Page = 1,
            PageSize = 10
        };

        // Act
        var (items, totalCount) = await _storeService.GetItemsAsync(_storeSlug, queryParams);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("Electronics", items.First().Category);
    }

    [Fact]
    public async Task GetItemsAsync_PriceFilter_ReturnsFilteredItems()
    {
        // Arrange
        var queryParams = new ItemQueryParams
        {
            MinPrice = 30m,
            MaxPrice = 50m,
            Page = 1,
            PageSize = 10
        };

        // Act
        var (items, totalCount) = await _storeService.GetItemsAsync(_storeSlug, queryParams);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal(45.50m, items.First().Price);
    }

    [Fact]
    public async Task GetItemsAsync_SearchFilter_ReturnsMatchingItems()
    {
        // Arrange
        var queryParams = new ItemQueryParams
        {
            Search = "great",
            Page = 1,
            PageSize = 10
        };

        // Act
        var (items, totalCount) = await _storeService.GetItemsAsync(_storeSlug, queryParams);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Contains("great", items.First().Description?.ToLower());
    }

    [Fact]
    public async Task GetItemsAsync_SortByPrice_ReturnsSortedItems()
    {
        // Arrange
        var queryParams = new ItemQueryParams
        {
            Sort = "price-low-high",
            Page = 1,
            PageSize = 10
        };

        // Act
        var (items, totalCount) = await _storeService.GetItemsAsync(_storeSlug, queryParams);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.True(items.First().Price < items.Last().Price);
        Assert.Equal(25.99m, items.First().Price);
        Assert.Equal(45.50m, items.Last().Price);
    }

    [Fact]
    public async Task GetItemsAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var queryParams = new ItemQueryParams
        {
            Page = 2,
            PageSize = 1
        };

        // Act
        var (items, totalCount) = await _storeService.GetItemsAsync(_storeSlug, queryParams);

        // Assert
        Assert.Equal(2, totalCount); // Total items available
        Assert.Single(items); // Only one item per page
    }

    [Fact]
    public async Task GetItemDetailAsync_ValidItem_ReturnsItemDetail()
    {
        // Arrange
        var availableItem = await _context.Items
            .Where(i => i.OrganizationId == _organizationId && i.Status == ItemStatus.Available)
            .FirstAsync();

        // Act
        var result = await _storeService.GetItemDetailAsync(_storeSlug, availableItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(availableItem.Id, result.Id);
        Assert.Equal(availableItem.Title, result.Title);
        Assert.Equal(availableItem.Price, result.Price);
        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task GetItemDetailAsync_SoldItem_ReturnsNull()
    {
        // Arrange
        var soldItem = await _context.Items
            .Where(i => i.OrganizationId == _organizationId && i.Status == ItemStatus.Sold)
            .FirstAsync();

        // Act
        var result = await _storeService.GetItemDetailAsync(_storeSlug, soldItem.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetItemDetailAsync_NonExistentItem_ReturnsNull()
    {
        // Act
        var result = await _storeService.GetItemDetailAsync(_storeSlug, Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCategoriesAsync_ValidStore_ReturnsCategories()
    {
        // Act
        var result = await _storeService.GetCategoriesAsync(_storeSlug);

        // Assert
        Assert.Equal(2, result.Count);

        var electronicsCategory = result.First(c => c.Name == "Electronics");
        Assert.Equal(1, electronicsCategory.ItemCount); // Only available items counted

        var clothingCategory = result.First(c => c.Name == "Clothing");
        Assert.Equal(1, clothingCategory.ItemCount);
    }

    [Fact]
    public async Task GetCategoriesAsync_InvalidStore_ReturnsEmptyList()
    {
        // Act
        var result = await _storeService.GetCategoriesAsync("non-existent-store");

        // Assert
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}