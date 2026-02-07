namespace ConsignmentGenie.Core.DTOs.Notifications;

public class NotificationPreferencesDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;

    // Channel Preferences
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }

    // Type-specific Email Preferences
    public bool EmailItemSold { get; set; }
    public bool EmailPayoutProcessed { get; set; }
    public bool EmailPayoutPending { get; set; }
    public bool EmailItemExpired { get; set; }
    public bool EmailStatementReady { get; set; }
    public bool EmailAccountUpdate { get; set; }

    // Digest Settings
    public bool DigestEnabled { get; set; }
    public string DigestFrequency { get; set; } = "daily"; // instant, daily, weekly
    public string DigestTime { get; set; } = "09:00";      // "09:00"
    public int DigestDay { get; set; } = 1;               // 1-7 for weekly

    // Thresholds
    public decimal PayoutPendingThreshold { get; set; }
}