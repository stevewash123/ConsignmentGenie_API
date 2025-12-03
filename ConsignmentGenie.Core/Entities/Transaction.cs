using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Transaction : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid ItemId { get; set; }

    public Guid ConsignorId { get; set; }

    // Link to order (for storefront transactions)
    public Guid? OrderId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SalePrice { get; set; }

    public DateTime SaleDate { get; set; }

    // Alias for spec compatibility
    public DateTime TransactionDate
    {
        get => SaleDate;
        set => SaleDate = value;
    }

    [MaxLength(50)]
    public string Source { get; set; } = "Manual";  // Manual for MVP, Phase 2+ will add: Square, Shopify, SquareOnline

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }  // Cash, Card, Online

    // Split calculation (calculated, not user-entered)
    [Column(TypeName = "decimal(5,2)")]
    public decimal ConsignorSplitPercentage { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ConsignorAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ShopAmount { get; set; }

    // Tax (optional, imported from POS)
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalesTaxAmount { get; set; }

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

    public bool QuickBooksSyncFailed { get; set; }

    public string? QuickBooksSyncError { get; set; }

    public string? Notes { get; set; }

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
    public Item Item { get; set; } = null!;
    public Consignor Consignor { get; set; } = null!;
    public Order? Order { get; set; }
    public Payout? Payout { get; set; }
}