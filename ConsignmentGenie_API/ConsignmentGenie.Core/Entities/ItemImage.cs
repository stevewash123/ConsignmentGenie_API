using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ItemImage : BaseEntity
{
    [Required]
    public Guid ItemId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsPrimary { get; set; } = false;

    public Guid? CreatedBy { get; set; }

    // Navigation properties
    public Item Item { get; set; } = null!;
    public User? CreatedByUser { get; set; }
}