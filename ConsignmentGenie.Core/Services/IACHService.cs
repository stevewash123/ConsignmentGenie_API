namespace ConsignmentGenie.Core.Services;

public interface IACHService
{
    Task<ACHTransferResult> InitiateTransfer(ACHTransferRequest request);
    Task<ACHTransferStatus> GetTransferStatus(string transferId);
}

public record ACHTransferRequest
{
    public decimal Amount { get; init; }
    public string FromAccountToken { get; init; } = "";
    public string ToAccountId { get; init; } = "";
    public string Description { get; init; } = "";
    public string Reference { get; init; } = "";
    public Guid PayoutId { get; init; }
}

public record ACHTransferResult
{
    public bool Success { get; init; }
    public string? TransferId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime InitiatedAt { get; init; } = DateTime.UtcNow;
}

public record ACHTransferStatus
{
    public string TransferId { get; init; } = "";
    public string Status { get; init; } = ""; // pending, processing, completed, failed, cancelled
    public string? ErrorMessage { get; init; }
    public DateTime StatusUpdatedAt { get; init; }
}