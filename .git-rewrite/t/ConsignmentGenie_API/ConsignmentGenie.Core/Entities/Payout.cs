using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Payout : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid ProviderId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    public DateTime? PaidAt { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public string? ItemsIncluded { get; set; }  // JSON array of transaction IDs

    // QuickBooks sync (Phase 3 - include fields now)
    public bool SyncedToQuickBooks { get; set; }

    public DateTime? SyncedAt { get; set; }

    [MaxLength(100)]
    public string? QuickBooksBillId { get; set; }

    public bool QuickBooksSyncFailed { get; set; }

    public string? QuickBooksSyncError { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
}