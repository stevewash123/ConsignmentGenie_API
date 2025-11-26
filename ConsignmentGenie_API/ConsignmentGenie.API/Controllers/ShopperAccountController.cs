using System.Security.Claims;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/shop/{storeSlug}/account")]
[Authorize(Roles = "Customer")] // Customer role represents Shoppers
public class ShopperAccountController : ControllerBase
{
    private readonly IShopperAuthService _shopperAuthService;
    private readonly ILogger<ShopperAccountController> _logger;

    public ShopperAccountController(IShopperAuthService shopperAuthService, ILogger<ShopperAccountController> logger)
    {
        _shopperAuthService = shopperAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Get shopper profile
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Shopper profile</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ShopperProfileDto>>> GetProfile(string storeSlug)
    {
        try
        {
            var userId = GetUserId();
            var organizationId = GetOrganizationId();
            var tokenSlug = GetSlug();

            // Verify the store slug in the URL matches the token
            if (!string.Equals(storeSlug, tokenSlug, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<ShopperProfileDto>.ErrorResult("Invalid store access"));
            }

            var profile = await _shopperAuthService.GetShopperProfileAsync(userId, organizationId);

            if (profile == null)
            {
                return NotFound(ApiResponse<ShopperProfileDto>.ErrorResult("Shopper profile not found"));
            }

            return Ok(ApiResponse<ShopperProfileDto>.SuccessResult(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shopper profile for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<ShopperProfileDto>.ErrorResult("An error occurred retrieving profile"));
        }
    }

    /// <summary>
    /// Update shopper profile
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated shopper profile</returns>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<ShopperProfileDto>>> UpdateProfile(
        string storeSlug,
        [FromBody] UpdateShopperProfileRequest request)
    {
        try
        {
            var userId = GetUserId();
            var organizationId = GetOrganizationId();
            var tokenSlug = GetSlug();

            // Verify the store slug in the URL matches the token
            if (!string.Equals(storeSlug, tokenSlug, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<ShopperProfileDto>.ErrorResult("Invalid store access"));
            }

            var updatedProfile = await _shopperAuthService.UpdateShopperProfileAsync(userId, organizationId, request);

            if (updatedProfile == null)
            {
                return NotFound(ApiResponse<ShopperProfileDto>.ErrorResult("Shopper profile not found"));
            }

            _logger.LogInformation("Shopper profile updated for store {Slug}, User {UserId}", storeSlug, userId);

            return Ok(ApiResponse<ShopperProfileDto>.SuccessResult(updatedProfile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shopper profile for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<ShopperProfileDto>.ErrorResult("An error occurred updating profile"));
        }
    }

    /// <summary>
    /// Change password
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Password change request</param>
    /// <returns>Success indicator</returns>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<PasswordChangeResponseDto>>> ChangePassword(
        string storeSlug,
        [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetUserId();
            var tokenSlug = GetSlug();

            // Verify the store slug in the URL matches the token
            if (!string.Equals(storeSlug, tokenSlug, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<PasswordChangeResponseDto>.ErrorResult("Invalid store access"));
            }

            var success = await _shopperAuthService.ChangePasswordAsync(userId, request);

            if (!success)
            {
                return BadRequest(ApiResponse<PasswordChangeResponseDto>.ErrorResult("Current password is incorrect"));
            }

            _logger.LogInformation("Password changed for shopper in store {Slug}, User {UserId}", storeSlug, userId);

            var response = new PasswordChangeResponseDto { Message = "Password changed successfully" };
            return Ok(ApiResponse<PasswordChangeResponseDto>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for shopper in store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<PasswordChangeResponseDto>.ErrorResult("An error occurred changing password"));
        }
    }

    /// <summary>
    /// Get order history - placeholder for Phase 2
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged order history</returns>
    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<PagedResult<object>>>> GetOrders(
        string storeSlug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var tokenSlug = GetSlug();

            // Verify the store slug in the URL matches the token
            if (!string.Equals(storeSlug, tokenSlug, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<PagedResult<object>>.ErrorResult("Invalid store access"));
            }

            // TODO: Implement order history retrieval in Phase 3
            var emptyResult = new PagedResult<object>
            {
                Items = new List<object>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<object>>.SuccessResult(emptyResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order history for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<PagedResult<object>>.ErrorResult("An error occurred retrieving order history"));
        }
    }

    /// <summary>
    /// Get single order detail - placeholder for Phase 2
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="orderId">Order ID</param>
    /// <returns>Order detail</returns>
    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<ApiResponse<OrderDetailResponseDto>>> GetOrder(string storeSlug, Guid orderId)
    {
        try
        {
            var tokenSlug = GetSlug();

            // Verify the store slug in the URL matches the token
            if (!string.Equals(storeSlug, tokenSlug, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<OrderDetailResponseDto>.ErrorResult("Invalid store access"));
            }

            // TODO: Implement order detail retrieval in Phase 3
            return NotFound(ApiResponse<OrderDetailResponseDto>.ErrorResult("Order not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId} for store {Slug}", orderId, storeSlug);
            return StatusCode(500, ApiResponse<OrderDetailResponseDto>.ErrorResult("An error occurred retrieving order"));
        }
    }

    #region Private Helper Methods

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(organizationIdClaim) || !Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            throw new UnauthorizedAccessException("Invalid organization ID in token");
        }
        return organizationId;
    }

    private string GetSlug()
    {
        var storeSlugClaim = User.FindFirst("Slug")?.Value;
        if (string.IsNullOrEmpty(storeSlugClaim))
        {
            throw new UnauthorizedAccessException("Invalid store slug in token");
        }
        return storeSlugClaim;
    }

    #endregion
}