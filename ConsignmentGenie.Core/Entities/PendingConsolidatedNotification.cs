using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

/// <summary>
/// Tracks pending consolidated notifications to prevent spam
/// Uses Hangfire for delayed execution with debounce logic
/// </summary>
[Table("PendingConsolidatedNotifications")]
public class PendingConsolidatedNotification : BaseEntity
{
    [Required]
    public Guid ConsignorId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of item IDs to include in notification
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string ItemIds { get; set; } = "[]";

    /// <summary>
    /// Hangfire job ID for cancellation when rescheduling
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string HangfireJobId { get; set; } = string.Empty;

    /// <summary>
    /// When the notification is scheduled to be sent
    /// </summary>
    [Required]
    public DateTime ScheduledFor { get; set; }

    /// <summary>
    /// When Hangfire job started processing (for race condition protection)
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }

    /// <summary>
    /// When notification was successfully sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When notification processing failed
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Reason for failure
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ConsignorId))]
    public virtual Consignor Consignor { get; set; } = null!;

    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;
}