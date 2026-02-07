using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly Mock<ICartService> _mockCartService;
    private readonly OrderService _orderService;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Guid _organizationId;
    private readonly Guid _itemId;

    public OrderServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<OrderService>>();
        _mockCartService = new Mock<ICartService>();
        _orderService = new OrderService(_context, _mockCartService.Object, _mockLogger.Object);

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
            PrimaryImageUrl = "https://example.com/image.jpg",
            ListedDate = DateOnly.FromDateTime(DateTime.Now)
        };

        var cart = new ShoppingCart
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            SessionId = "test-session",
            LastUpdatedAt = DateTime.UtcNow,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    ItemId = _itemId,
                    AddedAt = DateTime.UtcNow
                }
            }
        };

        _context.Organizations.Add(organization);
        _context.Items.Add(item);
        _context.ShoppingCarts.Add(cart);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task ValidateCartForCheckoutAsync_ValidCart_ReturnsValid()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        var result = await _orderService.ValidateCartForCheckoutAsync(_organizationId, sessionId, null);

        // Assert
        Assert.True(result.Valid);
        Assert.Empty(result.UnavailableItems);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCartForCheckoutAsync_EmptyCart_ReturnsInvalid()
    {
        // Arrange
        var sessionId = "empty-session";

        // Act
        var result = await _orderService.ValidateCartForCheckoutAsync(_organizationId, sessionId, null);

        // Assert
        Assert.False(result.Valid);
        Assert.Equal("Cart is empty", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCartForCheckoutAsync_ItemUnavailable_ReturnsInvalidWithUnavailableItems()
    {
        // Arrange
        var sessionId = "test-session";

        // Mark item as sold
        var item = await _context.Items.FindAsync(_itemId);
        item!.Status = ItemStatus.Sold;
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.ValidateCartForCheckoutAsync(_organizationId, sessionId, null);

        // Assert
        Assert.False(result.Valid);
        Assert.Contains(_itemId, result.UnavailableItems);
        Assert.Equal("Some items in your cart are no longer available", result.ErrorMessage);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_ValidRequest_ReturnsPaymentIntent()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        // Act
        var result = await _orderService.CreatePaymentIntentAsync(_organizationId, request, sessionId, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.PaymentIntentId);
        Assert.NotEmpty(result.ClientSecret);
        Assert.True(result.Amount > 0);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidRequest_CreatesOrder()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            Phone = "555-1234",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        // Setup mock cart service to clear cart
        _mockCartService.Setup(x => x.ClearCartAsync(_organizationId, sessionId, null))
                       .ReturnsAsync(true);

        // Act
        var result = await _orderService.CreateOrderAsync(_organizationId, request, sessionId, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.OrderNumber);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("test@example.com", result.CustomerEmail);
        Assert.Equal("Test Customer", result.CustomerName);
        Assert.Equal("pickup", result.FulfillmentType);
        Assert.Single(result.Items);
        Assert.True(result.TotalAmount > 0);

        // Verify item was marked as sold
        var item = await _context.Items.FindAsync(_itemId);
        Assert.Equal(ItemStatus.Sold, item!.Status);
        Assert.NotNull(item.SoldDate);
    }

    [Fact]
    public async Task CreateOrderAsync_ShippingWithoutAddress_ThrowsException()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "shipping", // Requires address
            PaymentMethod = "card"
            // ShippingAddress is null
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _orderService.CreateOrderAsync(_organizationId, request, sessionId, null));
    }

    [Fact]
    public async Task GetOrderAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        _mockCartService.Setup(x => x.ClearCartAsync(_organizationId, sessionId, null))
                       .ReturnsAsync(true);

        var createdOrder = await _orderService.CreateOrderAsync(_organizationId, request, sessionId, null);

        // Act
        var result = await _orderService.GetOrderAsync(_organizationId, createdOrder.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdOrder.Id, result.Id);
        Assert.Equal(createdOrder.OrderNumber, result.OrderNumber);
    }

    [Fact]
    public async Task GetOrderAsync_NonExistentOrder_ReturnsNull()
    {
        // Act
        var result = await _orderService.GetOrderAsync(_organizationId, Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ValidOrder_UpdatesStatus()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        _mockCartService.Setup(x => x.ClearCartAsync(_organizationId, sessionId, null))
                       .ReturnsAsync(true);

        var order = await _orderService.CreateOrderAsync(_organizationId, request, sessionId, null);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(_organizationId, order.Id, "Paid");

        // Assert
        Assert.True(result);

        var updatedOrder = await _orderService.GetOrderAsync(_organizationId, order.Id);
        Assert.Equal("Paid", updatedOrder!.Status);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidStatus_ReturnsFalse()
    {
        // Arrange
        var sessionId = "test-session";
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        _mockCartService.Setup(x => x.ClearCartAsync(_organizationId, sessionId, null))
                       .ReturnsAsync(true);

        var order = await _orderService.CreateOrderAsync(_organizationId, request, sessionId, null);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(_organizationId, order.Id, "InvalidStatus");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessPaymentConfirmationAsync_ValidPaymentIntent_ReturnsTrue()
    {
        // Arrange
        var paymentIntentId = "pi_test123";

        // Act
        var result = await _orderService.ProcessPaymentConfirmationAsync(_organizationId, paymentIntentId);

        // Assert
        Assert.True(result); // Placeholder implementation always returns true
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}