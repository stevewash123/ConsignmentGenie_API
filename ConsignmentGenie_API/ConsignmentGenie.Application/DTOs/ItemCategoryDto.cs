using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs;

public class ItemCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public decimal? DefaultCommissionRate { get; set; }
    public int SubCategoryCount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateItemCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(10)]
    public string? Color { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public int SortOrder { get; set; } = 0;

    [Range(0, 100)]
    public decimal? DefaultCommissionRate { get; set; }
}

public class UpdateItemCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(10)]
    public string? Color { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public int SortOrder { get; set; } = 0;

    [Range(0, 100)]
    public decimal? DefaultCommissionRate { get; set; }

    public bool IsActive { get; set; }
}