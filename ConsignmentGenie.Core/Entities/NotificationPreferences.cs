using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class NotificationPreferences : BaseEntity
{
    /// <summary>
    /// User these preferences belong to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Role for these preferences ('consignor', 'owner', 'admin')
    /// Allows different notification settings per role
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// SMS settings
    /// </summary>
    public bool SmsOptedIn { get; set; } = false;

    [MaxLength(20)]
    public string? SmsPhoneNumber { get; set; }

    public bool SmsPhoneVerified { get; set; } = false;

    public DateTime? SmsPhoneVerifiedAt { get; set; }

    /// <summary>
    /// Per-type notification preferences stored as JSONB
    /// Structure: { "item_sold": { "email": true, "sms": false }, "payout_processed": { "email": true, "sms": true } }
    /// </summary>
    [Required]
    public string TypePreferences { get; set; } = "{}";


    /// <summary>
    /// Legacy email preference properties for backward compatibility during migration
    /// TODO: Remove after updating ConsignorNotificationService to use TypePreferences JSONB
    /// </summary>
    [Obsolete("Use TypePreferences JSONB instead")]
    public bool EmailItemSold { get; set; } = true;

    [Obsolete("Use TypePreferences JSONB instead")]
    public bool EmailPayoutProcessed { get; set; } = true;

    [Obsolete("Use TypePreferences JSONB instead")]
    public bool EmailPayoutPending { get; set; } = true;

    [Obsolete("Use TypePreferences JSONB instead")]
    public bool EmailItemExpired { get; set; } = true;

    [Obsolete("Use TypePreferences JSONB instead")]
    public bool EmailStatementReady { get; set; } = true;

    [Obsolete("Use TypePreferences JSONB instead")]
    public bool EmailAccountUpdate { get; set; } = true;

    [Obsolete("Use TypePreferences JSONB instead")]
    public string DigestMode { get; set; } = "off";

    [Obsolete("Use TypePreferences JSONB instead")]
    public string DigestTime { get; set; } = "09:00";

    [Obsolete("Use TypePreferences JSONB instead")]
    public int DigestDay { get; set; } = 1;

    [Obsolete("Use TypePreferences JSONB instead")]
    public decimal PayoutPendingThreshold { get; set; } = 50.00m;

    [Obsolete("Use TypePreferences JSONB instead")]
    public bool EmailEnabled { get; set; } = true;

    // Navigation properties
    public User User { get; set; } = null!;
}