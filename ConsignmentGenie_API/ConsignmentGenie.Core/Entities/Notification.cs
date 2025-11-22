using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Entities;

public class Notification : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid? UserId { get; set; } // If null, it's an organization-wide notification

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Info;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    // Status tracking
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public bool IsDismissed { get; set; } = false;
    public DateTime? DismissedAt { get; set; }

    // Action data
    public string? ActionUrl { get; set; } // URL to navigate to when clicked
    public string? ActionData { get; set; } // JSON data for the action

    // Delivery tracking
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }
    public bool SmsSent { get; set; } = false;
    public DateTime? SmsSentAt { get; set; }

    // Expiration
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? User { get; set; }
}