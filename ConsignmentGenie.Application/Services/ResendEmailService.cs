using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _apiKey;

    public ResendEmailService(IConfiguration configuration, ILogger<ResendEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["Resend:ApiKey"] ?? "";
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ConsignmentGenie/1.0");
    }

    public async Task<bool> SendWelcomeEmailAsync(string email, string organizationName)
    {
        _logger.LogInformation("[EMAIL] Starting welcome email send to {Email} for organization {OrganizationName}", email, organizationName);

        try
        {
            var subject = "Welcome to Consignment Genie!";
            _logger.LogDebug("[EMAIL] Welcome email subject: {Subject}", subject);

            // Load HTML template
            var htmlTemplatePath = "/mnt/c/Projects/ConsignmentGenie/Documents/welcome-email.html";
            _logger.LogDebug("[EMAIL] Loading HTML template from {HtmlTemplatePath}", htmlTemplatePath);

            if (!File.Exists(htmlTemplatePath))
            {
                _logger.LogError("[EMAIL] HTML template file not found at {HtmlTemplatePath}", htmlTemplatePath);
                return false;
            }

            var htmlTemplate = await File.ReadAllTextAsync(htmlTemplatePath);
            _logger.LogDebug("[EMAIL] HTML template loaded, length: {TemplateLength} characters", htmlTemplate.Length);

            // Load plain text template
            var textTemplatePath = "/mnt/c/Projects/ConsignmentGenie/Documents/welcome-email.txt";
            _logger.LogDebug("[EMAIL] Loading text template from {TextTemplatePath}", textTemplatePath);

            if (!File.Exists(textTemplatePath))
            {
                _logger.LogError("[EMAIL] Text template file not found at {TextTemplatePath}", textTemplatePath);
                return false;
            }

            var textTemplate = await File.ReadAllTextAsync(textTemplatePath);
            _logger.LogDebug("[EMAIL] Text template loaded, length: {TemplateLength} characters", textTemplate.Length);

            // For now, extract first name from email (will be improved when we pass more data)
            var ownerFirstName = email.Split('@')[0];
            var loginUrl = "http://localhost:4200/owner/dashboard";
            var storeCode = "PENDING"; // Default when not yet generated
            var unsubscribeUrl = "#"; // Placeholder for now

            _logger.LogDebug("[EMAIL] Template variables: OwnerFirstName={OwnerFirstName}, ShopName={ShopName}, LoginUrl={LoginUrl}, StoreCode={StoreCode}, UnsubscribeUrl={UnsubscribeUrl}",
                ownerFirstName, organizationName, loginUrl, storeCode, unsubscribeUrl);

            // Replace template variables in HTML
            var htmlContent = htmlTemplate
                .Replace("{{OwnerFirstName}}", ownerFirstName)
                .Replace("{{ShopName}}", organizationName)
                .Replace("{{LoginUrl}}", loginUrl)
                .Replace("{{StoreCode}}", storeCode)
                .Replace("{{UnsubscribeUrl}}", unsubscribeUrl);

            // Replace template variables in text
            var textContent = textTemplate
                .Replace("{{OwnerFirstName}}", ownerFirstName)
                .Replace("{{ShopName}}", organizationName)
                .Replace("{{LoginUrl}}", loginUrl)
                .Replace("{{StoreCode}}", storeCode)
                .Replace("{{UnsubscribeUrl}}", unsubscribeUrl);

            _logger.LogInformation("[EMAIL] Templates processed successfully for welcome email to {Email}, calling SendSimpleEmailAsync", email);

            var result = await SendSimpleEmailAsync(email, subject, htmlContent, textContent);
            _logger.LogInformation("[EMAIL] Welcome email send result for {Email}: {Result}", email, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Failed to send welcome email to {Email} for organization {OrganizationName} - Template loading failed or variable substitution failed", email, organizationName);
            return false;
        }
    }

    public async Task<bool> SendTrialExpiringEmailAsync(string email, int daysRemaining)
    {
        try
        {
            var subject = $"Your ConsignmentGenie trial expires in {daysRemaining} days";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Trial Expiring Soon</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: #f39c12; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">Trial Expiring Soon</h1>
    </div>

    <div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;"">
        <p>Your ConsignmentGenie trial expires in <strong>{daysRemaining} days</strong>.</p>

        <p>Don't lose access to your consignment management tools! Upgrade now to continue using:</p>

        <ul style=""padding-left: 20px;"">
            <li>Provider management</li>
            <li>Inventory tracking</li>
            <li>Transaction recording</li>
            <li>Automated payout calculations</li>
            <li>Financial reporting</li>
        </ul>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""http://localhost:4200/billing"" style=""display: inline-block; background: #f39c12; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Upgrade Now</a>
        </div>

        <p style=""color: #666; font-size: 14px; text-align: center;"">Questions? Contact us at support@microsaasbuilders.com</p>
    </div>
</body>
</html>";

            var textContent = $@"
Your ConsignmentGenie trial expires in {daysRemaining} days.

Don't lose access to your consignment management tools! Upgrade now to continue using:
- Provider management
- Inventory tracking
- Transaction recording
- Automated payout calculations
- Financial reporting

Upgrade now: http://localhost:4200/billing

Questions? Contact us at support@microsaasbuilders.com
";

            return await SendSimpleEmailAsync(email, subject, htmlContent, textContent);
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
            var subject = "Payment failed - Please update your payment method";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Payment Failed</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: #e74c3c; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">Payment Failed</h1>
    </div>

    <div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;"">
        <p>We were unable to process your payment of <strong>{amount:C}</strong> for your ConsignmentGenie subscription.</p>

        <p>We'll automatically retry the payment on <strong>{retryDate:MMMM dd, yyyy}</strong>. To avoid any service interruption, please update your payment method before then.</p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""http://localhost:4200/billing"" style=""display: inline-block; background: #e74c3c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Update Payment Method</a>
        </div>

        <p style=""color: #666; font-size: 14px; text-align: center;"">Questions? Contact us at support@microsaasbuilders.com</p>
    </div>
</body>
</html>";

            var textContent = $@"
Payment Failed

We were unable to process your payment of {amount:C} for your ConsignmentGenie subscription.

We'll automatically retry the payment on {retryDate:MMMM dd, yyyy}. To avoid any service interruption, please update your payment method before then.

Update payment method: http://localhost:4200/billing

Questions? Contact us at support@microsaasbuilders.com
";

            return await SendSimpleEmailAsync(email, subject, htmlContent, textContent);
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
            var subject = "Payment received - Thank you!";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Payment Received</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: #27ae60; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">Payment Received</h1>
    </div>

    <div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;"">
        <p>Thank you! We've successfully received your payment of <strong>{amount:C}</strong> for your ConsignmentGenie subscription.</p>

        <p>Your service will continue uninterrupted. You can view and download your invoice using the link below:</p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{invoiceUrl}"" style=""display: inline-block; background: #27ae60; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">View Invoice</a>
        </div>

        <p>Thank you for choosing ConsignmentGenie for your business needs.</p>

        <p style=""color: #666; font-size: 14px; text-align: center;"">Questions? Contact us at support@microsaasbuilders.com</p>
    </div>
</body>
</html>";

            var textContent = $@"
Payment Received

Thank you! We've successfully received your payment of {amount:C} for your ConsignmentGenie subscription.

Your service will continue uninterrupted. View your invoice: {invoiceUrl}

Thank you for choosing ConsignmentGenie for your business needs.

Questions? Contact us at support@microsaasbuilders.com
";

            return await SendSimpleEmailAsync(email, subject, htmlContent, textContent);
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
            var subject = $"{integration} sync failed";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Sync Error</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: #e74c3c; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">Sync Error</h1>
    </div>

    <div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;"">
        <p>We encountered an error while syncing your <strong>{integration}</strong> data with ConsignmentGenie.</p>

        <div style=""background: #fff3cd; border: 1px solid #ffeeba; border-radius: 5px; padding: 15px; margin: 20px 0;"">
            <h4 style=""margin-top: 0; color: #856404;"">Error Details:</h4>
            <p style=""margin-bottom: 0; font-family: monospace; font-size: 14px; color: #856404;"">{errorMessage}</p>
        </div>

        <p>Please check your {integration} integration settings and try again. If the problem persists, contact our support team.</p>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""http://localhost:4200/owner/integrations"" style=""display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">Check Integration Settings</a>
        </div>

        <p style=""color: #666; font-size: 14px; text-align: center;"">Questions? Contact us at support@microsaasbuilders.com</p>
    </div>
</body>
</html>";

            var textContent = $@"
Sync Error

We encountered an error while syncing your {integration} data with ConsignmentGenie.

Error Details:
{errorMessage}

Please check your {integration} integration settings and try again. If the problem persists, contact our support team.

Check integration settings: http://localhost:4200/owner/integrations

Questions? Contact us at support@microsaasbuilders.com
";

            return await SendSimpleEmailAsync(email, subject, htmlContent, textContent);
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
            var developerEmail = _configuration["DeveloperEmail"] ?? "swashcode@outlook.com";
            var subject = $"New Suggestion: {suggestion.Type} from {suggestion.UserName}";

            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>New Suggestion Received</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: #667eea; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">New Suggestion Received</h1>
    </div>

    <div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;"">
        <div style=""background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea;"">
            <p><strong>From:</strong> {suggestion.UserName} ({suggestion.UserEmail})</p>
            <p><strong>Type:</strong> {suggestion.Type}</p>
            <p><strong>Submitted:</strong> {suggestion.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>

        <h3 style=""color: #667eea; margin: 30px 0 15px 0;"">Message:</h3>
        <div style=""background: #f5f5f5; padding: 15px; border-left: 4px solid #047857; margin: 10px 0; border-radius: 5px;"">
            {suggestion.Message.Replace("\n", "<br/>")}
        </div>

        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 14px; text-align: center; margin: 0;""><em>ConsignmentGenie Suggestion System</em></p>
    </div>
</body>
</html>";

            var textContent = $@"
New Suggestion Received

From: {suggestion.UserName} ({suggestion.UserEmail})
Type: {suggestion.Type}
Submitted: {suggestion.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC

Message:
{suggestion.Message}

---
ConsignmentGenie Suggestion System
";

            return await SendSimpleEmailAsync(developerEmail, subject, htmlContent, textContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send suggestion notification for suggestion {SuggestionId}", suggestion.Id);
            return false;
        }
    }

    public async Task<bool> SendSimpleEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var htmlBody = isHtml ? body : $"<pre>{body}</pre>";
        var textBody = isHtml ? null : body;

        return await SendSimpleEmailAsync(toEmail, subject, htmlBody, textBody);
    }

    private async Task<bool> SendSimpleEmailAsync(string toEmail, string subject, string htmlBody, string? textBody)
    {
        try
        {
            var fromEmail = _configuration["Resend:FromEmail"] ?? "noreply@microsaasbuilders.com";
            var fromName = _configuration["Resend:FromName"] ?? "ConsignmentGenie";

            var emailData = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = new[] { toEmail },
                subject = subject,
                html = htmlBody,
                text = textBody
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("emails", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var messageId = responseData.TryGetProperty("id", out var id) ? id.GetString() : "unknown";

                _logger.LogInformation("Email sent successfully to {Email} via Resend. Message ID: {MessageId}", toEmail, messageId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email to {Email} via Resend. Status: {Status}, Response: {Response}",
                    toEmail, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email} via Resend", toEmail);
            return false;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public async Task<bool> SendProviderInvitationAsync(string email, string providerName, string shopName, string inviteLink, string expirationDate)
    {
        _logger.LogInformation("[EMAIL] Starting provider invitation email to {Email} for provider {ProviderName} from shop {ShopName}", email, providerName, shopName);
        _logger.LogDebug("[EMAIL] Provider invitation details: InviteLink={InviteLink}, ExpirationDate={ExpirationDate}", inviteLink, expirationDate);

        try
        {
            var subject = $"Join {shopName} as a Provider - Invitation to ConsignmentGenie";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Provider Invitation</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: linear-gradient(135deg, #047857 0%, #065f46 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">You're Invited!</h1>
        <p style=""margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;"">Join {shopName} as a Consignment Provider</p>
    </div>

    <div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;"">
        <h2 style=""color: #047857; margin-top: 0;"">Hello {providerName}!</h2>

        <p>You've been invited to join <strong>{shopName}</strong> as a consignment provider on our ConsignmentGenie platform.</p>

        <p>As a provider, you'll be able to:</p>

        <div style=""background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #047857;"">
            <ul style=""margin: 0; padding-left: 20px;"">
                <li>Submit your items for consignment</li>
                <li>Track sales and earnings in real-time</li>
                <li>Receive automated payout calculations</li>
                <li>Access detailed sales reports</li>
                <li>Communicate with the shop owner</li>
            </ul>
        </div>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{inviteLink}""
               style=""background: linear-gradient(135deg, #047857 0%, #065f46 100%);
                      color: white;
                      padding: 15px 30px;
                      text-decoration: none;
                      border-radius: 25px;
                      font-weight: bold;
                      font-size: 16px;
                      display: inline-block;
                      box-shadow: 0 4px 15px rgba(4, 120, 87, 0.3);
                      transition: all 0.3s ease;"">
                Accept Invitation & Register
            </a>
        </div>

        <div style=""background: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 15px; margin: 20px 0; color: #856404;"">
            <strong>⏰ Important:</strong> This invitation expires on {expirationDate}. Please register before then to secure your access.
        </div>

        <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;"">

        <p style=""font-size: 14px; color: #666; margin-bottom: 5px;"">
            <strong>What happens next?</strong>
        </p>
        <ol style=""font-size: 14px; color: #666; margin: 0; padding-left: 20px;"">
            <li>Click the invitation link above</li>
            <li>Complete your provider registration</li>
            <li>Start submitting items for consignment</li>
            <li>Watch your earnings grow!</li>
        </ol>

        <div style=""margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #999; text-align: center;"">
            <p>If you have any questions, please contact {shopName} directly.</p>
            <p>This email was sent by ConsignmentGenie on behalf of {shopName}.</p>
        </div>
    </div>
</body>
</html>";

            var result = await SendSimpleEmailAsync(email, subject, htmlContent);
            _logger.LogInformation("[EMAIL] Provider invitation email send result for {Email}: {Result}", email, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Failed to send provider invitation email to {Email} for provider {ProviderName} from shop {ShopName}", email, providerName, shopName);
            return false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}