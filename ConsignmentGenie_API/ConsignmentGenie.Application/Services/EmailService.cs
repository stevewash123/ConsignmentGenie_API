using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
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

    public async Task<bool> SendSuggestionNotificationAsync(Suggestion suggestion)
    {
        try
        {
            // Send to yourself (developer email)
            var developerEmail = _configuration["DeveloperEmail"] ?? "swashcode@outlook.com";

            var subject = $"New Suggestion: {suggestion.Type} from {suggestion.UserName}";

            var from = new EmailAddress(
                _configuration["SendGrid:FromEmail"] ?? "noreply@consignmentgenie.com",
                "ConsignmentGenie Suggestion Box"
            );

            var to = new EmailAddress(developerEmail);

            var plainTextContent = $@"
New Suggestion Received

From: {suggestion.UserName} ({suggestion.UserEmail})
Type: {suggestion.Type}
Submitted: {suggestion.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC

Message:
{suggestion.Message}

---
ConsignmentGenie Suggestion System
";

            var htmlContent = $@"
<html>
<body>
<h2>New Suggestion Received</h2>
<p><strong>From:</strong> {suggestion.UserName} ({suggestion.UserEmail})</p>
<p><strong>Type:</strong> {suggestion.Type}</p>
<p><strong>Submitted:</strong> {suggestion.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
<br/>
<h3>Message:</h3>
<div style=""background-color: #f5f5f5; padding: 15px; border-left: 4px solid #047857; margin: 10px 0;"">{suggestion.Message.Replace("\n", "<br/>")}</div>
<br/>
<hr/>
<p><em>ConsignmentGenie Suggestion System</em></p>
</body>
</html>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Suggestion notification sent successfully for suggestion {SuggestionId}", suggestion.Id);
                return true;
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send suggestion notification. Status: {Status}, Body: {Body}", response.StatusCode, body);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send suggestion notification for suggestion {SuggestionId}", suggestion.Id);
            return false;
        }
    }

    public async Task<bool> SendSimpleEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            var from = new EmailAddress(
                _configuration["SendGrid:FromEmail"] ?? "noreply@consignmentgenie.com",
                _configuration["SendGrid:FromName"] ?? "ConsignmentGenie"
            );

            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                isHtml ? null : body,  // Plain text content
                isHtml ? body : null   // HTML content
            );

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Simple email sent successfully to {Email}", toEmail);
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("Failed to send simple email to {Email}. Status: {Status}, Body: {Body}", toEmail, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending simple email to {Email}", toEmail);
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