using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class NotificationPreferences : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    // Email Preferences (per notification type)
    public bool EmailItemSold { get; set; } = true;
    public bool EmailPayoutProcessed { get; set; } = true;
    public bool EmailPayoutPending { get; set; } = false;
    public bool EmailItemExpired { get; set; } = false;
    public bool EmailStatementReady { get; set; } = true;
    public bool EmailAccountUpdate { get; set; } = true;

    // Global Settings
    public bool EmailEnabled { get; set; } = true; // Master switch

    [MaxLength(20)]
    public string DigestMode { get; set; } = "instant"; // instant, daily, weekly

    public TimeSpan DigestTime { get; set; } = TimeSpan.FromHours(9); // For daily/weekly digest (09:00:00)

    public int DigestDay { get; set; } = 1; // For weekly (1=Monday)

    // Thresholds
    public decimal PayoutPendingThreshold { get; set; } = 50.00m; // Notify when balance exceeds

    // Navigation properties
    public User User { get; set; } = null!;
}