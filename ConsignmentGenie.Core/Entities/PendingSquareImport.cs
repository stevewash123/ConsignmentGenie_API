using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class PendingSquareImport : BaseEntity
{
    public Guid OrganizationId { get; set; }

    // Square identifiers
    [Required]
    [MaxLength(100)]
    public string SquareCatalogId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SquareVariationId { get; set; }

    // Item data from Square
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [MaxLength(100)]
    public string? Sku { get; set; }

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    // Square metadata
    public DateTime? SquareUpdatedAt { get; set; }

    // Import tracking
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}