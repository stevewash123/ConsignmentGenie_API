using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Category : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Audit fields
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
}