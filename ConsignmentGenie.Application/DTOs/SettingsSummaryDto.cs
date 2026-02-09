using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Core.DTOs.Settings;

namespace ConsignmentGenie.Application.DTOs;

/// <summary>
/// Unified settings summary that aggregates all organization settings
/// for efficient frontend caching and single-point access
/// </summary>
public class SettingsSummaryDto
{
    /// <summary>
    /// Consignor-related settings (onboarding, defaults, etc.)
    /// </summary>
    public ConsignorSettingsDto? Consignor { get; set; }

    /// <summary>
    /// Business settings (hours, policies, etc.)
    /// </summary>
    public Core.DTOs.Settings.BusinessSettingsDto? Business { get; set; }

    /// <summary>
    /// Notification preferences
    /// </summary>
    public Core.DTOs.Settings.NotificationSettingsDto? Notifications { get; set; }

    /// <summary>
    /// Payment and payout settings
    /// </summary>
    public Core.DTOs.Settings.OrganizationPayoutSettingsDto? Payment { get; set; }

    /// <summary>
    /// Organization profile settings
    /// </summary>
    public Core.DTOs.Settings.OrganizationProfileSettingsDto? Profile { get; set; }

    /// <summary>
    /// Sales and transaction settings
    /// </summary>
    public Core.DTOs.Settings.SalesSettingsDto? Sales { get; set; }

    /// <summary>
    /// Bookkeeping and accounting settings
    /// </summary>
    public Core.DTOs.Settings.OrganizationBookkeepingSettingsDto? Bookkeeping { get; set; }

    /// <summary>
    /// Account-level settings
    /// </summary>
    public Core.DTOs.Settings.OrganizationAccountSettingsDto? Account { get; set; }
}

/// <summary>
/// Composite DTO for consignor-related settings
/// </summary>
public class ConsignorSettingsDto
{
    public ConsignorOnboardingDto? Onboarding { get; set; }
    public ConsignorDefaultsDto? Defaults { get; set; }
}