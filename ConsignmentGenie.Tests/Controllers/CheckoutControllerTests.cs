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

public class CheckoutControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ILogger<CheckoutController>> _mockLogger;
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly CheckoutController _controller;

    public CheckoutControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockLogger = new Mock<ILogger<CheckoutController>>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _controller = new CheckoutController(_mockOrderService.Object, _mockLogger.Object, _mockOrganizationService.Object);

        // Setup ControllerContext for header access
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task ValidateCheckout_ValidCart_ReturnsValidationResult()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var validationResult = new CheckoutValidationDto
        {
            Valid = true,
            UnavailableItems = new List<Guid>(),
            ErrorMessage = null
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.ValidateCartForCheckoutAsync(organizationId, sessionId, null))
                        .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateCheckout(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CheckoutValidationDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CheckoutValidationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data.Valid);
        Assert.Empty(response.Data.UnavailableItems);
    }

    [Fact]
    public async Task ValidateCheckout_InvalidCart_ReturnsInvalidResult()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var validationResult = new CheckoutValidationDto
        {
            Valid = false,
            UnavailableItems = new List<Guid> { Guid.NewGuid() },
            ErrorMessage = "Some items in your cart are no longer available"
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.ValidateCartForCheckoutAsync(organizationId, sessionId, null))
                        .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ValidateCheckout(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CheckoutValidationDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CheckoutValidationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.False(response.Data.Valid);
        Assert.Single(response.Data.UnavailableItems);
        Assert.Contains("no longer available", response.Data.ErrorMessage);
    }

    [Fact]
    public async Task CreatePaymentIntent_ValidRequest_ReturnsPaymentIntent()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };
        var paymentIntent = new PaymentIntentDto
        {
            PaymentIntentId = "pi_test123",
            ClientSecret = "pi_test123_secret",
            Amount = 55.32m
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.CreatePaymentIntentAsync(organizationId, request, sessionId, null))
                        .ReturnsAsync(paymentIntent);

        // Act
        var result = await _controller.CreatePaymentIntent(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PaymentIntentDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PaymentIntentDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("pi_test123", response.Data.PaymentIntentId);
        Assert.Equal("pi_test123_secret", response.Data.ClientSecret);
        Assert.Equal(55.32m, response.Data.Amount);
    }

    [Fact]
    public async Task CreatePaymentIntent_InvalidCart_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.CreatePaymentIntentAsync(organizationId, request, sessionId, null))
                        .ThrowsAsync(new InvalidOperationException("Cart is empty"));

        // Act
        var result = await _controller.CreatePaymentIntent(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PaymentIntentDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PaymentIntentDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Cart is empty", response.Errors);
    }

    [Fact]
    public async Task CompleteCheckout_ValidRequest_ReturnsOrder()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            Phone = "555-1234",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };
        var order = new OrderDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "20231122-001",
            Status = "Pending",
            CustomerEmail = request.Email,
            CustomerName = request.Name,
            FulfillmentType = request.FulfillmentType,
            Subtotal = 50.00m,
            TaxAmount = 4.25m,
            TotalAmount = 54.25m,
            Items = new List<OrderItemDto>(),
            CreatedAt = DateTime.UtcNow
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.CreateOrderAsync(organizationId, request, sessionId, null))
                        .ReturnsAsync(order);

        // Act
        var result = await _controller.CompleteCheckout(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<OrderDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OrderDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(order.OrderNumber, response.Data.OrderNumber);
        Assert.Equal(order.CustomerEmail, response.Data.CustomerEmail);
    }

    [Fact]
    public async Task CompleteCheckout_ShippingWithoutAddress_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "shipping", // Requires shipping address
            PaymentMethod = "card"
            // Missing ShippingAddress
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.CreateOrderAsync(organizationId, request, sessionId, null))
                        .ThrowsAsync(new InvalidOperationException("Shipping address is required for shipping orders"));

        // Act
        var result = await _controller.CompleteCheckout(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<OrderDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OrderDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Shipping address is required for shipping orders", response.Errors);
    }

    [Fact]
    public async Task GetOrder_ValidOrder_ReturnsOrder()
    {
        // Arrange
        var storeSlug = "test-store";
        var orderId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var order = new OrderDto
        {
            Id = orderId,
            OrderNumber = "20231122-001",
            Status = "Pending",
            CustomerEmail = "test@example.com",
            CustomerName = "Test Customer",
            FulfillmentType = "pickup",
            Subtotal = 50.00m,
            TaxAmount = 4.25m,
            TotalAmount = 54.25m,
            Items = new List<OrderItemDto>(),
            CreatedAt = DateTime.UtcNow
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _mockOrderService.Setup(x => x.GetOrderAsync(organizationId, orderId))
                        .ReturnsAsync(order);

        // Act
        var result = await _controller.GetOrder(storeSlug, orderId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<OrderDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OrderDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(orderId, response.Data.Id);
        Assert.Equal("20231122-001", response.Data.OrderNumber);
    }

    [Fact]
    public async Task GetOrder_OrderNotFound_ReturnsNotFound()
    {
        // Arrange
        var storeSlug = "test-store";
        var orderId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _mockOrderService.Setup(x => x.GetOrderAsync(organizationId, orderId))
                        .ReturnsAsync((OrderDto?)null);

        // Act
        var result = await _controller.GetOrder(storeSlug, orderId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<OrderDto>>>(result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OrderDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Order not found", response.Errors);
    }

    [Fact]
    public async Task GetOrders_RequiresAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var storeSlug = "test-store";
        var organizationId = Guid.NewGuid();
        // No authentication setup

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);

        // Act
        var result = await _controller.GetOrders(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<List<OrderSummaryDto>>>>(result);
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<List<OrderSummaryDto>>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Authentication required", response.Errors);
    }

    [Fact]
    public async Task ConfirmPayment_ValidPaymentIntent_ReturnsSuccess()
    {
        // Arrange
        var storeSlug = "test-store";
        var paymentIntentId = "pi_test123";
        var organizationId = Guid.NewGuid();

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _mockOrderService.Setup(x => x.ProcessPaymentConfirmationAsync(organizationId, paymentIntentId))
                        .ReturnsAsync(true);

        // Act
        var result = await _controller.ConfirmPayment(storeSlug, paymentIntentId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task ConfirmPayment_InvalidPaymentIntent_ReturnsBadRequest()
    {
        // Arrange
        var storeSlug = "test-store";
        var paymentIntentId = "invalid_payment_intent";
        var organizationId = Guid.NewGuid();

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _mockOrderService.Setup(x => x.ProcessPaymentConfirmationAsync(organizationId, paymentIntentId))
                        .ReturnsAsync(false);

        // Act
        var result = await _controller.ConfirmPayment(storeSlug, paymentIntentId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Payment confirmation failed", response.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreatePaymentIntent_InvalidEmail_ReturnsBadRequest(string invalidEmail)
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = invalidEmail,
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        // Manually set ModelState to invalid for invalid email
        _controller.ModelState.AddModelError("Email", "The Email field is required.");

        // Act
        var result = await _controller.CreatePaymentIntent(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PaymentIntentDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PaymentIntentDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Invalid request data", response.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CompleteCheckout_InvalidName_ReturnsBadRequest(string invalidName)
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = invalidName,
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;

        // Manually set ModelState to invalid for invalid name
        _controller.ModelState.AddModelError("Name", "The Name field is required.");

        // Act
        var result = await _controller.CompleteCheckout(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<OrderDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OrderDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Errors);
        Assert.Contains("Invalid request data", response.Errors);
    }

    [Fact]
    public async Task CompleteCheckout_WithShippingAddress_CreatesShippingOrder()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "shipping",
            PaymentMethod = "card",
            ShippingAddress = new AddressDto
            {
                Address1 = "123 Main St",
                City = "Test City",
                State = "CA",
                Zip = "12345",
                Country = "US"
            }
        };
        var order = new OrderDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "20231122-001",
            Status = "Pending",
            FulfillmentType = "shipping",
            ShippingAmount = 10.00m,
            ShippingAddress = request.ShippingAddress,
            Items = new List<OrderItemDto>(),
            CreatedAt = DateTime.UtcNow
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.CreateOrderAsync(organizationId, request, sessionId, null))
                        .ReturnsAsync(order);

        // Act
        var result = await _controller.CompleteCheckout(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<OrderDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<OrderDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("shipping", response.Data.FulfillmentType);
        Assert.Equal(10.00m, response.Data.ShippingAmount);
        Assert.NotNull(response.Data.ShippingAddress);
    }

    [Fact]
    public async Task ValidateCheckout_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.ValidateCartForCheckoutAsync(organizationId, sessionId, null))
                        .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ValidateCheckout(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CheckoutValidationDto>>>(result);
        var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<CheckoutValidationDto>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("An error occurred validating checkout", response.Errors);
    }

    [Fact]
    public async Task CreatePaymentIntent_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var storeSlug = "test-store";
        var sessionId = "test-session-123";
        var organizationId = Guid.NewGuid();
        var request = new CheckoutRequestDto
        {
            Email = "test@example.com",
            Name = "Test Customer",
            FulfillmentType = "pickup",
            PaymentMethod = "card"
        };

        _mockOrganizationService
            .Setup(x => x.GetIdBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync(organizationId);
        _controller.HttpContext.Request.Headers["X-Session-Id"] = sessionId;
        _mockOrderService.Setup(x => x.CreatePaymentIntentAsync(organizationId, request, sessionId, null))
                        .ThrowsAsync(new Exception("Stripe service unavailable"));

        // Act
        var result = await _controller.CreatePaymentIntent(storeSlug, request);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PaymentIntentDto>>>(result);
        var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<PaymentIntentDto>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("An error occurred creating payment intent", response.Errors);
    }
}