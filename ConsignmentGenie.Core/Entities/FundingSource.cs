using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

/// <summary>
/// Bank account linked via Plaid/Dwolla
/// </summary>
public class FundingSource
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// If NULL, this is the organization's funding source.
    /// If set, this is a consignor's funding source.
    /// </summary>
    public Guid? ConsignorId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DwollaFundingSourceId { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string DwollaFundingSourceUrl { get; set; } = null!;

    /// <summary>
    /// Encrypted Plaid access token for future operations
    /// </summary>
    [MaxLength(255)]
    public string? PlaidAccessToken { get; set; }

    [MaxLength(255)]
    public string? PlaidAccountId { get; set; }

    [Required]
    [MaxLength(255)]
    public string BankName { get; set; } = null!;

    /// <summary>
    /// 'checking' or 'savings'
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; } = null!;

    /// <summary>
    /// Masked account number, e.g., "●●●●1234"
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string AccountNumberMask { get; set; } = null!;

    /// <summary>
    /// 'active', 'removed', 'verification_pending'
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "active";

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RemovedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(ConsignorId))]
    public virtual Consignor? Consignor { get; set; }
}