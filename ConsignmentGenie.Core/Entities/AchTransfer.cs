using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

/// <summary>
/// Record of an ACH transfer (payout) via Dwolla
/// </summary>
public class AchTransfer
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    public Guid? PayoutId { get; set; }

    [Required]
    public Guid ConsignorId { get; set; }

    [Required]
    public Guid SourceFundingSourceId { get; set; }

    [Required]
    public Guid DestinationFundingSourceId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DwollaTransferId { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string DwollaTransferUrl { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Internal status: 'pending', 'processed', 'failed', 'cancelled'
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Dwolla's actual status string
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DwollaStatus { get; set; } = null!;

    public string? FailureReason { get; set; }

    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(PayoutId))]
    public virtual Payout? Payout { get; set; }

    [ForeignKey(nameof(ConsignorId))]
    public virtual Consignor Consignor { get; set; } = null!;

    [ForeignKey(nameof(SourceFundingSourceId))]
    public virtual FundingSource SourceFundingSource { get; set; } = null!;

    [ForeignKey(nameof(DestinationFundingSourceId))]
    public virtual FundingSource DestinationFundingSource { get; set; } = null!;
}