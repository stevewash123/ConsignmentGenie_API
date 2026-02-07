namespace ConsignmentGenie.Core.DTOs.Items;

public class ReorderImagesRequest
{
    public List<ImageOrderDto> Images { get; set; } = new();
}

public class ImageOrderDto
{
    public Guid ItemImageId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}