using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ItemTag : BaseEntity
{
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? Color { get; set; } // Hex color code for UI

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<ItemTagAssignment> ItemTagAssignments { get; set; } = new List<ItemTagAssignment>();
}

// Junction table for many-to-many relationship
public class ItemTagAssignment
{
    public Guid ItemId { get; set; }
    public Guid ItemTagId { get; set; }

    public Item Item { get; set; } = null!;
    public ItemTag ItemTag { get; set; } = null!;
}