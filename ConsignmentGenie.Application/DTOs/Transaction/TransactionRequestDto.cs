using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Transaction;

public class TransactionItemRequest
{
    [Required]
    public Guid ItemId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }
}

public class CreateTransactionRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<TransactionItemRequest> Items { get; set; } = new();

    [Required]
    [StringLength(50)]
    public string PaymentType { get; set; } = string.Empty; // "Cash", "Card", "Check", "Other"

    [Range(0, 1, ErrorMessage = "Tax rate must be between 0 and 1")]
    public decimal TaxRate { get; set; } = 0;

    [MaxLength(255)]
    [EmailAddress]
    public string? CustomerEmail { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime? TransactionDate { get; set; } // Optional, defaults to now

    // User audit fields
    public Guid? ProcessedByUserId { get; set; }

    [StringLength(100)]
    public string? ProcessedByName { get; set; }
}

public class TransactionResponse
{
    public Guid TransactionId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateTransactionRequest
{
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}