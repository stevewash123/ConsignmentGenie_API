using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/storefront/{storeSlug}/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;
    private readonly IOrganizationService _organizationService;

    public CartController(ICartService cartService, ILogger<CartController> logger, IOrganizationService organizationService)
    {
        _cartService = cartService;
        _logger = logger;
        _organizationService = organizationService;
    }

    /// <summary>
    /// Get cart contents
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Cart contents</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart(string storeSlug)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<CartDto>.ErrorResult("Store not found"));
            }

            var (sessionId, customerId) = GetSessionAndCustomerIds();
            var cart = await _cartService.GetCartAsync(organizationId.Value, sessionId, customerId);

            return Ok(ApiResponse<CartDto>.SuccessResult(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<CartDto>.ErrorResult("An error occurred retrieving cart"));
        }
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Add to cart request</param>
    /// <returns>Updated cart contents</returns>
    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddItemToCart(string storeSlug, [FromBody] AddToCartRequest request)
    {
        try
        {
            if (request.ItemId == Guid.Empty)
            {
                return BadRequest(ApiResponse<CartDto>.ErrorResult("Item ID is required"));
            }

            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<CartDto>.ErrorResult("Store not found"));
            }

            var (sessionId, customerId) = GetSessionAndCustomerIds();
            var cart = await _cartService.AddItemToCartAsync(organizationId.Value, request.ItemId, sessionId, customerId);

            return Ok(ApiResponse<CartDto>.SuccessResult(cart));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CartDto>.ErrorResult(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CartDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item {ItemId} to cart for store {Slug}", request.ItemId, storeSlug);
            return StatusCode(500, ApiResponse<CartDto>.ErrorResult("An error occurred adding item to cart"));
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="itemId">Item ID to remove</param>
    /// <returns>Updated cart contents</returns>
    [HttpDelete("items/{itemId}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveItemFromCart(string storeSlug, Guid itemId)
    {
        try
        {
            if (itemId == Guid.Empty)
            {
                return BadRequest(ApiResponse<CartDto>.ErrorResult("Item ID is required"));
            }

            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<CartDto>.ErrorResult("Store not found"));
            }

            var (sessionId, customerId) = GetSessionAndCustomerIds();
            var cart = await _cartService.RemoveItemFromCartAsync(organizationId.Value, itemId, sessionId, customerId);

            return Ok(ApiResponse<CartDto>.SuccessResult(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from cart for store {Slug}", itemId, storeSlug);
            return StatusCode(500, ApiResponse<CartDto>.ErrorResult("An error occurred removing item from cart"));
        }
    }

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Empty cart</returns>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<bool>>> ClearCart(string storeSlug)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Store not found"));
            }

            var (sessionId, customerId) = GetSessionAndCustomerIds();
            var result = await _cartService.ClearCartAsync(organizationId.Value, sessionId, customerId);

            return Ok(ApiResponse<bool>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred clearing cart"));
        }
    }

    /// <summary>
    /// Merge anonymous cart with user account cart (requires authentication)
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Merged cart contents</returns>
    [HttpPost("merge")]
    public async Task<ActionResult<ApiResponse<CartDto>>> MergeCart(string storeSlug)
    {
        try
        {
            var organizationId = await _organizationService.GetIdBySlugAsync(storeSlug);
            if (organizationId == null)
            {
                return NotFound(ApiResponse<CartDto>.ErrorResult("Store not found"));
            }

            // Get session ID from headers/cookies
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(ApiResponse<CartDto>.ErrorResult("Session ID required for cart merge"));
            }

            // Get customer ID from JWT token (requires authentication)
            var customerId = GetCustomerId();
            if (!customerId.HasValue)
            {
                return Unauthorized(ApiResponse<CartDto>.ErrorResult("Authentication required for cart merge"));
            }

            var cart = await _cartService.MergeCartAsync(organizationId.Value, sessionId, customerId.Value);

            return Ok(ApiResponse<CartDto>.SuccessResult(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging cart for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<CartDto>.ErrorResult("An error occurred merging cart"));
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