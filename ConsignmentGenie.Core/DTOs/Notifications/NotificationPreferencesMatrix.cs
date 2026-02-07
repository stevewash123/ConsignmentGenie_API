using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Notifications;

/// <summary>
/// Represents a user's notification preferences in an optimized matrix format for fast lookups
/// </summary>
public class NotificationPreferencesMatrix
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? PrimaryEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public bool SmsVerified { get; set; }

    /// <summary>
    /// Fast lookup dictionary: NotificationType → Channel → Enabled
    /// Example: { "item_sold": { "email": true, "sms": false, "system": true }, "payout_ready": { "email": true, "sms": true, "system": true } }
    /// </summary>
    public Dictionary<string, Dictionary<NotificationChannel, bool>> Preferences { get; set; } = new();

    /// <summary>
    /// Batch delivery preferences for consolidated reports
    /// </summary>
    public Dictionary<string, string> BatchPreferences { get; set; } = new();

    /// <summary>
    /// Threshold settings for conditional notifications
    /// </summary>
    public Dictionary<string, decimal> Thresholds { get; set; } = new();

    /// <summary>
    /// When preferences were last updated (for cache invalidation)
    /// </summary>
    public DateTime LastUpdated { get; set; }
}