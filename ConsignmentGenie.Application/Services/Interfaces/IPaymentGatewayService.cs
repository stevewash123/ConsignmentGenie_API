using ConsignmentGenie.Application.Models.PaymentGateway;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IPaymentGatewayService
{
    string ConsignorName { get; }

    // Payment processing
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResult> CapturePaymentAsync(string paymentIntentId, decimal? amount = null);
    Task<PaymentResult> RefundPaymentAsync(RefundRequest request);

    // Payment methods
    Task<string> CreatePaymentMethodAsync(string customerId, Dictionary<string, object> paymentMethodData);
    Task<List<PaymentMethodInfo>> GetPaymentMethodsAsync(string customerId);
    Task<bool> DeletePaymentMethodAsync(string paymentMethodId);

    // Customer management
    Task<string> CreateCustomerAsync(string email, string? name = null, Dictionary<string, object>? metadata = null);
    Task<bool> UpdateCustomerAsync(string customerId, Dictionary<string, object> updates);
    Task<bool> DeleteCustomerAsync(string customerId);

    // Webhooks and validation
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret);
    Task HandleWebhookAsync(string payload, Dictionary<string, object> headers);

    // Gateway-specific features
    Task<Dictionary<string, object>> GetGatewaySpecificDataAsync(string transactionId);
    Task<bool> SupportsFunctionality(string functionality); // "recurring", "marketplace", "tokenization", etc.
}