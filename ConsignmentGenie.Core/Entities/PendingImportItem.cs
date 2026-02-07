using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class PendingImportItem : BaseEntity
{
    // Core identification
    public Guid OrganizationId { get; set; }

    // Item data
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumPrice { get; set; }

    [MaxLength(100)]
    public string? Sku { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Condition { get; set; }

    // Source tracking
    [Required]
    public ImportSource Source { get; set; }

    [MaxLength(200)]
    public string? SourceReference { get; set; } // ManifestId, filename, etc.

    // Image data from manifest
    [MaxLength(300)]
    public string? ImagePublicId { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    // Assignment state
    public Guid? ConsignorId { get; set; }

    // UI state
    public bool IsVerified { get; set; } = false;
    public bool IsSelectedForAssignment { get; set; } = false;

    // Processing state
    [Required]
    public ImportStatus Status { get; set; } = ImportStatus.Pending;

    public DateTime? ImportedAt { get; set; }

    public Guid? ImportedItemId { get; set; } // Links to final Item when imported

    // Metadata - CreatedAt inherited from BaseEntity

    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;

    public Consignor? Consignor { get; set; }

    public Item? ImportedItem { get; set; }
}

public enum ImportSource
{
    Manifest = 1,
    CSV = 2,
    Square = 3
}

public enum ImportStatus
{
    Pending = 0,
    Assigned = 1,
    Verified = 2,
    Imported = 3,
    Rejected = 4,
    Deleted = 5
}