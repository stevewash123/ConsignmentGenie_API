using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/storefront/{storeSlug}/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<CheckoutController> _logger;
    private readonly IOrganizationService _organizationService;

    public CheckoutController(IOrderService orderService, ILogger<CheckoutController> logger, IOrganizationService organizationService)
    {
        _orderService = orderService;
        _logger = logger;
        _organizationService = organizationService;
    }

    /// <summary>
    /// Validate cart for checkout
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<CheckoutValidationDto>>> ValidateCheckout(string storeSlug)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<CheckoutValidationDto>.ErrorResult("Store not found"));
            }

            var (sessionId, customerId) = GetSessionAndCustomerIds();
            var validation = await _orderService.ValidateCartForCheckoutAsync(organizationId.Value, sessionId, customerId);

            return Ok(ApiResponse<CheckoutValidationDto>.SuccessResult(validation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating checkout for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<CheckoutValidationDto>.ErrorResult("An error occurred validating checkout"));
        }
    }

    /// <summary>
    /// Create payment intent
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Checkout request</param>
    /// <returns>Payment intent information</returns>
    [HttpPost("payment-intent")]
    public async Task<ActionResult<ApiResponse<PaymentIntentDto>>> CreatePaymentIntent(string storeSlug, [FromBody] CheckoutRequestDto request)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<PaymentIntentDto>.ErrorResult("Store not found"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<PaymentIntentDto>.ErrorResult("Invalid request data"));
            }

            var (sessionId, customerId) = GetSessionAndCustomerIds();
            var paymentIntent = await _orderService.CreatePaymentIntentAsync(organizationId.Value, request, sessionId, customerId);

            return Ok(ApiResponse<PaymentIntentDto>.SuccessResult(paymentIntent));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PaymentIntentDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<PaymentIntentDto>.ErrorResult("An error occurred creating payment intent"));
        }
    }

    /// <summary>
    /// Complete checkout and create order
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Checkout request</param>
    /// <returns>Created order</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CompleteCheckout(string storeSlug, [FromBody] CheckoutRequestDto request)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<OrderDto>.ErrorResult("Store not found"));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OrderDto>.ErrorResult("Invalid request data"));
            }

            // Validate fulfillment type and address
            if (request.FulfillmentType == "shipping" && request.ShippingAddress == null)
            {
                return BadRequest(ApiResponse<OrderDto>.ErrorResult("Shipping address is required for shipping orders"));
            }

            var (sessionId, customerId) = GetSessionAndCustomerIds();
            var order = await _orderService.CreateOrderAsync(organizationId.Value, request, sessionId, customerId);

            return Ok(ApiResponse<OrderDto>.SuccessResult(order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OrderDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing checkout for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred completing checkout"));
        }
    }

    /// <summary>
    /// Get order details
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="orderId">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(string storeSlug, Guid orderId)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<OrderDto>.ErrorResult("Store not found"));
            }

            var order = await _orderService.GetOrderAsync(organizationId.Value, orderId);

            if (order == null)
            {
                return NotFound(ApiResponse<OrderDto>.ErrorResult("Order not found"));
            }

            return Ok(ApiResponse<OrderDto>.SuccessResult(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId} for store {Slug}", orderId, storeSlug);
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResult("An error occurred retrieving order"));
        }
    }

    /// <summary>
    /// Get customer orders (requires authentication)
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Customer order history</returns>
    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<List<OrderSummaryDto>>>> GetOrders(
        string storeSlug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<List<OrderSummaryDto>>.ErrorResult("Store not found"));
            }

            var customerId = GetCustomerId();
            if (!customerId.HasValue)
            {
                return Unauthorized(ApiResponse<List<OrderSummaryDto>>.ErrorResult("Authentication required"));
            }

            var orders = await _orderService.GetOrdersAsync(organizationId.Value, customerId.Value, page, pageSize);

            return Ok(ApiResponse<List<OrderSummaryDto>>.SuccessResult(orders));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<List<OrderSummaryDto>>.ErrorResult("An error occurred retrieving orders"));
        }
    }

    /// <summary>
    /// Handle payment confirmation webhook (for Stripe)
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="paymentIntentId">Payment intent ID from Stripe</param>
    /// <returns>Success status</returns>
    [HttpPost("payment-confirmation")]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmPayment(string storeSlug, [FromBody] string paymentIntentId)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Store not found"));
            }

            var success = await _orderService.ProcessPaymentConfirmationAsync(organizationId.Value, paymentIntentId);

            if (!success)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Payment confirmation failed"));
            }

            return Ok(ApiResponse<bool>.SuccessResult(success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment confirmation for store {Slug}, payment intent {PaymentIntentId}", storeSlug, paymentIntentId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred processing payment confirmation"));
        }
    }


    private (string? sessionId, Guid? customerId) GetSessionAndCustomerIds()
    {
        var sessionId = GetSessionId();
        var customerId = GetCustomerId();
        return (sessionId, customerId);
    }

    private string? GetSessionId()
    {
        // Try to get session ID from X-Session-Id header first, then from cookies
        if (Request.Headers.TryGetValue("X-Session-Id", out var headerSessionId))
        {
            return headerSessionId.FirstOrDefault();
        }

        if (Request.Cookies.TryGetValue("sessionId", out var cookieSessionId))
        {
            return cookieSessionId;
        }

        return null;
    }

    private Guid? GetCustomerId()
    {
        // TODO: Extract customer ID from JWT token claims
        // This would require authentication middleware to be set up
        // For now, return null (anonymous user)
        return null;
    }
}