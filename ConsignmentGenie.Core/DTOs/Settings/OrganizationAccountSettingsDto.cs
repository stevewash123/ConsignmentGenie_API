using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Organization account settings DTO
/// </summary>
public class OrganizationAccountSettingsDto
{
    // Billing & Subscription
    public string? SubscriptionPlan { get; set; }
    public DateTime? SubscriptionExpiryDate { get; set; }
    public string? BillingEmail { get; set; }
    public bool AutoRenewSubscription { get; set; } = true;

    // Payment Method
    public string? PaymentMethodId { get; set; }
    public string? PaymentMethodType { get; set; }
    public string? CardLastFourDigits { get; set; }
    public DateTime? CardExpiryDate { get; set; }

    // Account Security
    public bool TwoFactorEnabled { get; set; } = false;
    public DateTime? LastPasswordChange { get; set; }
    public bool RequirePasswordChange { get; set; } = false;
    public int LoginSessionTimeoutMinutes { get; set; } = 480; // 8 hours

    // Owner Contact (Account Level)
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public string? SecondaryContactEmail { get; set; }

    // Account Preferences
    public string? PreferredLanguage { get; set; } = "en";
    public string? PreferredTimezone { get; set; }
    public string? PreferredDateFormat { get; set; }
    public string? PreferredCurrency { get; set; } = "USD";

    // Data & Privacy
    public bool DataRetentionEnabled { get; set; } = true;
    public int DataRetentionYears { get; set; } = 7;
    public bool AllowDataExport { get; set; } = true;
    public bool AllowAccountDeletion { get; set; } = false;
}

/// <summary>
/// Request DTO for updating organization account settings
/// </summary>
public class UpdateOrganizationAccountSettingsRequest
{
    public string? SubscriptionPlan { get; set; }
    public string? BillingEmail { get; set; }
    public bool? AutoRenewSubscription { get; set; }
    public string? PaymentMethodId { get; set; }
    public bool? TwoFactorEnabled { get; set; }
    public bool? RequirePasswordChange { get; set; }
    public int? LoginSessionTimeoutMinutes { get; set; }
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public string? SecondaryContactEmail { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredTimezone { get; set; }
    public string? PreferredDateFormat { get; set; }
    public string? PreferredCurrency { get; set; }
    public bool? DataRetentionEnabled { get; set; }
    public int? DataRetentionYears { get; set; }
    public bool? AllowDataExport { get; set; }
    public bool? AllowAccountDeletion { get; set; }
}