namespace ConsignmentGenie.Application.Models.PaymentGateway;

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public decimal? AmountProcessed { get; set; }
    public decimal? Fee { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Dictionary<string, object> GatewayResponse { get; set; } = new();
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Cancelled,
    RequiresAction,
    RequiresPaymentMethod,
    Refunded,
    PartiallyRefunded
}