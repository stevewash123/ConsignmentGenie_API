using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IEmailService
{
    // Legacy template-based methods (for backwards compatibility)
    Task<bool> SendWelcomeEmailAsync(string email, string organizationName);
    Task<bool> SendWelcomeEmailAsync(string email, string organizationName, string ownerFirstName, string storeCode);
    Task<bool> SendTrialExpiringEmailAsync(string email, int daysRemaining);
    Task<bool> SendPaymentFailedEmailAsync(string email, decimal amount, DateTime retryDate);
    Task<bool> SendPaymentReceiptAsync(string email, decimal amount, string invoiceUrl);
    Task<bool> SendSyncErrorEmailAsync(string email, string integration, string errorMessage);
    Task<bool> SendSuggestionNotificationAsync(Suggestion suggestion);

    // New simplified method for notification service
    Task<bool> SendSimpleEmailAsync(string toEmail, string subject, string body, bool isHtml = true);

    // Provider invitation method
    Task<bool> SendProviderInvitationAsync(string email, string providerName, string shopName, string inviteLink, string expirationDate);
}