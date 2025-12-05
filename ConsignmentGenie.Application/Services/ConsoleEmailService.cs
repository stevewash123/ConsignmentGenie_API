using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
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

    public async Task<bool> SendWelcomeEmailAsync(string email, string organizationName, string ownerFirstName, string storeCode)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Enhanced Welcome Email\n" +
            "  To: {Email}\n" +
            "  Subject: Welcome to ConsignmentGenie!\n" +
            "  Organization: {OrganizationName}\n" +
            "  Owner: {OwnerFirstName}\n" +
            "  Store Code: {StoreCode}",
            email, organizationName, ownerFirstName, storeCode
        );

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

    public async Task<bool> SendSuggestionNotificationAsync(Suggestion suggestion)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Suggestion Notification\n" +
            "  From: {UserName} ({UserEmail})\n" +
            "  Type: {SuggestionType}\n" +
            "  Message: {Message}",
            suggestion.UserName, suggestion.UserEmail, suggestion.Type, suggestion.Message
        );

        await Task.Delay(100);
        return true;
    }

    public async Task<bool> SendSimpleEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Simple Email\n" +
            "  To: {Email}\n" +
            "  Subject: {Subject}\n" +
            "  Body: {Body}\n" +
            "  HTML: {IsHtml}",
            toEmail, subject, body, isHtml
        );

        await Task.Delay(100);
        return true;
    }

    public async Task<bool> SendConsignorInvitationAsync(string email, string consignorName, string shopName, string inviteLink, string expirationDate)
    {
        _logger.LogInformation(
            "[CONSOLE EMAIL] Consignor Invitation Email\n" +
            "  To: {Email}\n" +
            "  Subject: Join {ShopName} as a Consignor - Invitation to ConsignmentGenie\n" +
            "  Consignor: {ConsignorName}\n" +
            "  Shop: {ShopName}\n" +
            "  Invite Link: {InviteLink}\n" +
            "  Expires: {ExpirationDate}",
            email, shopName, consignorName, shopName, inviteLink, expirationDate
        );

        await Task.Delay(100);
        return true;
    }
}