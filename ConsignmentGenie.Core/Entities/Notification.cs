using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Entities;

public class Notification : BaseEntity
{
    /// <summary>
    /// Organization ID - nullable for admin notifications
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// FROM/TO pattern for clear notification routing
    /// </summary>
    public Guid? FromUserId { get; set; }

    [MaxLength(20)]
    public string? FromType { get; set; } // 'owner', 'consignor', 'admin', 'customer', 'system'

    [Required]
    public Guid ToUserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string ToType { get; set; } = string.Empty; // 'owner', 'consignor', 'admin', 'customer'

    /// <summary>
    /// Action status for actionable notifications (dropoff manifests, approvals, etc.)
    /// </summary>
    [MaxLength(20)]
    public string? ActionStatus { get; set; } // 'pending', 'completed', 'failed', 'cancelled'

    public DateTime? ActionCompletedAt { get; set; }
    public Guid? ActionCompletedByUserId { get; set; }


    /// <summary>
    /// Notification categorization and content
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // item_sold, payout_processed, etc.

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// JSONB payload for flexible notification data
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Optional attachment support
    /// </summary>
    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }

    [MaxLength(200)]
    public string? AttachmentName { get; set; }

    [MaxLength(50)]
    public string? AttachmentType { get; set; }

    /// <summary>
    /// Deep link for "View in app" action
    /// </summary>
    [MaxLength(500)]
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
    [MaxLength(50)]
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// JSONB metadata for email templating and structured notification data
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Read status tracking
    /// </summary>
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Importance flag for priority notifications
    /// </summary>
    public bool IsImportant { get; set; } = false;
    public DateTime? MarkedImportantAt { get; set; }
    public Guid? MarkedImportantByUserId { get; set; }

    /// <summary>
    /// Receipt acknowledgment tracking - for notifications requiring explicit user acknowledgment
    /// </summary>
    public bool IsAcknowledged { get; set; } = false;
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }

    /// <summary>
    /// Email delivery tracking
    /// </summary>
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }

    /// <summary>
    /// SMS delivery tracking
    /// </summary>
    public bool SmsSent { get; set; } = false;
    public DateTime? SmsSentAt { get; set; }

    /// <summary>
    /// Soft delete for dismissed notifications
    /// </summary>
    public DateTime? DeletedAt { get; set; }


    // Navigation properties
    public Organization? Organization { get; set; }
    public User? FromUser { get; set; }  // Who sent/triggered this notification
    public User ToUser { get; set; } = null!;  // Who receives this notification
    public User? ActionCompletedByUser { get; set; }  // Who completed the action
    public User? AcknowledgedByUser { get; set; }  // Who acknowledged receipt
    public User? MarkedImportantByUser { get; set; }  // Who marked as important


    // Related entity navigation properties
    public Item? Item { get; set; }
    public Transaction? Transaction { get; set; }
    public Payout? Payout { get; set; }
    public Statement? Statement { get; set; }
    public ItemRequest? ItemRequest { get; set; }
}