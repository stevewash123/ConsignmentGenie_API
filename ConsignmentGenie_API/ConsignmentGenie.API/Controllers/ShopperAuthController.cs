using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/shop/{storeSlug}/auth")]
public class ShopperAuthController : ControllerBase
{
    private readonly IShopperAuthService _shopperAuthService;
    private readonly ILogger<ShopperAuthController> _logger;

    public ShopperAuthController(IShopperAuthService shopperAuthService, ILogger<ShopperAuthController> logger)
    {
        _shopperAuthService = shopperAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Register new shopper (no approval required)
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Registration request</param>
    /// <returns>Authentication result with token and profile</returns>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Register(
        string storeSlug,
        [FromBody] ShopperRegisterRequest request)
    {
        try
        {
            var result = await _shopperAuthService.RegisterAsync(request, storeSlug);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<AuthResultDto>.ErrorResult(result.ErrorMessage ?? "Registration failed"));
            }

            _logger.LogInformation("Shopper registered successfully for store {StoreSlug}: {Email}",
                storeSlug, request.Email);

            return Ok(ApiResponse<AuthResultDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering shopper for store {StoreSlug}: {Email}",
                storeSlug, request.Email);
            return StatusCode(500, ApiResponse<AuthResultDto>.ErrorResult("An error occurred during registration"));
        }
    }

    /// <summary>
    /// Login existing shopper
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Login request</param>
    /// <returns>Authentication result with token and profile</returns>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResultDto>>> Login(
        string storeSlug,
        [FromBody] ShopperLoginRequest request)
    {
        try
        {
            var result = await _shopperAuthService.LoginAsync(request, storeSlug);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<AuthResultDto>.ErrorResult(result.ErrorMessage ?? "Login failed"));
            }

            _logger.LogInformation("Shopper logged in successfully for store {StoreSlug}: {Email}",
                storeSlug, request.Email);

            return Ok(ApiResponse<AuthResultDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shopper login for store {StoreSlug}: {Email}",
                storeSlug, request.Email);
            return StatusCode(500, ApiResponse<AuthResultDto>.ErrorResult("An error occurred during login"));
        }
    }

    /// <summary>
    /// Guest checkout session
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Guest session request</param>
    /// <returns>Guest session token and expiry</returns>
    [HttpPost("guest")]
    public async Task<ActionResult<ApiResponse<GuestSessionDto>>> CreateGuestSession(
        string storeSlug,
        [FromBody] GuestSessionRequest request)
    {
        try
        {
            var result = await _shopperAuthService.CreateGuestSessionAsync(request, storeSlug);

            _logger.LogInformation("Guest session created for store {StoreSlug}: {Email}",
                storeSlug, request.Email);

            return Ok(ApiResponse<GuestSessionDto>.SuccessResult(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid store slug provided: {StoreSlug}", storeSlug);
            return BadRequest(ApiResponse<GuestSessionDto>.ErrorResult("Store not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest session for store {StoreSlug}: {Email}",
                storeSlug, request.Email);
            return StatusCode(500, ApiResponse<GuestSessionDto>.ErrorResult("An error occurred creating guest session"));
        }
    }

    /// <summary>
    /// Forgot password - initiate password reset
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Forgot password request</param>
    /// <returns>Success indicator</returns>
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        string storeSlug,
        [FromBody] ForgotPasswordRequest request)
    {
        try
        {
            // TODO: Implement forgot password functionality
            // This would typically:
            // 1. Find the shopper by email and store
            // 2. Generate a password reset token
            // 3. Send password reset email
            // 4. Return success (don't reveal if email exists for security)

            _logger.LogInformation("Password reset requested for store {StoreSlug}: {Email}",
                storeSlug, request.Email);

            // For now, return success without actual implementation
            return Ok(ApiResponse<object>.SuccessResult(new { message = "Password reset email sent if account exists" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password for store {StoreSlug}: {Email}",
                storeSlug, request.Email);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred processing password reset"));
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="request">Reset password request</param>
    /// <returns>Success indicator</returns>
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        string storeSlug,
        [FromBody] ResetPasswordRequest request)
    {
        try
        {
            // TODO: Implement password reset functionality
            // This would typically:
            // 1. Validate the reset token
            // 2. Find the user by email and token
            // 3. Update the password
            // 4. Invalidate the reset token

            _logger.LogInformation("Password reset attempted for store {StoreSlug}: {Email}",
                storeSlug, request.Email);

            // For now, return success without actual implementation
            return Ok(ApiResponse<object>.SuccessResult(new { message = "Password reset successfully" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for store {StoreSlug}: {Email}",
                storeSlug, request.Email);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred resetting password"));
        }
    }
}