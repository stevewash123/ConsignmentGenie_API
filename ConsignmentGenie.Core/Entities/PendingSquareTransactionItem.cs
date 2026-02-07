using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

/// <summary>
/// Represents an item within a pending Square transaction.
/// Links to either a PendingSquareImport (if unassigned) or an Item (if assigned).
/// </summary>
public class PendingSquareTransactionItem : BaseEntity
{
    [Required]
    public Guid PendingTransactionId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    // Links to either pending import OR assigned item
    public Guid? PendingSquareImportId { get; set; }  // If item is still pending assignment
    public Guid? ItemId { get; set; }                 // If item has been assigned to consignor

    // Square identifiers for linking
    [MaxLength(100)]
    [Required]
    public string SquareCatalogId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SquareVariationId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal => UnitPrice * Quantity;

    // Item details captured at transaction time (in case item data changes)
    [MaxLength(200)]
    [Required]
    public string ItemName { get; set; } = string.Empty;

    public string? ItemDescription { get; set; }

    [MaxLength(100)]
    public string? ItemSku { get; set; }

    // Navigation properties
    public PendingSquareTransaction PendingTransaction { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public PendingSquareImport? PendingSquareImport { get; set; }
    public Item? Item { get; set; }

    // Helper properties
    public bool IsItemAssigned => ItemId.HasValue;

    // Will be populated when item is assigned and we can calculate splits
    [Column(TypeName = "decimal(5,4)")]
    public decimal? ConsignorSplitPercentage { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ConsignorAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? StoreAmount { get; set; }

    public Guid? ConsignorId { get; set; } // Populated when item is assigned
}