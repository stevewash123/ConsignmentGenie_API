using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Entities;

public class Notification : BaseEntity
{
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public Guid? ConsignorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // item_sold, payout_processed, etc.

    // Related Entities (for linking/navigation)
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; } // Item, Transaction, Payout, Statement
    public Guid? RelatedEntityId { get; set; }

    // Status tracking
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Email Tracking
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }
    [MaxLength(255)]
    public string? EmailFailedReason { get; set; }

    // Metadata (JSON for type-specific data)
    public string? Metadata { get; set; } // Store as JSON string

    // Expiration
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
    public Consignor? Consignor { get; set; }
}