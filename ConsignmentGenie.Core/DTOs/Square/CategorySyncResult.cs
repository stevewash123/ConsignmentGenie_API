namespace ConsignmentGenie.Core.DTOs.Square;

public class CategorySyncResult
{
    public bool Success { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Total { get; set; }
}