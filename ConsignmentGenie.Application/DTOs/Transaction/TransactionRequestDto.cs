using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Transaction;

public class CreateTransactionRequest
{
    [Required]
    public Guid ItemId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Sale price must be greater than 0")]
    public decimal SalePrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Sales tax cannot be negative")]
    public decimal? SalesTaxAmount { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty; // "Cash", "Card", "Online"

    // Source removed for MVP - will default to "Manual" in Phase 2+ will support Square, Shopify, etc.

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime? SaleDate { get; set; } // Optional, defaults to now
}

public class UpdateTransactionRequest
{
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}