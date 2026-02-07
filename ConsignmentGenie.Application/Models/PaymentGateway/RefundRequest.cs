namespace ConsignmentGenie.Application.Models.PaymentGateway;

public class RefundRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal? Amount { get; set; } // null = full refund
    public string? Reason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? IdempotencyKey { get; set; }
}