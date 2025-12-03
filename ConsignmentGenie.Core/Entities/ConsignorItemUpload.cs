using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ProviderItemUpload : BaseEntity
{
    [Required]
    public Guid ConsignorId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    public ItemUploadStatus Status { get; set; } = ItemUploadStatus.Pending;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    [MaxLength(1000)]
    public string? ReviewNotes { get; set; }

    [MaxLength(500)]
    public string? ProviderNotes { get; set; }

    // Navigation properties
    public Consignor Consignor { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}