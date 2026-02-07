using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;

namespace ConsignmentGenie.API.Controllers.Settings;

[ApiController]
[Route("api/owner/settings/notifications")]
[Authorize(Roles = "Owner")]
public class OrganizationNotificationSettingsController : ControllerBase
{
    private readonly ILogger<OrganizationNotificationSettingsController> _logger;
    private readonly IOrganizationSettingsManager _organizationSettingsManager;

    public OrganizationNotificationSettingsController(
        ILogger<OrganizationNotificationSettingsController> logger,
        IOrganizationSettingsManager organizationSettingsManager)
    {
        _logger = logger;
        _organizationSettingsManager = organizationSettingsManager;
    }

    [HttpGet]
    public async Task<ActionResult<OrganizationNotificationSettingsDto>> GetNotificationSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[NOTIFICATION_SETTINGS] Getting notification settings for organization {OrganizationId}", organizationId);

        try
        {
            var settings = await _organizationSettingsManager.GetPageSettingsAsync<OrganizationNotificationSettingsDto>(organizationId, SettingsPage.Notifications);

            _logger.LogDebug("[NOTIFICATION_SETTINGS] Notification settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION_SETTINGS] Error getting notification settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut]
    public async Task<ActionResult> UpdateNotificationSettings([FromBody] OrganizationNotificationSettingsDto settings)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[NOTIFICATION_SETTINGS] Updating notification settings for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _organizationSettingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Notifications, settings);

            _logger.LogInformation("[NOTIFICATION_SETTINGS] Notification settings updated successfully for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, message = "Notification settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION_SETTINGS] Error updating notification settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch]
    public async Task<ActionResult> PatchNotificationSettings([FromBody] UpdateOrganizationNotificationSettingsRequest changes)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[NOTIFICATION_SETTINGS] Patching notification settings for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Get current notification settings
            var currentSettings = await _organizationSettingsManager.GetPageSettingsAsync<OrganizationNotificationSettingsDto>(organizationId, SettingsPage.Notifications);

            // Apply changes using the matrix structure
            if (changes.ContactInfo != null)
            {
                if (changes.ContactInfo.PrimaryEmailAddress != null)
                    currentSettings.ContactInfo.PrimaryEmailAddress = changes.ContactInfo.PrimaryEmailAddress;
                if (changes.ContactInfo.PhoneNumber != null)
                    currentSettings.ContactInfo.PhoneNumber = changes.ContactInfo.PhoneNumber;
            }

            if (changes.NotificationPreferences != null)
            {
                currentSettings.NotificationPreferences = changes.NotificationPreferences;
            }

            if (changes.Thresholds != null)
            {
                if (changes.Thresholds.HighValueSaleThreshold.HasValue)
                    currentSettings.Thresholds.HighValueSaleThreshold = changes.Thresholds.HighValueSaleThreshold;
                if (changes.Thresholds.LowInventoryAlertThreshold.HasValue)
                    currentSettings.Thresholds.LowInventoryAlertThreshold = changes.Thresholds.LowInventoryAlertThreshold;
            }

            // Save updated settings
            await _organizationSettingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Notifications, currentSettings);

            _logger.LogInformation("[NOTIFICATION_SETTINGS] Notification settings partially updated for organization {OrganizationId}", organizationId);
            return Ok(new { success = true, data = currentSettings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION_SETTINGS] Error patching notification settings for organization {OrganizationId}", organizationId);
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
}