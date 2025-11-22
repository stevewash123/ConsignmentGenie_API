using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ConsignmentGenie.Application.Services;

public class EmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(ISendGridClient sendGridClient, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string organizationName)
    {
        try
        {
            var templateId = _configuration["SendGrid:Templates:Welcome"];
            var dynamicTemplateData = new
            {
                organization_name = organizationName,
                subject = "Welcome to ConsignmentGenie!"
            };

            return await SendTemplateEmailAsync(email, templateId, dynamicTemplateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendTrialExpiringEmailAsync(string email, int daysRemaining)
    {
        try
        {
            var templateId = _configuration["SendGrid:Templates:TrialExpiring"];
            var dynamicTemplateData = new
            {
                days_remaining = daysRemaining,
                subject = $"Your ConsignmentGenie trial expires in {daysRemaining} days"
            };

            return await SendTemplateEmailAsync(email, templateId, dynamicTemplateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send trial expiring email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPaymentFailedEmailAsync(string email, decimal amount, DateTime retryDate)
    {
        try
        {
            var templateId = _configuration["SendGrid:Templates:PaymentFailed"];
            var dynamicTemplateData = new
            {
                amount = amount.ToString("C"),
                retry_date = retryDate.ToString("MMMM dd, yyyy"),
                subject = "Payment failed - Please update your payment method"
            };

            return await SendTemplateEmailAsync(email, templateId, dynamicTemplateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment failed email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPaymentReceiptAsync(string email, decimal amount, string invoiceUrl)
    {
        try
        {
            var templateId = _configuration["SendGrid:Templates:PaymentReceipt"];
            var dynamicTemplateData = new
            {
                amount = amount.ToString("C"),
                invoice_url = invoiceUrl,
                subject = "Payment received - Thank you!"
            };

            return await SendTemplateEmailAsync(email, templateId, dynamicTemplateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment receipt email to {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendSyncErrorEmailAsync(string email, string integration, string errorMessage)
    {
        try
        {
            var templateId = _configuration["SendGrid:Templates:SyncError"];
            var dynamicTemplateData = new
            {
                integration = integration,
                error_message = errorMessage,
                subject = $"{integration} sync failed"
            };

            return await SendTemplateEmailAsync(email, templateId, dynamicTemplateData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send sync error email to {Email}", email);
            return false;
        }
    }

    private async Task<bool> SendTemplateEmailAsync(string toEmail, string? templateId, object dynamicTemplateData)
    {
        if (string.IsNullOrEmpty(templateId))
        {
            _logger.LogWarning("Template ID not configured for email to {Email}", toEmail);
            return false;
        }

        var from = new EmailAddress(
            _configuration["SendGrid:FromEmail"] ?? "noreply@consignmentgenie.com",
            _configuration["SendGrid:FromName"] ?? "ConsignmentGenie"
        );
        var to = new EmailAddress(toEmail);

        var msg = MailHelper.CreateSingleTemplateEmail(from, to, templateId, dynamicTemplateData);

        var response = await _sendGridClient.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email sent successfully to {Email} using template {TemplateId}", toEmail, templateId);
            return true;
        }
        else
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("Failed to send email to {Email}. Status: {Status}, Body: {Body}", toEmail, response.StatusCode, body);
            return false;
        }
    }
}