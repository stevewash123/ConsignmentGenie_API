using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class AnonymousSessionTests : IDisposable
{
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Mock<ILogger<CartService>> _mockLogger;
    private readonly CartService _cartService;
    private readonly Guid _organizationId;
    private readonly Guid _itemId1;
    private readonly Guid _itemId2;

    public AnonymousSessionTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<CartService>>();
        _cartService = new CartService(_context, _mockLogger.Object);

        _organizationId = Guid.NewGuid();
        _itemId1 = Guid.NewGuid();
        _itemId2 = Guid.NewGuid();

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

        var items = new List<Item>
        {
            new Item
            {
                Id = _itemId1,
                OrganizationId = _organizationId,
                Title = "Test Item 1",
                Price = 25.99m,
                Status = ItemStatus.Available,
                Category = "Electronics",
                ListedDate = DateOnly.FromDateTime(DateTime.Now)
            },
            new Item
            {
                Id = _itemId2,
                OrganizationId = _organizationId,
                Title = "Test Item 2",
                Price = 45.50m,
                Status = ItemStatus.Available,
                Category = "Clothing",
                ListedDate = DateOnly.FromDateTime(DateTime.Now)
            }
        };

        _context.Organizations.Add(organization);
        _context.Items.AddRange(items);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AnonymousCart_CreateAndRetrieve_MaintainsSession()
    {
        // Arrange
        var sessionId = "anonymous-session-" + Guid.NewGuid();

        // Act - Add item to cart with session ID
        var cartWithItem = await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null);

        // Retrieve cart with same session ID
        var retrievedCart = await _cartService.GetCartAsync(_organizationId, sessionId, null);

        // Assert
        Assert.NotNull(cartWithItem);
        Assert.NotNull(retrievedCart);
        Assert.Equal(cartWithItem.Id, retrievedCart.Id);
        Assert.Equal(1, retrievedCart.ItemCount);
        Assert.Equal(25.99m, retrievedCart.Subtotal);
    }

    [Fact]
    public async Task AnonymousCart_DifferentSessionIds_IsolatedCarts()
    {
        // Arrange
        var sessionId1 = "anonymous-session-1-" + Guid.NewGuid();
        var sessionId2 = "anonymous-session-2-" + Guid.NewGuid();

        // Act - Add different items to different sessions
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId1, null);
        await _cartService.AddItemToCartAsync(_organizationId, _itemId2, sessionId2, null);

        var cart1 = await _cartService.GetCartAsync(_organizationId, sessionId1, null);
        var cart2 = await _cartService.GetCartAsync(_organizationId, sessionId2, null);

        // Assert
        Assert.NotEqual(cart1.Id, cart2.Id);
        Assert.Equal(1, cart1.ItemCount);
        Assert.Equal(1, cart2.ItemCount);
        Assert.Equal(25.99m, cart1.Subtotal); // Item 1 price
        Assert.Equal(45.50m, cart2.Subtotal); // Item 2 price
    }

    [Fact]
    public async Task AnonymousCart_AddSameItemTwice_DoesNotDuplicate()
    {
        // Arrange
        var sessionId = "anonymous-session-" + Guid.NewGuid();

        // Act - Add same item twice
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null);

        // Adding same item again should not duplicate
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null));

        // Assert
        Assert.Contains("Item is already reserved in another cart", exception.Message);

        var cart = await _cartService.GetCartAsync(_organizationId, sessionId, null);
        Assert.Equal(1, cart.ItemCount); // Should still be 1
    }

    [Fact]
    public async Task AnonymousCart_SessionPersistence_AcrossMultipleOperations()
    {
        // Arrange
        var sessionId = "persistent-session-" + Guid.NewGuid();

        // Act - Perform multiple operations
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null);
        var cartAfterAdd1 = await _cartService.GetCartAsync(_organizationId, sessionId, null);

        await _cartService.AddItemToCartAsync(_organizationId, _itemId2, sessionId, null);
        var cartAfterAdd2 = await _cartService.GetCartAsync(_organizationId, sessionId, null);

        await _cartService.RemoveItemFromCartAsync(_organizationId, _itemId1, sessionId, null);
        var cartAfterRemove = await _cartService.GetCartAsync(_organizationId, sessionId, null);

        // Assert
        Assert.Equal(1, cartAfterAdd1.ItemCount);
        Assert.Equal(2, cartAfterAdd2.ItemCount);
        Assert.Equal(1, cartAfterRemove.ItemCount);

        // All operations should use same cart ID
        Assert.Equal(cartAfterAdd1.Id, cartAfterAdd2.Id);
        Assert.Equal(cartAfterAdd2.Id, cartAfterRemove.Id);
    }

    [Fact]
    public async Task AnonymousCart_MergeWithAuthenticated_CombinesCarts()
    {
        // Arrange
        var sessionId = "anonymous-session-" + Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Create anonymous cart
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null);

        // Create authenticated cart (simulate existing customer cart)
        await _cartService.AddItemToCartAsync(_organizationId, _itemId2, null, customerId);

        // Act - Merge carts
        var mergedCart = await _cartService.MergeCartAsync(_organizationId, sessionId, customerId);

        // Assert
        Assert.Equal(2, mergedCart.ItemCount);
        Assert.Equal(71.49m, mergedCart.Subtotal); // 25.99 + 45.50

        // Verify anonymous cart is cleared/merged
        var anonymousCartAfterMerge = await _cartService.GetCartAsync(_organizationId, sessionId, null);
        Assert.Equal(0, anonymousCartAfterMerge.ItemCount);

        // Verify authenticated cart has both items
        var authenticatedCart = await _cartService.GetCartAsync(_organizationId, null, customerId);
        Assert.Equal(2, authenticatedCart.ItemCount);
    }

    [Fact]
    public async Task AnonymousCart_EmptySessionId_CreatesNewCart()
    {
        // Act - Get cart with null/empty session
        var cart1 = await _cartService.GetCartAsync(_organizationId, null, null);
        var cart2 = await _cartService.GetCartAsync(_organizationId, "", null);
        var cart3 = await _cartService.GetCartAsync(_organizationId, "   ", null);

        // Assert - Should create new empty carts each time
        Assert.Equal(0, cart1.ItemCount);
        Assert.Equal(0, cart2.ItemCount);
        Assert.Equal(0, cart3.ItemCount);

        // Each call should create a different cart
        Assert.NotEqual(cart1.Id, cart2.Id);
        Assert.NotEqual(cart2.Id, cart3.Id);
    }

    [Fact]
    public async Task AnonymousCart_LongSessionId_HandledCorrectly()
    {
        // Arrange - Create very long session ID (test boundary conditions)
        var longSessionId = "session-" + string.Join("", Enumerable.Repeat("a", 90)); // Close to 100 char limit

        // Act
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, longSessionId, null);
        var cart = await _cartService.GetCartAsync(_organizationId, longSessionId, null);

        // Assert
        Assert.Equal(1, cart.ItemCount);
    }

    [Fact]
    public async Task AnonymousCart_SpecialCharactersInSessionId_HandledCorrectly()
    {
        // Arrange - Test session ID with special characters
        var specialSessionId = "session-@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Act
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, specialSessionId, null);
        var cart = await _cartService.GetCartAsync(_organizationId, specialSessionId, null);

        // Assert
        Assert.Equal(1, cart.ItemCount);
    }

    [Fact]
    public async Task AnonymousCart_ClearCart_RemovesAllItems()
    {
        // Arrange
        var sessionId = "clear-test-session-" + Guid.NewGuid();

        // Add multiple items
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null);
        await _cartService.AddItemToCartAsync(_organizationId, _itemId2, sessionId, null);

        var cartBeforeClear = await _cartService.GetCartAsync(_organizationId, sessionId, null);
        Assert.Equal(2, cartBeforeClear.ItemCount);

        // Act - Clear cart
        var result = await _cartService.ClearCartAsync(_organizationId, sessionId, null);
        var cartAfterClear = await _cartService.GetCartAsync(_organizationId, sessionId, null);

        // Assert
        Assert.True(result);
        Assert.Equal(0, cartAfterClear.ItemCount);
        Assert.Equal(0m, cartAfterClear.Subtotal);
    }

    [Fact]
    public async Task AnonymousCart_ItemStatusChange_AffectsAvailability()
    {
        // Arrange
        var sessionId = "availability-test-" + Guid.NewGuid();

        // Add item to cart
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null);

        // Change item status to sold (simulate another user purchasing)
        var item = await _context.Items.FindAsync(_itemId1);
        item!.Status = ItemStatus.Sold;
        await _context.SaveChangesAsync();

        // Act - Get cart (should show item as unavailable)
        var cart = await _cartService.GetCartAsync(_organizationId, sessionId, null);

        // Assert
        Assert.Equal(1, cart.ItemCount);
        var cartItem = cart.Items.First();
        Assert.False(cartItem.IsAvailable);
    }

    [Fact]
    public async Task AnonymousCart_OrganizationIsolation_PreventsCrossContamination()
    {
        // Arrange
        var organization2Id = Guid.NewGuid();
        var organization2 = new Organization
        {
            Id = organization2Id,
            Name = "Store 2",
            Slug = "store-2",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };
        _context.Organizations.Add(organization2);
        await _context.SaveChangesAsync();

        var sessionId = "cross-org-test-" + Guid.NewGuid();

        // Act - Add item to org1 cart, try to access from org2
        await _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId, null);

        var org1Cart = await _cartService.GetCartAsync(_organizationId, sessionId, null);
        var org2Cart = await _cartService.GetCartAsync(organization2Id, sessionId, null);

        // Assert - Carts should be isolated by organization
        Assert.Equal(1, org1Cart.ItemCount);
        Assert.Equal(0, org2Cart.ItemCount);
        Assert.NotEqual(org1Cart.Id, org2Cart.Id);
    }

    [Fact]
    public async Task AnonymousCart_ConcurrentSessions_HandlesSafely()
    {
        // Arrange
        var sessionId1 = "concurrent-1-" + Guid.NewGuid();
        var sessionId2 = "concurrent-2-" + Guid.NewGuid();

        // Act - Simulate concurrent operations
        var tasks = new List<Task>
        {
            _cartService.AddItemToCartAsync(_organizationId, _itemId1, sessionId1, null),
            _cartService.AddItemToCartAsync(_organizationId, _itemId2, sessionId2, null),
            _cartService.GetCartAsync(_organizationId, sessionId1, null),
            _cartService.GetCartAsync(_organizationId, sessionId2, null)
        };

        await Task.WhenAll(tasks);

        // Get final state
        var cart1 = await _cartService.GetCartAsync(_organizationId, sessionId1, null);
        var cart2 = await _cartService.GetCartAsync(_organizationId, sessionId2, null);

        // Assert - Both carts should have their respective items
        Assert.Equal(1, cart1.ItemCount);
        Assert.Equal(1, cart2.ItemCount);
        Assert.NotEqual(cart1.Id, cart2.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}