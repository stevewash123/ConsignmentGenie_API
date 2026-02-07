using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

public class NotificationSettingsDto
{
    [Required]
    [EmailAddress]
    public string PrimaryEmail { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public Dictionary<string, bool> EmailPreferences { get; set; } = new();
    public Dictionary<string, bool> SmsPreferences { get; set; } = new();

    public NotificationThresholdsDto Thresholds { get; set; } = new();
}

public class NotificationThresholdsDto
{
    [Range(0, double.MaxValue)]
    public decimal HighValueSale { get; set; } = 500m;

    [Range(0, int.MaxValue)]
    public int LowInventory { get; set; } = 10;
}

/// <summary>
/// Request DTO for updating notification settings (partial updates with flat key-value structure)
/// </summary>
public class UpdateNotificationSettingsRequest
{
    [EmailAddress]
    public string? PrimaryEmail { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    // Email preference toggles (e.g., "email_item_sold": true)
    public bool? Email_ItemSold { get; set; }
    public bool? Email_DailySalesSummary { get; set; }
    public bool? Email_WeeklyReport { get; set; }
    public bool? Email_MonthlyStatement { get; set; }
    public bool? Email_ConsignorSignup { get; set; }
    public bool? Email_ConsignorItemAdded { get; set; }
    public bool? Email_PendingApproval { get; set; }
    public bool? Email_ConsignorPayoutReady { get; set; }
    public bool? Email_HighValueSale { get; set; }
    public bool? Email_LowInventory { get; set; }
    public bool? Email_PricingSuggestions { get; set; }
    public bool? Email_SystemMaintenance { get; set; }
    public bool? Email_SecurityAlerts { get; set; }
    public bool? Email_AccountChanges { get; set; }
    public bool? Email_BackupStatus { get; set; }

    // SMS preference toggles (e.g., "sms_item_sold": true)
    public bool? Sms_ItemSold { get; set; }
    public bool? Sms_DailySalesSummary { get; set; }
    public bool? Sms_WeeklyReport { get; set; }
    public bool? Sms_MonthlyStatement { get; set; }
    public bool? Sms_ConsignorSignup { get; set; }
    public bool? Sms_ConsignorItemAdded { get; set; }
    public bool? Sms_PendingApproval { get; set; }
    public bool? Sms_ConsignorPayoutReady { get; set; }
    public bool? Sms_HighValueSale { get; set; }
    public bool? Sms_LowInventory { get; set; }
    public bool? Sms_PricingSuggestions { get; set; }
    public bool? Sms_SystemMaintenance { get; set; }
    public bool? Sms_SecurityAlerts { get; set; }
    public bool? Sms_AccountChanges { get; set; }
    public bool? Sms_BackupStatus { get; set; }

    // Threshold settings
    [Range(0, double.MaxValue)]
    public decimal? HighValueSaleThreshold { get; set; }

    [Range(0, int.MaxValue)]
    public int? LowInventoryThreshold { get; set; }
}