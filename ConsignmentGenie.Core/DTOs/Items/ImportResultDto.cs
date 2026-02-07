namespace ConsignmentGenie.Core.DTOs.Items;

public class ImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportedItemDto> ImportedItems { get; set; } = new();
    public List<ImportErrorDto> Errors { get; set; } = new();
}