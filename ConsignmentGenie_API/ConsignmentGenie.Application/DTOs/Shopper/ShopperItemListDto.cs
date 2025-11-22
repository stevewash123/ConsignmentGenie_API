using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs.Shopper;

/// <summary>
/// Shopper-facing item list DTO - hides internal business data
/// </summary>
public class ShopperItemListDto
{
    public Guid ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public ItemCondition Condition { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public DateTime? ListedDate { get; set; }
    public List<ShopperItemImageDto> Images { get; set; } = new();
}