namespace ConsignmentGenie.Core.DTOs.Items;

public class BulkImportResultDto
{
    public int TotalItems { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<string> Errors { get; set; } = new();
}