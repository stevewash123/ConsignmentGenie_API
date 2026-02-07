using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class CartServiceTests : IDisposable
{
    private readonly Mock<ILogger<CartService>> _mockLogger;
    private readonly CartService _cartService;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Guid _organizationId;
    private readonly Guid _itemId;

    public CartServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<CartService>>();
        _cartService = new CartService(_context, _mockLogger.Object);

        // Setup test data
        _organizationId = Guid.NewGuid();
        _itemId = Guid.NewGuid();

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        var organization = new Organization
        {
            Id = _organizationId,
            Name = "Test Store",
            Slug = "test-store",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };

        var item = new Item
        {
            Id = _itemId,
            OrganizationId = _organizationId,
            Title = "Test Item",
            Price = 25.99m,
            Status = ItemStatus.Available,
            Category = "Electronics",
            ListedDate = DateOnly.FromDateTime(DateTime.Now)
        };

        _context.Organizations.Add(organization);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCartAsync_EmptyCart_ReturnsEmptyCart()
    {
        // Act
        var result = await _cartService.GetCartAsync(_organizationId, "test-session", null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.ItemCount);
        Assert.Equal(0, result.Subtotal);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task AddItemToCartAsync_ValidItem_AddsToCart()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        var result = await _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ItemCount);
        Assert.Equal(25.99m, result.Subtotal);
        Assert.Single(result.Items);
        Assert.Equal(_itemId, result.Items.First().ItemId);
        Assert.Equal("Test Item", result.Items.First().Name);
    }

    [Fact]
    public async Task AddItemToCartAsync_ItemNotFound_ThrowsArgumentException()
    {
        // Arrange
        var nonExistentItemId = Guid.NewGuid();
        var sessionId = "test-session";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cartService.AddItemToCartAsync(_organizationId, nonExistentItemId, sessionId, null));
    }

    [Fact]
    public async Task AddItemToCartAsync_ItemNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var sessionId = "test-session";

        // Mark item as sold
        var item = await _context.Items.FindAsync(_itemId);
        item!.Status = ItemStatus.Sold;
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId, null));
    }

    [Fact]
    public async Task AddItemToCartAsync_ItemAlreadyInAnotherCart_ThrowsInvalidOperationException()
    {
        // Arrange
        var sessionId1 = "session-1";
        var sessionId2 = "session-2";

        // Add item to first cart
        await _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId1, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId2, null));
    }

    [Fact]
    public async Task RemoveItemFromCartAsync_ExistingItem_RemovesFromCart()
    {
        // Arrange
        var sessionId = "test-session";
        await _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId, null);

        // Act
        var result = await _cartService.RemoveItemFromCartAsync(_organizationId, _itemId, sessionId, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.ItemCount);
        Assert.Equal(0, result.Subtotal);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ClearCartAsync_WithItems_ClearsCart()
    {
        // Arrange
        var sessionId = "test-session";
        await _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId, null);

        // Act
        var result = await _cartService.ClearCartAsync(_organizationId, sessionId, null);

        // Assert
        Assert.True(result);

        // Verify cart is empty
        var cart = await _cartService.GetCartAsync(_organizationId, sessionId, null);
        Assert.Equal(0, cart.ItemCount);
    }

    [Fact]
    public async Task MergeCartAsync_AnonymousCartExists_MergesWithUserCart()
    {
        // Arrange
        var sessionId = "test-session";
        var customerId = Guid.NewGuid();

        // Add item to anonymous cart
        await _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId, null);

        // Act
        var result = await _cartService.MergeCartAsync(_organizationId, sessionId, customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ItemCount);
        Assert.Equal(_itemId, result.Items.First().ItemId);

        // Verify anonymous cart no longer exists
        var anonymousCart = await _cartService.GetCartAsync(_organizationId, sessionId, null);
        Assert.Equal(0, anonymousCart.ItemCount);

        // Verify user cart has the item
        var userCart = await _cartService.GetCartAsync(_organizationId, null, customerId);
        Assert.Equal(1, userCart.ItemCount);
    }

    [Fact]
    public async Task IsItemInCartAsync_ItemInCart_ReturnsTrue()
    {
        // Arrange
        var sessionId = "test-session";
        await _cartService.AddItemToCartAsync(_organizationId, _itemId, sessionId, null);

        // Act
        var result = await _cartService.IsItemInCartAsync(_organizationId, _itemId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsItemInCartAsync_ItemNotInCart_ReturnsFalse()
    {
        // Arrange
        var nonExistentItemId = Guid.NewGuid();

        // Act
        var result = await _cartService.IsItemInCartAsync(_organizationId, nonExistentItemId);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}