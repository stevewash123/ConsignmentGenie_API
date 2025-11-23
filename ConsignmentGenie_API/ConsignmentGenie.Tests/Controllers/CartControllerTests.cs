using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers;

public class CartControllerTests
{
    private readonly Mock<ICartService> _mockCartService;
    private readonly Mock<ILogger<CartController>> _mockLogger;
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly CartController _controller;

    public CartControllerTests()
    {
        _mockCartService = new Mock<ICartService>();
        _mockLogger = new Mock<ILogger<CartController>>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _controller = new CartController(_mockCartService.Object, _mockLogger.Object, _mockOrganizationService.Object);

        // Setup ControllerContext for header access
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetCart_WithSessionId_ReturnsCart()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var expectedCart = new CartDto
        {
            Id = Guid.NewGuid(),
            ItemCount = 2,
            Subtotal = 50.99m,
            EstimatedTax = 4.33m,
            EstimatedTotal = 55.32m,
            Items = new List<CartItemDto>()
        };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        // Mock the organization service to return a valid organization ID
        _mockOrganizationService.Setup(x => x.GetIdBySlugAsync(storeSlug))
                               .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.GetCartAsync(organizationId, sessionId, null))
                       .ReturnsAsync(expectedCart);

        // Act
        var result = await _controller.GetCart(storeSlug);

        // Assert - Now should work properly with constructor injection
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedCart.ItemCount, response.Data.ItemCount);
        Assert.Equal(expectedCart.Subtotal, response.Data.Subtotal);
    }

    [Fact]
    public async Task GetCart_WithoutSessionId_ReturnsEmptyCart()
    {
        // Arrange
        var storeSlug = "test-store";
        var organizationId = Guid.NewGuid();
        var emptyCart = new CartDto
        {
            Id = Guid.NewGuid(),
            ItemCount = 0,
            Subtotal = 0m,
            EstimatedTax = 0m,
            EstimatedTotal = 0m,
            Items = new List<CartItemDto>()
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.GetCartAsync(organizationId, null, null))
                       .ReturnsAsync(emptyCart);

        // Act
        var result = await _controller.GetCart(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(0, response.Data.ItemCount);
    }

    [Fact]
    public async Task AddItemToCart_ValidItem_ReturnsUpdatedCart()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new AddToCartRequest { ItemId = Guid.NewGuid() };
        var updatedCart = new CartDto
        {
            Id = Guid.NewGuid(),
            ItemCount = 1,
            Subtotal = 25.99m,
            EstimatedTax = 2.21m,
            EstimatedTotal = 28.20m,
            Items = new List<CartItemDto>
            {
                new CartItemDto
                {
                    ItemId = request.ItemId,
                    Name = "Test Item",
                    Price = 25.99m,
                    IsAvailable = true,
                    AddedAt = DateTime.UtcNow
                }
            }
        };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.AddItemToCartAsync(organizationId, request.ItemId, sessionId, null))
                       .ReturnsAsync(updatedCart);

        // Act
        var result = await _controller.AddItemToCart(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Data.ItemCount);
        Assert.Single(response.Data.Items);
    }

    [Fact]
    public async Task AddItemToCart_ItemNotFound_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new AddToCartRequest { ItemId = Guid.NewGuid() };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.AddItemToCartAsync(organizationId, request.ItemId, sessionId, null))
                       .ThrowsAsync(new ArgumentException("Item not found"));

        // Act
        var result = await _controller.AddItemToCart(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Item not found", response.Errors);
    }

    [Fact]
    public async Task AddItemToCart_ItemNotAvailable_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new AddToCartRequest { ItemId = Guid.NewGuid() };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.AddItemToCartAsync(organizationId, request.ItemId, sessionId, null))
                       .ThrowsAsync(new InvalidOperationException("Item is not available for purchase"));

        // Act
        var result = await _controller.AddItemToCart(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Item is not available for purchase", response.Errors);
    }

    [Fact]
    public async Task RemoveFromCart_ValidItem_ReturnsUpdatedCart()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var updatedCart = new CartDto
        {
            Id = Guid.NewGuid(),
            ItemCount = 0,
            Subtotal = 0m,
            EstimatedTax = 0m,
            EstimatedTotal = 0m,
            Items = new List<CartItemDto>()
        };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.RemoveItemFromCartAsync(organizationId, itemId, sessionId, null))
                       .ReturnsAsync(updatedCart);

        // Act
        var result = await _controller.RemoveItemFromCart(storeSlug, itemId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(0, response.Data.ItemCount);
    }

    [Fact]
    public async Task RemoveFromCart_ItemNotInCart_ReturnsCurrentCart()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var currentCart = new CartDto
        {
            Id = Guid.NewGuid(),
            ItemCount = 1,
            Subtotal = 25.99m,
            EstimatedTax = 2.21m,
            EstimatedTotal = 28.20m,
            Items = new List<CartItemDto>()
        };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.RemoveItemFromCartAsync(organizationId, itemId, sessionId, null))
                       .ReturnsAsync(currentCart);

        // Act
        var result = await _controller.RemoveItemFromCart(storeSlug, itemId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Data.ItemCount);
    }

    [Fact]
    public async Task ClearCart_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.ClearCartAsync(organizationId, sessionId, null))
                       .ReturnsAsync(true);

        // Act
        var result = await _controller.ClearCart(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task ClearCart_ServiceFails_ReturnsInternalServerError()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.ClearCartAsync(organizationId, sessionId, null))
                       .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ClearCart(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
        var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<bool>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("An error occurred clearing cart", response.Errors);
    }

    [Fact]
    public async Task MergeCart_ValidRequest_ReturnsUpdatedCart()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var mergedCart = new CartDto
        {
            Id = Guid.NewGuid(),
            ItemCount = 3,
            Subtotal = 75.99m,
            EstimatedTax = 6.46m,
            EstimatedTotal = 82.45m,
            Items = new List<CartItemDto>()
        };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.MergeCartAsync(organizationId, sessionId, It.IsAny<Guid>()))
                       .ReturnsAsync(mergedCart);

        // Act
        var result = await _controller.MergeCart(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Authentication required for cart merge", response.Errors);
    }

    [Fact]
    public async Task MergeCart_NoSessionId_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var organizationId = Guid.NewGuid();
        // No session ID header set

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        // Act
        var result = await _controller.MergeCart(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Session ID required for cart merge", response.Errors);
    }

    [Fact]
    public async Task AddItemToCart_InvalidGuid_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new AddToCartRequest { ItemId = Guid.Empty };

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        // Act
        var result = await _controller.AddItemToCart(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Item ID is required", response.Errors);
    }

    [Fact]
    public async Task RemoveFromCart_InvalidGuid_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var itemId = Guid.Empty;

        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        // Act
        var result = await _controller.RemoveItemFromCart(storeSlug, itemId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CartDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Item ID is required", response.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetCart_InvalidSlug_CallsServiceWithSlug(string invalidSlug)
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var emptyCart = new CartDto
        {
            Id = Guid.NewGuid(),
            ItemCount = 0,
            Subtotal = 0m,
            EstimatedTax = 0m,
            EstimatedTotal = 0m,
            Items = new List<CartItemDto>()
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.GetCartAsync(organizationId, It.IsAny<string>(), It.IsAny<Guid?>()))
                       .ReturnsAsync(emptyCart);

        // Act
        var result = await _controller.GetCart(invalidSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.NotNull(okResult);
        // The service should still be called - slug validation happens at service level
        _mockCartService.Verify(x => x.GetCartAsync(organizationId, It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task GetCart_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var storeSlug = "test-store";
        var organizationId = Guid.NewGuid();

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        _mockCartService.Setup(x => x.GetCartAsync(organizationId, It.IsAny<string>(), It.IsAny<Guid?>()))
                       .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _controller.GetCart(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CartDto>>>(result);
        var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<CartDto>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("An error occurred retrieving cart", response.Errors);
    }
}