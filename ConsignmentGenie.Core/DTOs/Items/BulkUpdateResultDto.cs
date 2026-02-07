namespace ConsignmentGenie.Core.DTOs.Items;

public class BulkUpdateResultDto
{
    public int SuccessfulUpdates { get; set; }
    public int FailedUpdates { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<Guid> UpdatedItemIds { get; set; } = new();
    public List<Guid> FailedItemIds { get; set; } = new();
}