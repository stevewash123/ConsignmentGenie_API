namespace ConsignmentGenie.Core.DTOs.Items;

public class ItemImageDto
{
    public Guid ItemImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}