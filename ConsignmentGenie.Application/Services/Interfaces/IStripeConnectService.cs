namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IStripeConnectService
{
    /// <summary>
    /// Generate a Stripe Connect OAuth link for an organization to connect their account
    /// </summary>
    Task<string> GenerateConnectLinkAsync(Guid organizationId);

    /// <summary>
    /// Handle the OAuth callback from Stripe Connect
    /// </summary>
    Task<bool> HandleConnectCallbackAsync(string authorizationCode, string state);

    /// <summary>
    /// Create a payment intent for a customer purchase
    /// </summary>
    Task<string> CreatePaymentIntentAsync(Guid organizationId, decimal amount, string currency = "usd", Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Retrieve payment intent status
    /// </summary>
    Task<string> GetPaymentIntentStatusAsync(string paymentIntentId, Guid organizationId);

    /// <summary>
    /// Confirm a payment intent
    /// </summary>
    Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId, Guid organizationId);

    /// <summary>
    /// Get the organization's Stripe Connect account status
    /// </summary>
    Task<StripeConnectAccountStatus> GetAccountStatusAsync(Guid organizationId);

    /// <summary>
    /// Disconnect Stripe Connect account
    /// </summary>
    Task<bool> DisconnectAccountAsync(Guid organizationId);
}

public class StripeConnectAccountStatus
{
    public bool IsConnected { get; set; }
    public bool PayoutsEnabled { get; set; }
    public bool ChargesEnabled { get; set; }
    public string? AccountId { get; set; }
    public string? PublishableKey { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public List<string> RequirementsPending { get; set; } = new();
}