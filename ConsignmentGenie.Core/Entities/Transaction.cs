using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Transaction : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    // Link to order (for storefront transactions)
    public Guid? OrderId { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    // Alias for legacy compatibility
    public DateTime SaleDate
    {
        get => TransactionDate;
        set => TransactionDate = value;
    }

    [MaxLength(50)]
    public string Source { get; set; } = "Manual";  // Manual for MVP, Phase 2+ will add: Square, Shopify, SquareOnline

    [Required]
    [MaxLength(50)]
    public string PaymentType { get; set; } = string.Empty;  // Cash, Card, Check, Other

    // Transaction totals (calculated from items)
    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    // Customer information
    [MaxLength(255)]
    [EmailAddress]
    public string? CustomerEmail { get; set; }

    // Legacy tax field for compatibility
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalesTaxAmount
    {
        get => TaxAmount;
        set => TaxAmount = value ?? 0;
    }

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    // Square sync (Phase 2)
    [MaxLength(100)]
    public string? SquarePaymentId { get; set; }

    [MaxLength(100)]
    public string? SquareLocationId { get; set; }

    public bool ImportedFromSquare { get; set; }

    public DateTime? SquareCreatedAt { get; set; }

    // QuickBooks sync (Phase 3 - include fields now)
    public bool SyncedToQuickBooks { get; set; }

    public DateTime? SyncedAt { get; set; }

    [MaxLength(100)]
    public string? QuickBooksSalesReceiptId { get; set; }

    [MaxLength(100)]
    public string? QBSalesReceiptId { get; set; }

    public bool QuickBooksSyncFailed { get; set; }

    public string? QuickBooksSyncError { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Calculated date when funds from this transaction are available for payout.
    /// Based on sale date + clearance days for the payment method.
    /// </summary>
    public DateTime? ClearDate { get; set; }

    // Payout tracking (Enhanced for Phase 1)
    public bool ConsignorPaidOut { get; set; } = false;
    public DateTime? ConsignorPaidOutDate { get; set; }
    public string? PayoutMethod { get; set; } // "Check", "Cash", "PayPal", "Bank Transfer"
    public string? PayoutNotes { get; set; }

    // New Payout System Fields
    public Guid? PayoutId { get; set; }
    [MaxLength(20)]
    public string PayoutStatus { get; set; } = "Pending"; // "Pending", "Paid"

    [MaxLength(20)]
    public string Status { get; set; } = "Completed"; // "Completed", "Pending", "Cancelled"

    // Clerk audit trail
    public Guid? ProcessedByUserId { get; set; }  // Who processed this transaction

    [MaxLength(100)]
    public string? ProcessedByName { get; set; }  // Denormalized for reports

    // Navigation properties
    public User? ProcessedByUser { get; set; }  // Link to user who processed transaction
    public Organization Organization { get; set; } = null!;
    public Order? Order { get; set; }
    public Payout? Payout { get; set; }

    // New multi-item relationship
    public ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();

    // Legacy navigation properties removed - now using Items collection
}