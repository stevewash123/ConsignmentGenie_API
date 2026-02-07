namespace ConsignmentGenie.Core.DTOs.Items;

public class ImportErrorDto
{
    public int RowNumber { get; set; }
    public string? Name { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public Dictionary<string, string> OriginalData { get; set; } = new();
}