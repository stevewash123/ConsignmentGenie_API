using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IStripeService
{
    Task<string> CreateCustomerAsync(Guid organizationId, string email, string organizationName);
    Task<SubscriptionResult> CreateSubscriptionAsync(Guid organizationId, SubscriptionTier tier, bool isFounder, int? founderTier);
    Task<SubscriptionResult> UpdateSubscriptionAsync(Guid organizationId, SubscriptionTier newTier);
    Task<bool> CancelSubscriptionAsync(Guid organizationId, bool immediately = false);
    Task<string> CreateBillingPortalSessionAsync(Guid organizationId, string returnUrl);
    Task ProcessWebhookAsync(string json, string stripeSignature);
    Task<FounderEligibilityResult> ValidateFounderEligibilityAsync();
}

public class SubscriptionResult
{
    public bool Success { get; set; }
    public string? ClientSecret { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SubscriptionId { get; set; }
    public string? CustomerId { get; set; }
}

public class FounderEligibilityResult
{
    public bool IsEligible { get; set; }
    public int? FounderTier { get; set; }  // 1, 2, or 3
    public decimal? FounderPrice { get; set; }
    public string Message { get; set; } = string.Empty;
}