using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Entities;

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

    // Amendment 1: Hybrid model fields
    public CategorySource Source { get; set; }
    public CategoryStatus Status { get; set; }
    public Guid? ConsignorId { get; set; }
    public string? ConsignorName { get; set; } // For display in pending sections
    public string? SquareCategoryId { get; set; }
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

    /// <summary>
    /// Required flag when creating a new category that doesn't match existing ones.
    /// Prevents accidental category creation without user confirmation.
    /// </summary>
    public bool ConfirmedNew { get; set; } = false;

    /// <summary>
    /// Amendment 1: When true, creates a pending category for owner approval instead of active category.
    /// Only valid when called by consignors and shop allows category suggestions.
    /// </summary>
    public bool RequestPending { get; set; } = false;
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

public class ValidateCategoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class ValidateCategoriesBulkRequest
{
    [Required]
    public List<string> Names { get; set; } = new();
}

public class CategoryValidationResponse
{
    public CategoryMatchDto? ExactMatch { get; set; }
    public List<CategorySuggestionDto> SuggestedMatches { get; set; } = new();
    public bool IsExactMatch { get; set; }
}

public class CategoryMatchDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CategorySuggestionDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Similarity { get; set; }
}