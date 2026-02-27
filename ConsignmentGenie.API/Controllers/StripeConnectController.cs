using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConsignmentGenie.Application.Services.Interfaces;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/stripe-connect")]
[Authorize]
public class StripeConnectController : ControllerBase
{
    private readonly IStripeConnectService _stripeConnectService;
    private readonly ILogger<StripeConnectController> _logger;

    public StripeConnectController(
        IStripeConnectService stripeConnectService,
        ILogger<StripeConnectController> logger)
    {
        _stripeConnectService = stripeConnectService;
        _logger = logger;
    }

    /// <summary>
    /// Generate Stripe Connect OAuth link for the organization
    /// </summary>
    [HttpGet("connect-link")]
    public async Task<ActionResult<object>> GetConnectLink()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var connectUrl = await _stripeConnectService.GenerateConnectLinkAsync(organizationId);

            return Ok(new { success = true, data = new { connectUrl } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Stripe Connect link");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Handle Stripe Connect OAuth callback (no auth required for callback)
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleCallback([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Stripe Connect callback received error: {Error}", error);
                return Redirect($"{Request.Scheme}://{Request.Host}/owner/settings/integrations?stripe_error={error}");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("Stripe Connect callback missing required parameters");
                return Redirect($"{Request.Scheme}://{Request.Host}/owner/settings/integrations?stripe_error=missing_parameters");
            }

            var success = await _stripeConnectService.HandleConnectCallbackAsync(code, state);

            if (success)
            {
                return Redirect($"{Request.Scheme}://{Request.Host}/owner/settings/integrations?stripe_success=true");
            }
            else
            {
                return Redirect($"{Request.Scheme}://{Request.Host}/owner/settings/integrations?stripe_error=connection_failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe Connect callback");
            return Redirect($"{Request.Scheme}://{Request.Host}/owner/settings/integrations?stripe_error=internal_error");
        }
    }

    /// <summary>
    /// Get current Stripe Connect account status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetConnectionStatus()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var status = await _stripeConnectService.GetAccountStatusAsync(organizationId);

            return Ok(new { success = true, data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe Connect status");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Disconnect Stripe Connect account
    /// </summary>
    [HttpPost("disconnect")]
    public async Task<ActionResult<object>> DisconnectAccount()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var success = await _stripeConnectService.DisconnectAccountAsync(organizationId);

            if (success)
            {
                return Ok(new { success = true, message = "Stripe Connect account disconnected successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Failed to disconnect Stripe Connect account" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting Stripe Connect account");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgId))
            return orgId;
        throw new UnauthorizedAccessException("Organization ID not found in token");
    }
}