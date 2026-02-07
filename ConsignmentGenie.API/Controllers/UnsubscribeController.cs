using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

/// <summary>
/// Public controller for email unsubscribe functionality
/// No authentication required - accessed via unsubscribe links in emails
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UnsubscribeController : ControllerBase
{
    private readonly IEmailComplianceService _emailComplianceService;
    private readonly ILogger<UnsubscribeController> _logger;

    public UnsubscribeController(
        IEmailComplianceService emailComplianceService,
        ILogger<UnsubscribeController> logger)
    {
        _emailComplianceService = emailComplianceService;
        _logger = logger;
    }

    /// <summary>
    /// Displays unsubscribe page for user to confirm their unsubscribe preferences
    /// GET /api/unsubscribe?token={token}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUnsubscribePage([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { error = "Unsubscribe token is required" });
        }

        try
        {
            var tokenInfo = await _emailComplianceService.ValidateUnsubscribeTokenAsync(token);
            if (tokenInfo == null)
            {
                return BadRequest(new { error = "Invalid unsubscribe token" });
            }

            if (tokenInfo.IsExpired)
            {
                return BadRequest(new { error = "Unsubscribe token has expired. Please contact support for assistance." });
            }

            // Return token information so the frontend can display appropriate options
            var response = new
            {
                isValid = true,
                userId = tokenInfo.UserId,
                role = tokenInfo.Role,
                notificationType = tokenInfo.NotificationType,
                expiresAt = tokenInfo.ExpiresAt,
                token = token
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating unsubscribe token");
            return StatusCode(500, new { error = "An error occurred while processing your unsubscribe request" });
        }
    }

    /// <summary>
    /// Processes unsubscribe request
    /// POST /api/unsubscribe
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ProcessUnsubscribe([FromBody] UnsubscribeRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { error = "Unsubscribe token is required" });
        }

        try
        {
            var result = await _emailComplianceService.ProcessUnsubscribeAsync(request.Token, request.Scope);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Successful unsubscribe: Token validated, scope {Scope}, {Count} notification types updated",
                request.Scope, result.UpdatedNotificationTypes.Count);

            return Ok(new
            {
                success = true,
                message = result.Message,
                scope = result.ProcessedScope.ToString(),
                updatedNotificationTypes = result.UpdatedNotificationTypes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unsubscribe request");
            return StatusCode(500, new { error = "An error occurred while processing your unsubscribe request" });
        }
    }

    /// <summary>
    /// One-click unsubscribe for email clients that support it (RFC 8058)
    /// POST /api/unsubscribe/one-click
    /// </summary>
    [HttpPost("one-click")]
    public async Task<IActionResult> OneClickUnsubscribe([FromForm] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest();
        }

        try
        {
            // For one-click unsubscribe, use specific notification scope by default
            var result = await _emailComplianceService.ProcessUnsubscribeAsync(token, UnsubscribeScope.SpecificNotification);

            if (result.Success)
            {
                _logger.LogInformation("Successful one-click unsubscribe for token");
                return Ok();
            }
            else
            {
                _logger.LogWarning("One-click unsubscribe failed: {Message}", result.Message);
                return BadRequest();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing one-click unsubscribe");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Health check endpoint to verify unsubscribe service is working
    /// GET /api/unsubscribe/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            service = "Email Unsubscribe Service",
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Request model for unsubscribe processing
/// </summary>
public class UnsubscribeRequest
{
    /// <summary>
    /// The unsubscribe token from the email
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Scope of unsubscribe operation
    /// </summary>
    public UnsubscribeScope Scope { get; set; } = UnsubscribeScope.SpecificNotification;
}