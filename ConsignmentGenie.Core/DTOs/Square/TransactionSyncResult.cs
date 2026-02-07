namespace ConsignmentGenie.Core.DTOs.Square;

public class TransactionSyncResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int TransactionsSynced { get; set; }
    public int PendingTransactionsCreated { get; set; }
    public int MainTransactionsCreated { get; set; }
    public DateTime SyncedAt { get; set; }
    public List<string> Errors { get; set; } = new();
}