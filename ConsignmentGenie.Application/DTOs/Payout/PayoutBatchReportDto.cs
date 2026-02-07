namespace ConsignmentGenie.Application.DTOs.Payout;

public record PayoutBatchReportDto
{
    public string BatchId { get; init; } = "";
    public int TotalPayouts { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<PayoutBatchItemDto> Payouts { get; init; } = new();
}

public record PayoutBatchItemDto
{
    public string PayoutNumber { get; init; } = "";
    public string ConsignorName { get; init; } = "";
    public int ItemCount { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = "";
}