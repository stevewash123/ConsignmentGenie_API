using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Owner,Clerk,Consignor")]
public class UnifiedSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<UnifiedSettingsController> _logger;

    public UnifiedSettingsController(ConsignmentGenieContext context, ILogger<UnifiedSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get unified settings summary for efficient frontend caching
    /// </summary>
    /// <param name="include">Optional comma-separated list of sections to include (e.g., "consignor,business")</param>
    /// <returns>Settings summary with requested sections</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<SettingsSummaryDto>> GetSettingsSummary(
        [FromQuery] string? include = null)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[UNIFIED_SETTINGS] Getting settings summary for organization {OrganizationId}, include: {Include}", organizationId, include ?? "all");

        try
        {
            var sections = string.IsNullOrEmpty(include)
                ? null // null = load all
                : include.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => s.Trim().ToLower())
                         .ToHashSet();

            var summary = new SettingsSummaryDto();

            // Load consignor settings if requested
            if (sections == null || sections.Contains("consignor"))
            {
                summary.Consignor = await GetConsignorSettings(organizationId);
            }

            // Load business settings if requested
            if (sections == null || sections.Contains("business"))
            {
                summary.Business = await GetBusinessSettings(organizationId);
            }

            // Load notification settings if requested
            if (sections == null || sections.Contains("notifications"))
            {
                summary.Notifications = await GetNotificationSettings(organizationId);
            }

            // Load payment settings if requested
            if (sections == null || sections.Contains("payment"))
            {
                summary.Payment = await GetPaymentSettings(organizationId);
            }

            // Load profile settings if requested
            if (sections == null || sections.Contains("profile"))
            {
                summary.Profile = await GetProfileSettings(organizationId);
            }

            // Load sales settings if requested
            if (sections == null || sections.Contains("sales"))
            {
                summary.Sales = await GetSalesSettings(organizationId);
            }

            // Load bookkeeping settings if requested
            if (sections == null || sections.Contains("bookkeeping"))
            {
                summary.Bookkeeping = await GetBookkeepingSettings(organizationId);
            }

            // Load account settings if requested
            if (sections == null || sections.Contains("account"))
            {
                summary.Account = await GetAccountSettings(organizationId);
            }

            _logger.LogDebug("[UNIFIED_SETTINGS] Successfully loaded settings summary for organization {OrganizationId}", organizationId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UNIFIED_SETTINGS] Error getting settings summary for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while retrieving settings summary");
        }
    }

    #region Private Methods

    private async Task<ConsignorSettingsDto?> GetConsignorSettings(Guid organizationId)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null) return null;

            var settings = new ConsignorSettingsDto();

            // Get onboarding settings
            settings.Onboarding = await GetConsignorOnboardingSettings(organization);

            // Get defaults settings
            settings.Defaults = await GetConsignorDefaultsSettings(organization);

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UNIFIED_SETTINGS] Error loading consignor settings for organization {OrganizationId}", organizationId);
            return null;
        }
    }

    private async Task<ConsignorOnboardingDto> GetConsignorOnboardingSettings(Organization organization)
    {
        // Reuse logic from OrganizationConsignorSettingsController
        ConsignorOnboardingDto settings;
        if (!string.IsNullOrEmpty(organization.ConsignorSettings))
        {
            try
            {
                var fullSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorSettings);
                if (fullSettings?.ContainsKey("onboarding") == true)
                {
                    var onboardingJson = JsonSerializer.Serialize(fullSettings["onboarding"]);
                    settings = JsonSerializer.Deserialize<ConsignorOnboardingDto>(onboardingJson) ?? CreateDefaultConsignorOnboardingSettings();
                }
                else
                {
                    settings = CreateDefaultConsignorOnboardingSettings();
                }
            }
            catch (JsonException)
            {
                settings = CreateDefaultConsignorOnboardingSettings();
            }
        }
        else
        {
            settings = CreateDefaultConsignorOnboardingSettings();
        }

        // Sync with Organization entity fields for backwards compatibility
        if (!string.IsNullOrEmpty(organization.AgreementMethod))
        {
            settings.AgreementRequirement = organization.AgreementMethod.ToLower() switch
            {
                "acknowledge" => AgreementRequirement.Acknowledge,
                "upload" => AgreementRequirement.Upload,
                _ => AgreementRequirement.Upload
            };
        }

        if (!string.IsNullOrEmpty(organization.AgreementTemplateCloudinaryUrl))
        {
            settings.AgreementTemplateId = Guid.NewGuid(); // Generate a temporary GUID for backwards compatibility
        }

        if (!string.IsNullOrEmpty(organization.AcknowledgeTermsText))
        {
            settings.AcknowledgeTermsText = organization.AcknowledgeTermsText;
        }

        settings.ApprovalMode = organization.ApprovalMode;

        return settings;
    }

    private async Task<ConsignorDefaultsDto> GetConsignorDefaultsSettings(Organization organization)
    {
        // Reuse logic from OrganizationConsignorSettingsController
        ConsignorDefaultsDto defaults;

        if (!string.IsNullOrEmpty(organization.ConsignorSettings))
        {
            try
            {
                var consignorSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorSettings);

                if (consignorSettings != null && consignorSettings.ContainsKey("defaults"))
                {
                    var defaultsJson = consignorSettings["defaults"].ToString();
                    defaults = JsonSerializer.Deserialize<ConsignorDefaultsDto>(defaultsJson!) ?? CreateDefaultConsignorDefaults(organization);
                }
                else
                {
                    defaults = CreateDefaultConsignorDefaults(organization);
                }
            }
            catch (JsonException)
            {
                defaults = CreateDefaultConsignorDefaults(organization);
            }
        }
        else
        {
            defaults = CreateDefaultConsignorDefaults(organization);
        }

        return defaults;
    }

    private static ConsignorOnboardingDto CreateDefaultConsignorOnboardingSettings()
    {
        return new ConsignorOnboardingDto
        {
            AgreementRequirement = AgreementRequirement.Upload,
            AgreementTemplateId = null,
            AcknowledgeTermsText = null,
            ApprovalMode = ApprovalMode.Manual,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static ConsignorDefaultsDto CreateDefaultConsignorDefaults(Organization organization)
    {
        return new ConsignorDefaultsDto
        {
            ShopCommissionPercent = 100M - organization.DefaultSplitPercentage,  // Convert consignor percentage to shop percentage
            ConsignmentPeriodDays = 90,
            RetrievalPeriodDays = 14,
            UnsoldItemPolicy = "return-to-consignor",
            LastUpdated = DateTime.UtcNow
        };
    }

    // Placeholder methods for other settings sections
    // These will be implemented as the respective controllers are created

    private async Task<Core.DTOs.Settings.BusinessSettingsDto?> GetBusinessSettings(Guid organizationId)
    {
        // TODO: Implement when OrganizationBusinessSettingsController logic is available
        return new Core.DTOs.Settings.BusinessSettingsDto();
    }

    private async Task<Core.DTOs.Settings.NotificationSettingsDto?> GetNotificationSettings(Guid organizationId)
    {
        // TODO: Implement when OrganizationNotificationSettingsController logic is available
        return new Core.DTOs.Settings.NotificationSettingsDto();
    }

    private async Task<Core.DTOs.Settings.OrganizationPayoutSettingsDto?> GetPaymentSettings(Guid organizationId)
    {
        // TODO: Implement when OrganizationPayoutSettingsController logic is available
        return new Core.DTOs.Settings.OrganizationPayoutSettingsDto();
    }

    private async Task<Core.DTOs.Settings.OrganizationProfileSettingsDto?> GetProfileSettings(Guid organizationId)
    {
        // TODO: Implement when OrganizationProfileSettingsController logic is available
        return new Core.DTOs.Settings.OrganizationProfileSettingsDto();
    }

    private async Task<Core.DTOs.Settings.SalesSettingsDto?> GetSalesSettings(Guid organizationId)
    {
        // TODO: Implement when OrganizationSalesSettingsController logic is available
        return new Core.DTOs.Settings.SalesSettingsDto();
    }

    private async Task<Core.DTOs.Settings.OrganizationBookkeepingSettingsDto?> GetBookkeepingSettings(Guid organizationId)
    {
        // TODO: Implement when OrganizationBookkeepingSettingsController logic is available
        return new Core.DTOs.Settings.OrganizationBookkeepingSettingsDto();
    }

    private async Task<Core.DTOs.Settings.OrganizationAccountSettingsDto?> GetAccountSettings(Guid organizationId)
    {
        // TODO: Implement when OrganizationAccountSettingsController logic is available
        return new Core.DTOs.Settings.OrganizationAccountSettingsDto();
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        return orgIdClaim != null ? Guid.Parse(orgIdClaim) : Guid.Empty;
    }

    #endregion
}