using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.DTOs.Items;

public class CreateItemRequest
{
    [Required]
    public Guid ProviderId { get; set; }

    public string? Sku { get; set; }              // Optional - auto-generate if empty
    public string? Barcode { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;

    public string? Brand { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }

    [Required]
    public ItemCondition Condition { get; set; }

    public string? Materials { get; set; }
    public string? Measurements { get; set; }

    [Required, Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }
    public decimal? MinimumPrice { get; set; }

    public DateTime? ReceivedDate { get; set; }  // Defaults to today
    public DateTime? ExpirationDate { get; set; }

    public string? Location { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
}