namespace ConsignmentGenie.Application.DTOs.Shopper;

/// <summary>
/// Shopper-facing item image DTO
/// </summary>
public class ShopperItemImageDto
{
    public Guid ImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}