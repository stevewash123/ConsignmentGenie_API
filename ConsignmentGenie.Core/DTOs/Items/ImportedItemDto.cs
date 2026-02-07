namespace ConsignmentGenie.Core.DTOs.Items;

public class ImportedItemDto
{
    public int RowNumber { get; set; }
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
}