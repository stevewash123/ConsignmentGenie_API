using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers.Settings;

[ApiController]
[Route("api/owner/settings/account")]
[Authorize(Roles = "Owner")]
public class OrganizationAccountSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationAccountSettingsController> _logger;

    public OrganizationAccountSettingsController(ConsignmentGenieContext context, ILogger<OrganizationAccountSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("billing-subscription")]
    public async Task<ActionResult> GetBillingSubscription()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[ACCOUNT_SETTINGS] Getting billing & subscription info for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[ACCOUNT_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Return billing/subscription info - would be extracted from Organization entity or Subscription table
            var billing = new
            {
                SubscriptionStatus = "active", // Default - would come from entity
                SubscriptionPlan = "basic",
                BillingCycle = "monthly",
                NextBillingDate = DateTime.UtcNow.AddMonths(1),
                // Add other billing-related fields as needed
            };

            return Ok(billing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACCOUNT_SETTINGS] Error getting billing & subscription for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("billing-subscription")]
    public async Task<ActionResult> UpdateBillingSubscription([FromBody] dynamic billing)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[ACCOUNT_SETTINGS] Updating billing & subscription for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[ACCOUNT_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Update billing/subscription-related fields
            organization.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Billing & subscription updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACCOUNT_SETTINGS] Error updating billing & subscription for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("owner-contact-information")]
    public async Task<ActionResult> GetOwnerContactInformation()
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        _logger.LogInformation("[ACCOUNT_SETTINGS] Getting owner contact info for organization {OrganizationId}, user {UserId}", organizationId, userId);

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);

            if (user == null)
            {
                _logger.LogWarning("[ACCOUNT_SETTINGS] User {UserId} not found in organization {OrganizationId}", userId, organizationId);
                return NotFound("User not found");
            }

            // Return owner contact information
            var contactInfo = new
            {
                FirstName = user.Name,
                LastName = user,
                Email = user.Email,
                Phone = user.Phone,
                // Add other contact-related fields as needed
            };

            return Ok(contactInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACCOUNT_SETTINGS] Error getting owner contact info for organization {OrganizationId}, user {UserId}", organizationId, userId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("owner-contact-information")]
    public async Task<ActionResult> UpdateOwnerContactInformation([FromBody] dynamic contactInfo)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();
        _logger.LogInformation("[ACCOUNT_SETTINGS] Updating owner contact info for organization {OrganizationId}, user {UserId}", organizationId, userId);

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);

            if (user == null)
            {
                _logger.LogWarning("[ACCOUNT_SETTINGS] User {UserId} not found in organization {OrganizationId}", userId, organizationId);
                return NotFound("User not found");
            }

            // Update contact information fields
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Owner contact information updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACCOUNT_SETTINGS] Error updating owner contact info for organization {OrganizationId}, user {UserId}", organizationId, userId);
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var finalValue = userIdClaim ?? nameIdentifierClaim;

        if (Guid.TryParse(finalValue, out var userId))
            return userId;

        throw new UnauthorizedAccessException("User ID not found in token");
    }
}