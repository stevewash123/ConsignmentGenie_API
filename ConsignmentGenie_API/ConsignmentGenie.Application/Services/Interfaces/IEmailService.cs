namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendWelcomeEmailAsync(string email, string organizationName);
    Task<bool> SendTrialExpiringEmailAsync(string email, int daysRemaining);
    Task<bool> SendPaymentFailedEmailAsync(string email, decimal amount, DateTime retryDate);
    Task<bool> SendPaymentReceiptAsync(string email, decimal amount, string invoiceUrl);
    Task<bool> SendSyncErrorEmailAsync(string email, string integration, string errorMessage);
}