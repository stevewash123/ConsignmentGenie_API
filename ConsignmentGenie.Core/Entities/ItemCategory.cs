using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsignmentGenie.Core.Entities;

public enum CategorySource
{
    CG,      // Created by ConsignmentGenie owner or consignor
    Square   // Imported from Square
}

public enum CategoryStatus
{
    Active,  // Available for use in dropdowns and assignments
    Pending  // Awaiting owner approval (consignor-originated)
}

public class ItemCategory : BaseEntity
{
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(10)]
    public string? Color { get; set; } // Hex color code for UI

    // Amendment 1: Hybrid model fields
    [Required]
    public CategorySource Source { get; set; } = CategorySource.CG;

    [Required]
    public CategoryStatus Status { get; set; } = CategoryStatus.Active;

    // Non-null while Status = Pending; cleared on promotion to Active
    public Guid? ConsignorId { get; set; }

    // Square's catalog object ID. Populated for Source = Square.
    [MaxLength(100)]
    public string? SquareCategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    // Hierarchy support
    public Guid? ParentCategoryId { get; set; }
    public int SortOrder { get; set; } = 0;

    // Commission override
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DefaultCommissionRate { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ItemCategory? ParentCategory { get; set; }
    public ICollection<ItemCategory> SubCategories { get; set; } = new List<ItemCategory>();
    public ICollection<Item> Items { get; set; } = new List<Item>();

    // Consignor who suggested this category (when Status = Pending)
    public Consignor? Consignor { get; set; }
}