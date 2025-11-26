using ConsignmentGenie.Application.Models.Notifications;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly INotificationTemplateService _templateService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ConsignmentGenieContext context,
        INotificationTemplateService templateService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _templateService = templateService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(NotificationRequest request)
    {
        try
        {
            _logger.LogInformation("Processing notification {NotificationType} for user {UserId}", request.Type, request.UserId);

            // Get user information
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for notification {NotificationType}", request.UserId, request.Type);
                return false;
            }

            // Get user preferences for this notification type
            var preference = await GetUserPreferenceAsync(request.UserId, request.Type);

            bool emailSent = false;

            // Send via Email (MVP implementation)
            if (preference?.EmailEnabled ?? true) // Default to email if no preference exists
            {
                try
                {
                    // Add default data if missing
                    EnsureDefaultData(request.Data, user);

                    // Render the email from template
                    var emailMessage = _templateService.RenderTemplate(request.Type, request.Data, user.Email);

                    // Send using the existing email service
                    emailSent = await SendEmailAsync(emailMessage);

                    if (emailSent)
                    {
                        _logger.LogInformation("Email notification sent successfully to {Email} for {NotificationType}", user.Email, request.Type);
                    }
                    else
                    {
                        _logger.LogWarning("Email notification failed for {Email} and {NotificationType}", user.Email, request.Type);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email notification for {NotificationType} to user {UserId}", request.Type, request.UserId);
                    emailSent = false;
                }
            }
            else
            {
                _logger.LogInformation("Email notification disabled for user {UserId} and type {NotificationType}", request.UserId, request.Type);
            }

            // Future: SMS Implementation
            // if (preference?.SmsEnabled == true && !string.IsNullOrEmpty(user.Phone))
            // {
            //     await _smsService.SendAsync(...);
            // }

            // Future: Slack Implementation
            // if (preference?.SlackEnabled == true && !string.IsNullOrEmpty(user.SlackUserId))
            // {
            //     await _slackService.SendAsync(...);
            // }

            // For now, consider successful if any channel succeeded
            return emailSent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing notification {NotificationType} for user {UserId}", request.Type, request.UserId);
            return false;
        }
    }

    public async Task<Dictionary<Guid, bool>> SendBulkAsync(IEnumerable<NotificationRequest> requests)
    {
        var results = new Dictionary<Guid, bool>();

        foreach (var request in requests)
        {
            var success = await SendAsync(request);
            results[request.UserId] = success;
        }

        return results;
    }

    public async Task<UserNotificationPreference?> GetUserPreferenceAsync(Guid userId, NotificationType type)
    {
        return await _context.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type);
    }

    public async Task<bool> UpdateUserPreferenceAsync(Guid userId, NotificationType type, bool emailEnabled, bool smsEnabled = false, bool slackEnabled = false)
    {
        try
        {
            var preference = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == type);

            if (preference == null)
            {
                preference = new UserNotificationPreference
                {
                    UserId = userId,
                    NotificationType = type,
                    EmailEnabled = emailEnabled,
                    SmsEnabled = smsEnabled,
                    SlackEnabled = slackEnabled
                };
                _context.UserNotificationPreferences.Add(preference);
            }
            else
            {
                preference.EmailEnabled = emailEnabled;
                preference.SmsEnabled = smsEnabled;
                preference.SlackEnabled = slackEnabled;
                preference.UpdatedAt = DateTime.UtcNow;
                _context.UserNotificationPreferences.Update(preference);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preference for user {UserId} and type {NotificationType}", userId, type);
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(EmailMessage emailMessage)
    {
        try
        {
            // Use the new simplified email interface
            return await _emailService.SendSimpleEmailAsync(
                emailMessage.To,
                emailMessage.Subject,
                emailMessage.Body,
                emailMessage.IsHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", emailMessage.To);
            return false;
        }
    }

    private void EnsureDefaultData(Dictionary<string, string> data, User user)
    {
        // Add common data that's always available
        data.TryAdd("UserName", user.Email.Split('@')[0]); // Use email username part since BusinessName doesn't exist
        data.TryAdd("UserEmail", user.Email);

        // Add organization data if available
        if (user.Organization != null)
        {
            data.TryAdd("OrganizationName", user.Organization.Name);
            data.TryAdd("ShopName", user.Organization.Name);
        }

        // Add URLs
        var baseUrl = _configuration["ClientUrl"] ?? "http://localhost:4200";
        data.TryAdd("LoginUrl", $"{baseUrl}/login");
        data.TryAdd("PortalUrl", $"{baseUrl}/owner/dashboard");
        data.TryAdd("ReviewUrl", $"{baseUrl}/owner/providers");

        // Add timestamp
        data.TryAdd("SubmittedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
    }
}