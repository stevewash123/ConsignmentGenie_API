namespace ConsignmentGenie.Core.DTOs.Square;

public class FullSyncResult
{
    public bool Success { get; set; }
    public int CategoriesSynced { get; set; }
    public int ItemsSynced { get; set; }
    public int ItemsCreated { get; set; }
    public int ItemsUpdated { get; set; }
    public int TransactionsSynced { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public DateTime SyncedAt { get; set; }
}