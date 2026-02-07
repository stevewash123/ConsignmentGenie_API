using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Entities;

public class UserNotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }

    [Required]
    public NotificationType NotificationType { get; set; }

    // Channel preferences
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool SlackEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = true; // For future mobile app

    // Additional contact info (will also be on User entity eventually)
    public string? PhoneNumber { get; set; }
    public string? SlackUserId { get; set; }

    // Timing preferences (for future use)
    public bool InstantDelivery { get; set; } = true;
    public TimeSpan? QuietHoursStart { get; set; } // e.g., 22:00
    public TimeSpan? QuietHoursEnd { get; set; }   // e.g., 08:00

    // Navigation properties
    public User User { get; set; } = null!;
}