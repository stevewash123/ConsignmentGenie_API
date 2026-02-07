using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

/// <summary>
/// Cached payout summary data, computed nightly by Hangfire job.
/// One record per consignor per organization.
/// </summary>
public class PayoutSummary
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;

    [Required]
    public Guid ConsignorId { get; set; }

    [ForeignKey(nameof(ConsignorId))]
    public Consignor Consignor { get; set; } = null!;

    // === Computed Amounts ===
    public decimal ClearedAmount { get; set; }
    public decimal UnclearedAmount { get; set; }
    public decimal TotalPendingAmount { get; set; }

    // === Transaction Counts ===
    public int ClearedTransactionCount { get; set; }
    public int UnclearedTransactionCount { get; set; }
    public int TotalTransactionCount { get; set; }

    // === Date Info ===
    public DateTime? EarliestSaleDate { get; set; }
    public DateTime? LatestSaleDate { get; set; }
    public DateTime? NextClearDate { get; set; }

    // === Threshold Status ===
    public bool MeetsMinimumThreshold { get; set; }
    public decimal LowestMinimumThreshold { get; set; }

    // === Notification Tracking ===
    public bool NotificationSent { get; set; }
    public DateTime? NotificationSentAt { get; set; }

    // === Audit ===
    public DateTime LastComputedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}