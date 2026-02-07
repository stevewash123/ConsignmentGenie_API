namespace ConsignmentGenie.Core.DTOs.Notifications;

public class NotificationDto
{
    public Guid NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public bool IsImportant { get; set; }
    public DateTime? MarkedImportantAt { get; set; }
    public Guid? MarkedImportantByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty; // "2 hours ago", "Yesterday"

    // FROM/TO pattern for modern notification routing
    public Guid? FromUserId { get; set; }
    public string? FromType { get; set; } // 'owner', 'consignor', 'admin', 'customer', 'system'
    public Guid ToUserId { get; set; }
    public string ToType { get; set; } = string.Empty; // 'owner', 'consignor', 'admin', 'customer'

    // Action status for actionable notifications
    public string? ActionStatus { get; set; } // 'pending', 'completed', 'failed', 'cancelled'
    public DateTime? ActionCompletedAt { get; set; }
    public Guid? ActionCompletedByUserId { get; set; }

    // Receipt acknowledgment tracking
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }

    // For navigation - using modern property names
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ActionUrl { get; set; } // "/provider/items/xxx" or "/provider/payouts/xxx"

    // Legacy properties for backward compatibility (deprecated)
    [Obsolete("Use ReferenceType instead")]
    public string? RelatedEntityType { get; set; }

    [Obsolete("Use ReferenceId instead")]
    public Guid? RelatedEntityId { get; set; }

    // Type-specific data
    public NotificationMetadata? Metadata { get; set; }
}