using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

/// <summary>
/// Represents a Square transaction that references items still in the pending assignment queue.
/// When all referenced items are assigned to consignors, this transaction is promoted to the main Transaction table.
/// </summary>
public class PendingSquareTransaction : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    [MaxLength(100)]
    [Required]
    public string SquarePaymentId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SquareLocationId { get; set; }

    [Required]
    public DateTime SquareCreatedAt { get; set; }

    [MaxLength(50)]
    public string Source { get; set; } = "Square";

    [Required]
    [MaxLength(50)]
    public string PaymentType { get; set; } = string.Empty;  // Cash, Card, Check, Other

    /// <summary>
    /// Payment method from Square (CARD, CASH, SQUARE_GIFT_CARD, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? SquarePaymentMethod { get; set; }

    /// <summary>
    /// Card brand if card payment (VISA, MASTERCARD, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? SquareCardBrand { get; set; }

    // Transaction totals
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

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    public string? Notes { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // "Pending", "Ready" (all items assigned)

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<PendingSquareTransactionItem> Items { get; set; } = new List<PendingSquareTransactionItem>();

    // Helper property to check if all items are assigned
    public bool AllItemsAssigned => Items.All(i => i.IsItemAssigned);
}