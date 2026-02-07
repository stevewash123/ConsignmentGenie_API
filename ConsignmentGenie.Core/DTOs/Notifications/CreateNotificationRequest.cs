using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Notifications;

public class CreateNotificationRequest
{
    /// <summary>
    /// Organization ID - nullable for admin notifications
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// FROM/TO pattern for clear notification routing
    /// </summary>
    public Guid? FromUserId { get; set; }
    public string? FromType { get; set; } // 'owner', 'consignor', 'admin', 'customer', 'system'

    public Guid ToUserId { get; set; }
    public string ToType { get; set; } = string.Empty; // 'owner', 'consignor', 'admin', 'customer'

    /// <summary>
    /// Action status for actionable notifications (dropoff manifests, approvals, etc.)
    /// </summary>
    public string? ActionStatus { get; set; } // 'pending', 'completed', 'failed', 'cancelled'

    /// <summary>
    /// Notification type - use constants from NotificationTypes
    /// </summary>
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JSONB payload for flexible notification data
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Optional attachment support
    /// </summary>
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentType { get; set; }

    /// <summary>
    /// Deep link for "View in app" action
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Related entity references for specific notifications
    /// </summary>
    public Guid? ItemId { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid? PayoutId { get; set; }
    public Guid? StatementId { get; set; }
    public Guid? ItemRequestId { get; set; }

    /// <summary>
    /// Generic reference fields for flexibility
    /// </summary>
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// Metadata for email templating and notification enrichment
    /// </summary>
    public NotificationMetadata? Metadata { get; set; }

}