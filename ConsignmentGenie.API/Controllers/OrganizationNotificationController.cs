using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Settings;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations/notifications")]
[Authorize(Roles = "Owner")]
public class OrganizationNotificationController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationNotificationController> _logger;

    public OrganizationNotificationController(ConsignmentGenieContext context, ILogger<OrganizationNotificationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<NotificationSettingsDto>> GetNotificationSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[NOTIFICATION_SETTINGS] Getting notification settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[NOTIFICATION_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            NotificationSettingsDto? notificationSettings = null;
            if (!string.IsNullOrEmpty(organization.NotificationSettings))
            {
                try
                {
                    notificationSettings = JsonSerializer.Deserialize<NotificationSettingsDto>(organization.NotificationSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[NOTIFICATION_SETTINGS] Failed to parse notification settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Return default settings if none exist
            notificationSettings ??= new NotificationSettingsDto
            {
                PrimaryEmail = organization.ShopEmail ?? "",
                PhoneNumber = organization.ShopPhone,
                EmailPreferences = new Dictionary<string, bool>
                {
                    ["item_sold"] = true,
                    ["daily_sales_summary"] = false,
                    ["weekly_report"] = true,
                    ["monthly_statement"] = true,
                    ["consignor_signup"] = true,
                    ["consignor_item_added"] = false,
                    ["pending_approval"] = true,
                    ["consignor_payout_ready"] = true,
                    ["high_value_sale"] = true,
                    ["low_inventory"] = true,
                    ["pricing_suggestions"] = false,
                    ["system_maintenance"] = true,
                    ["security_alerts"] = true,
                    ["account_changes"] = true,
                    ["backup_status"] = false
                },
                SmsPreferences = new Dictionary<string, bool>
                {
                    ["item_sold"] = false,
                    ["daily_sales_summary"] = false,
                    ["weekly_report"] = false,
                    ["monthly_statement"] = false,
                    ["consignor_signup"] = false,
                    ["consignor_item_added"] = false,
                    ["pending_approval"] = false,
                    ["consignor_payout_ready"] = false,
                    ["high_value_sale"] = false,
                    ["low_inventory"] = false,
                    ["pricing_suggestions"] = false,
                    ["system_maintenance"] = false,
                    ["security_alerts"] = true,
                    ["account_changes"] = true,
                    ["backup_status"] = false
                },
                Thresholds = new NotificationThresholdsDto
                {
                    HighValueSale = 500m,
                    LowInventory = 10
                }
            };

            _logger.LogDebug("[NOTIFICATION_SETTINGS] Notification settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(notificationSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION_SETTINGS] Error getting notification settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while retrieving notification settings");
        }
    }

    [HttpPatch("settings")]
    public async Task<ActionResult<NotificationSettingsDto>> UpdateNotificationSettingsPartial([FromBody] Dictionary<string, object> updates)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[NOTIFICATION_SETTINGS] Updating notification settings (partial) for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (updates == null || updates.Count == 0)
        {
            return BadRequest("No updates provided");
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[NOTIFICATION_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Get existing settings or create defaults
            NotificationSettingsDto currentSettings;
            if (!string.IsNullOrEmpty(organization.NotificationSettings))
            {
                try
                {
                    currentSettings = JsonSerializer.Deserialize<NotificationSettingsDto>(organization.NotificationSettings) ?? new NotificationSettingsDto();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[NOTIFICATION_SETTINGS] Failed to parse existing settings, using defaults");
                    currentSettings = new NotificationSettingsDto();
                }
            }
            else
            {
                currentSettings = new NotificationSettingsDto
                {
                    PrimaryEmail = organization.ShopEmail ?? "",
                    PhoneNumber = organization.ShopPhone,
                    EmailPreferences = new Dictionary<string, bool>(),
                    SmsPreferences = new Dictionary<string, bool>(),
                    Thresholds = new NotificationThresholdsDto()
                };
            }

            // Apply partial updates
            foreach (var update in updates)
            {
                var key = update.Key;
                var value = update.Value;

                try
                {
                    if (key == "PrimaryEmail" && value is string emailValue)
                    {
                        currentSettings.PrimaryEmail = emailValue;
                    }
                    else if (key == "PhoneNumber" && value is string phoneValue)
                    {
                        currentSettings.PhoneNumber = phoneValue;
                    }
                    else if (key == "HighValueSaleThreshold" && value is JsonElement thresholdElement && thresholdElement.TryGetDecimal(out var thresholdValue))
                    {
                        currentSettings.Thresholds.HighValueSale = thresholdValue;
                    }
                    else if (key == "LowInventoryThreshold" && value is JsonElement inventoryElement && inventoryElement.TryGetInt32(out var inventoryValue))
                    {
                        currentSettings.Thresholds.LowInventory = inventoryValue;
                    }
                    else if (key.StartsWith("Email_"))
                    {
                        var prefKey = key.Substring(6).ToLowerInvariant().Replace("_", "_");
                        if (value is bool boolValue)
                        {
                            currentSettings.EmailPreferences[prefKey] = boolValue;
                        }
                        else if (value is JsonElement element && element.ValueKind == JsonValueKind.True)
                        {
                            currentSettings.EmailPreferences[prefKey] = true;
                        }
                        else if (value is JsonElement element2 && element2.ValueKind == JsonValueKind.False)
                        {
                            currentSettings.EmailPreferences[prefKey] = false;
                        }
                    }
                    else if (key.StartsWith("Sms_"))
                    {
                        var prefKey = key.Substring(4).ToLowerInvariant().Replace("_", "_");
                        if (value is bool boolValue)
                        {
                            currentSettings.SmsPreferences[prefKey] = boolValue;
                        }
                        else if (value is JsonElement element && element.ValueKind == JsonValueKind.True)
                        {
                            currentSettings.SmsPreferences[prefKey] = true;
                        }
                        else if (value is JsonElement element2 && element2.ValueKind == JsonValueKind.False)
                        {
                            currentSettings.SmsPreferences[prefKey] = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[NOTIFICATION_SETTINGS] Failed to apply update for key {Key}", key);
                }
            }

            // Save updated settings
            organization.NotificationSettings = JsonSerializer.Serialize(currentSettings);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[NOTIFICATION_SETTINGS] Notification settings updated for organization {OrganizationId}", organizationId);
            return Ok(currentSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION_SETTINGS] Error updating notification settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while updating notification settings");
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