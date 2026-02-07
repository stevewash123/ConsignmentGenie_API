using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ItemRequestImage : BaseEntity
{
    [Required]
    public Guid ItemRequestId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsPrimary { get; set; } = false;

    // Navigation properties
    public ItemRequest ItemRequest { get; set; } = null!;
}