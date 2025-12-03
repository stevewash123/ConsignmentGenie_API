using ConsignmentGenie.Application.Models.Notifications;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using System.Text;

namespace ConsignmentGenie.Application.Services;

public class NotificationTemplateService : INotificationTemplateService
{
    private readonly Dictionary<NotificationType, NotificationTemplate> _templates;

    public NotificationTemplateService()
    {
        _templates = InitializeTemplates();
    }

    public NotificationTemplate GetTemplate(NotificationType type)
    {
        return _templates.TryGetValue(type, out var template)
            ? template
            : _templates[NotificationType.Info]; // Fallback template
    }

    public EmailMessage RenderTemplate(NotificationType type, Dictionary<string, string> data, string recipientEmail)
    {
        var template = GetTemplate(type);

        return new EmailMessage
        {
            To = recipientEmail,
            Subject = RenderString(template.Subject, data),
            Body = RenderString(template.Body, data),
            IsHtml = template.IsHtml
        };
    }

    private string RenderString(string template, Dictionary<string, string> data)
    {
        var result = template;

        foreach (var kvp in data)
        {
            var placeholder = $"{{{kvp.Key}}}";
            result = result.Replace(placeholder, kvp.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private Dictionary<NotificationType, NotificationTemplate> InitializeTemplates()
    {
        return new Dictionary<NotificationType, NotificationTemplate>
        {
            // Consignor Notifications
            [NotificationType.ProviderApproved] = new NotificationTemplate
            {
                Subject = "Welcome to {ShopName} - Consignor Application Approved!",
                Body = @"
                    <html>
                    <body>
                        <h2>Congratulations, {ConsignorName}!</h2>
                        <p>Your application to become a provider for <strong>{ShopName}</strong> has been approved.</p>
                        <p>You can now start listing items for consignment. Here's what you need to know:</p>
                        <ul>
                            <li>Commission Rate: {CommissionRate}%</li>
                            <li>Minimum Item Value: {MinItemValue}</li>
                            <li>Payment Schedule: {PaymentSchedule}</li>
                        </ul>
                        <p><a href=""{LoginUrl}"" style=""background-color: #047857; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Access Your Portal</a></p>
                        <p>Welcome aboard!</p>
                        <p><em>{ShopName} Team</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            [NotificationType.ProviderRejected] = new NotificationTemplate
            {
                Subject = "Consignor Application Update - {ShopName}",
                Body = @"
                    <html>
                    <body>
                        <h2>Consignor Application Update</h2>
                        <p>Thank you for your interest in becoming a provider for <strong>{ShopName}</strong>.</p>
                        <p>After reviewing your application, we've decided not to move forward at this time.</p>
                        <p>Reason: {RejectionReason}</p>
                        <p>You're welcome to reapply in the future when circumstances change.</p>
                        <p>Thank you for your understanding.</p>
                        <p><em>{ShopName} Team</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            [NotificationType.ItemSold] = new NotificationTemplate
            {
                Subject = "Great news! Your item \"{ItemName}\" just sold!",
                Body = @"
                    <html>
                    <body>
                        <h2>Item Sold! 🎉</h2>
                        <p>Hello {ConsignorName},</p>
                        <p>Congratulations! Your item <strong>{ItemName}</strong> has been sold.</p>
                        <div style=""background-color: #f0fdf4; padding: 15px; border-left: 4px solid #047857; margin: 10px 0;"">
                            <p><strong>Sale Details:</strong></p>
                            <ul>
                                <li>Sale Price: {SalePrice}</li>
                                <li>Your Share: {ConsignorAmount}</li>
                                <li>Commission: {CommissionAmount}</li>
                                <li>Sale Date: {SaleDate}</li>
                            </ul>
                        </div>
                        <p>Your earnings will be included in the next payout cycle.</p>
                        <p><a href=""{PortalUrl}"" style=""background-color: #047857; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">View Details</a></p>
                        <p>Thanks for being part of our consignment community!</p>
                        <p><em>{ShopName} Team</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            [NotificationType.PayoutReady] = new NotificationTemplate
            {
                Subject = "Your payout of {PayoutAmount} is ready - {ShopName}",
                Body = @"
                    <html>
                    <body>
                        <h2>Payout Ready! 💰</h2>
                        <p>Hello {ConsignorName},</p>
                        <p>Your payout is ready for processing!</p>
                        <div style=""background-color: #f0fdf4; padding: 15px; border-left: 4px solid #047857; margin: 10px 0;"">
                            <p><strong>Payout Details:</strong></p>
                            <ul>
                                <li>Amount: {PayoutAmount}</li>
                                <li>Items Sold: {ItemCount}</li>
                                <li>Period: {PayoutPeriod}</li>
                                <li>Payment Method: {PaymentMethod}</li>
                            </ul>
                        </div>
                        <p>Payment will be processed within {ProcessingDays} business days.</p>
                        <p><a href=""{PortalUrl}"" style=""background-color: #047857; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">View Payout Details</a></p>
                        <p><em>{ShopName} Team</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            // Owner Notifications
            [NotificationType.NewProviderRequest] = new NotificationTemplate
            {
                Subject = "New Consignor Application - {ConsignorName}",
                Body = @"
                    <html>
                    <body>
                        <h2>New Consignor Application</h2>
                        <p>You have a new provider application waiting for review:</p>
                        <div style=""background-color: #f9fafb; padding: 15px; border: 1px solid #e5e7eb; border-radius: 5px; margin: 10px 0;"">
                            <p><strong>Consignor Details:</strong></p>
                            <ul>
                                <li>Name: {ConsignorName}</li>
                                <li>Email: {ProviderEmail}</li>
                                <li>Phone: {ProviderPhone}</li>
                                <li>Applied: {ApplicationDate}</li>
                            </ul>
                            <p><strong>Application Notes:</strong></p>
                            <p>{ApplicationNotes}</p>
                        </div>
                        <p><a href=""{ReviewUrl}"" style=""background-color: #047857; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Review Application</a></p>
                        <p><em>ConsignmentGenie</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            [NotificationType.SuggestionSubmitted] = new NotificationTemplate
            {
                Subject = "New {SuggestionType}: {SuggestionTitle} from {SuggesterName}",
                Body = @"
                    <html>
                    <body>
                        <h2>New Suggestion Received</h2>
                        <p><strong>From:</strong> {SuggesterName} ({SuggesterEmail})</p>
                        <p><strong>Type:</strong> {SuggestionType}</p>
                        <p><strong>Submitted:</strong> {SubmittedAt}</p>
                        <div style=""background-color: #f5f5f5; padding: 15px; border-left: 4px solid #047857; margin: 10px 0;"">
                            <h3>Message:</h3>
                            <p>{Message}</p>
                        </div>
                        <p><strong>Organization:</strong> {OrganizationName}</p>
                        <hr/>
                        <p><em>ConsignmentGenie Suggestion System</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            // System Notifications
            [NotificationType.WelcomeEmail] = new NotificationTemplate
            {
                Subject = "Welcome to {OrganizationName} - Get Started with ConsignmentGenie!",
                Body = @"
                    <html>
                    <body>
                        <h2>Welcome to ConsignmentGenie!</h2>
                        <p>Hello {UserName},</p>
                        <p>Welcome to <strong>{OrganizationName}</strong>! We're excited to help you manage your consignment business.</p>
                        <div style=""background-color: #f0fdf4; padding: 15px; border-left: 4px solid #047857; margin: 10px 0;"">
                            <p><strong>Getting Started:</strong></p>
                            <ol>
                                <li>Complete your shop setup</li>
                                <li>Add your first providers</li>
                                <li>Configure commission rates</li>
                                <li>Start tracking inventory</li>
                            </ol>
                        </div>
                        <p><a href=""{LoginUrl}"" style=""background-color: #047857; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Access Your Dashboard</a></p>
                        <p>If you have questions, feel free to reach out to our support team.</p>
                        <p><em>The ConsignmentGenie Team</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            [NotificationType.PasswordReset] = new NotificationTemplate
            {
                Subject = "Password Reset Request - {OrganizationName}",
                Body = @"
                    <html>
                    <body>
                        <h2>Password Reset Request</h2>
                        <p>Hello {UserName},</p>
                        <p>We received a request to reset your password for your {OrganizationName} account.</p>
                        <p><a href=""{ResetUrl}"" style=""background-color: #047857; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Reset Your Password</a></p>
                        <p>This link will expire in {ExpirationHours} hours.</p>
                        <p>If you didn't request this password reset, please ignore this email.</p>
                        <p><em>The ConsignmentGenie Team</em></p>
                    </body>
                    </html>",
                IsHtml = true
            },

            // Fallback template
            [NotificationType.Info] = new NotificationTemplate
            {
                Subject = "Notification from {OrganizationName}",
                Body = @"
                    <html>
                    <body>
                        <h2>Notification</h2>
                        <p>{Message}</p>
                        <p><em>{OrganizationName}</em></p>
                    </body>
                    </html>",
                IsHtml = true
            }
        };
    }
}