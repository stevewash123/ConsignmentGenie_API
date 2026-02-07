namespace ConsignmentGenie.Core.DTOs.Square;

public class ItemSyncResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public SyncedItemDto? Item { get; set; }
    public Guid? CgItemId { get; set; }
    public DateTime SyncedAt { get; set; }
}