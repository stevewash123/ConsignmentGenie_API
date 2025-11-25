using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class Item : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ProviderId { get; set; }

    // Identification
    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    // Details
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [Required]
    public ItemCondition Condition { get; set; } = ItemCondition.Good;

    [MaxLength(255)]
    public string? Materials { get; set; }

    public string? Measurements { get; set; }

    // Pricing
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? OriginalPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumPrice { get; set; }

    // Status
    [Required]
    public ItemStatus Status { get; set; } = ItemStatus.Available;

    public DateTime? StatusChangedAt { get; set; }

    [MaxLength(255)]
    public string? StatusChangedReason { get; set; }

    // Dates
    [Required]
    public DateOnly ReceivedDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly? ListedDate { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public DateOnly? SoldDate { get; set; }

    // Photos
    [MaxLength(500)]
    public string? PrimaryImageUrl { get; set; }

    // Location
    [MaxLength(100)]
    public string? Location { get; set; }

    // Notes
    public string? Notes { get; set; }

    public string? InternalNotes { get; set; }

    // Legacy Photos JSON (for compatibility with existing controllers)
    public string? Photos { get; set; }

    // Audit - using inherited CreatedAt, UpdatedAt from BaseEntity
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public Transaction? Transaction { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
}