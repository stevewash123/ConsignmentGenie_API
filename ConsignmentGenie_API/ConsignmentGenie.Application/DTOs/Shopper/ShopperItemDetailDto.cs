using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs.Shopper;

/// <summary>
/// Shopper-facing item detail DTO - hides internal business data
/// </summary>
public class ShopperItemDetailDto
{
    public Guid ItemId { get; set; }

    // Basic info
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public ItemCondition Condition { get; set; }
    public string? Materials { get; set; }
    public string? Measurements { get; set; }

    // Images
    public List<ShopperItemImageDto> Images { get; set; } = new();

    // Availability
    public bool IsAvailable { get; set; }
    public DateTime? ListedDate { get; set; }
}