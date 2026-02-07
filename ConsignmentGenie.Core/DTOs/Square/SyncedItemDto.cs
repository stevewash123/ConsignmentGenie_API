namespace ConsignmentGenie.Core.DTOs.Square;

public class SyncedItemDto
{
    public string SquareCatalogId { get; set; } = string.Empty;
    public string? SquareVariationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public bool Available { get; set; }
}