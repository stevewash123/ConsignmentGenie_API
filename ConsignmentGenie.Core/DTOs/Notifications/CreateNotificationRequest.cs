using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Notifications;

public class CreateNotificationRequest
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ConsignorId { get; set; }

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Related entity (flexible approach)
    public string? RelatedEntityType { get; set; } // "Item", "Transaction", "Payout", "Statement"
    public Guid? RelatedEntityId { get; set; }

    // Type-specific metadata
    public NotificationMetadata? Metadata { get; set; }

    // Optional expiration
    public DateTime? ExpiresAt { get; set; }
}