using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// MVP Email Service - logs emails to console instead of actually sending them
/// Use this for development and MVP phase before implementing SendGrid in Phase 2+
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string organizationName)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Welcome Email\n" +
            "  To: {Email}\n" +
            "  Subject: Welcome to ConsignmentGenie!\n" +
            "  Organization: {OrganizationName}",
            email, organizationName
        );

        // Simulate async operation
        await Task.Delay(100);
        return true;
    }

    public async Task<bool> SendTrialExpiringEmailAsync(string email, int daysRemaining)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Trial Expiring Email\n" +
            "  To: {Email}\n" +
            "  Subject: Your ConsignmentGenie trial expires in {DaysRemaining} days\n" +
            "  Days Remaining: {DaysRemaining}",
            email, daysRemaining, daysRemaining
        );

        await Task.Delay(100);
        return true;
    }

    public async Task<bool> SendPaymentFailedEmailAsync(string email, decimal amount, DateTime retryDate)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Payment Failed Email\n" +
            "  To: {Email}\n" +
            "  Subject: Payment failed - Please update your payment method\n" +
            "  Amount: {Amount:C}\n" +
            "  Retry Date: {RetryDate:yyyy-MM-dd}",
            email, amount, retryDate
        );

        await Task.Delay(100);
        return true;
    }

    public async Task<bool> SendPaymentReceiptAsync(string email, decimal amount, string invoiceUrl)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Payment Receipt Email\n" +
            "  To: {Email}\n" +
            "  Subject: Payment received - Thank you!\n" +
            "  Amount: {Amount:C}\n" +
            "  Invoice URL: {InvoiceUrl}",
            email, amount, invoiceUrl
        );

        await Task.Delay(100);
        return true;
    }

    public async Task<bool> SendSyncErrorEmailAsync(string email, string integration, string errorMessage)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Sync Error Email\n" +
            "  To: {Email}\n" +
            "  Subject: {Integration} sync failed\n" +
            "  Integration: {Integration}\n" +
            "  Error: {ErrorMessage}",
            email, integration, integration, errorMessage
        );

        await Task.Delay(100);
        return true;
    }
}