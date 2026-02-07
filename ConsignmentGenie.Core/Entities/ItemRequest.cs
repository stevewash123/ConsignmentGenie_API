using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public class ItemRequest : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid ConsignorId { get; set; }

    // Item Details
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Condition { get; set; }

    public string? Measurements { get; set; }

    // Pricing
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SuggestedPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinAcceptablePrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? OriginalPurchasePrice { get; set; }

    // Notes
    public string? ConsignorNotes { get; set; }

    // Status
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    // Review
    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    public string? OwnerNotes { get; set; }

    // Approval
    public Guid? ApprovedItemId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ApprovedPrice { get; set; }

    [MaxLength(100)]
    public string? ApprovedCategory { get; set; }

    // Resubmission
    public Guid? OriginalRequestId { get; set; }

    public int ResubmissionCount { get; set; } = 0;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Consignor Consignor { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public Item? ApprovedItem { get; set; }
    public ItemRequest? OriginalRequest { get; set; }
    public ICollection<ItemRequestImage> Images { get; set; } = new List<ItemRequestImage>();
}