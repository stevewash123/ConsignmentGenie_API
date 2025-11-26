namespace ConsignmentGenie.Core.DTOs.Notifications;

public class NotificationPreferencesDto
{
    // Email Preferences
    public bool EmailEnabled { get; set; }
    public bool EmailItemSold { get; set; }
    public bool EmailPayoutProcessed { get; set; }
    public bool EmailPayoutPending { get; set; }
    public bool EmailItemExpired { get; set; }
    public bool EmailStatementReady { get; set; }
    public bool EmailAccountUpdate { get; set; }

    // Digest Settings
    public string DigestMode { get; set; } = "instant"; // instant, daily, weekly
    public string DigestTime { get; set; } = "09:00";   // "09:00"
    public int DigestDay { get; set; } = 1;            // 1-7 for weekly

    // Thresholds
    public decimal PayoutPendingThreshold { get; set; }
}