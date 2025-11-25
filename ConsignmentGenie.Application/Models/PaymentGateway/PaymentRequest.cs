namespace ConsignmentGenie.Application.Models.PaymentGateway;

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? PaymentMethodId { get; set; }
    public string? CustomerId { get; set; }
    public string? Description { get; set; }
    public bool CaptureImmediately { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();

    // For idempotency
    public string? IdempotencyKey { get; set; }

    // For recurring payments
    public bool SavePaymentMethod { get; set; } = false;

    // Application context
    public Guid OrganizationId { get; set; }
    public string? InternalOrderId { get; set; }
}