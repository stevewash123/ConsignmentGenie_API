using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Transaction : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid ItemId { get; set; }

    public Guid ProviderId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SalePrice { get; set; }

    public DateTime SaleDate { get; set; }

    [MaxLength(50)]
    public string Source { get; set; } = "InStore";  // InStore, Square, Shopify, SquareOnline

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }  // Cash, Card, Online

    // Split calculation (calculated, not user-entered)
    [Column(TypeName = "decimal(5,2)")]
    public decimal ProviderSplitPercentage { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ProviderAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ShopAmount { get; set; }

    // Tax (optional, imported from POS)
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalesTaxAmount { get; set; }

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    // QuickBooks sync (Phase 3 - include fields now)
    public bool SyncedToQuickBooks { get; set; }

    public DateTime? SyncedAt { get; set; }

    [MaxLength(100)]
    public string? QuickBooksSalesReceiptId { get; set; }

    public bool QuickBooksSyncFailed { get; set; }

    public string? QuickBooksSyncError { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
}