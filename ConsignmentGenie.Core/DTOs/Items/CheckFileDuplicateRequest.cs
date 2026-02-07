namespace ConsignmentGenie.Core.DTOs.Items;

public class CheckFileDuplicateRequest
{
    public string? FileName { get; set; }
    public string FirstDataRow { get; set; } = string.Empty;
    public int RowCount { get; set; }
}