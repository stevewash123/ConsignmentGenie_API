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

    // Phase 4: Advanced inventory features
    public Guid? CategoryId { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Condition { get; set; } // New, Like New, Good, Fair, Poor

    // Inventory tracking
    public int Quantity { get; set; } = 1;
    public int? MinimumStock { get; set; }
    public int? MaximumStock { get; set; }

    // Search optimization
    public string? SearchKeywords { get; set; } // Space-separated keywords for search

    // Weight/Dimensions for shipping
    [Column(TypeName = "decimal(8,2)")]
    public decimal? Weight { get; set; } // in pounds

    [Column(TypeName = "decimal(8,2)")]
    public decimal? Length { get; set; } // in inches

    [Column(TypeName = "decimal(8,2)")]
    public decimal? Width { get; set; } // in inches

    [Column(TypeName = "decimal(8,2)")]
    public decimal? Height { get; set; } // in inches

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public Transaction? Transaction { get; set; }  // One-to-one when sold
    public ItemCategory? ItemCategory { get; set; }
    public ICollection<ItemTagAssignment> ItemTagAssignments { get; set; } = new List<ItemTagAssignment>();
}