using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Item : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid ProviderId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CostBasis { get; set; }  // Minimum provider wants

    public string? Photos { get; set; }  // JSON array of URLs (Phase 2: blob storage)

    [MaxLength(50)]
    public string? Category { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Available;

    // Product extensibility (future)
    [MaxLength(50)]
    public string ProductType { get; set; } = "General";  // General, Clothing, Art, Furniture

    public string? ExtendedProperties { get; set; }  // JSON for type-specific fields

    // Split override (optional)
    [Column(TypeName = "decimal(5,2)")]
    public decimal? OverrideSplitPercentage { get; set; }  // Override provider's default

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public Transaction? Transaction { get; set; }  // One-to-one when sold
}