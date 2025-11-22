using ConsignmentGenie.Application.Models.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Send a notification to a user based on their notification preferences
    /// </summary>
    Task<bool> SendAsync(NotificationRequest request);

    /// <summary>
    /// Send multiple notifications in bulk
    /// </summary>
    Task<Dictionary<Guid, bool>> SendBulkAsync(IEnumerable<NotificationRequest> requests);

    /// <summary>
    /// Get user's notification preferences for a specific notification type
    /// </summary>
    Task<UserNotificationPreference?> GetUserPreferenceAsync(Guid userId, NotificationType type);

    /// <summary>
    /// Update user's notification preferences
    /// </summary>
    Task<bool> UpdateUserPreferenceAsync(Guid userId, NotificationType type, bool emailEnabled, bool smsEnabled = false, bool slackEnabled = false);
}

public interface INotificationTemplateService
{
    /// <summary>
    /// Get the template for a notification type
    /// </summary>
    NotificationTemplate GetTemplate(NotificationType type);

    /// <summary>
    /// Render a template with data
    /// </summary>
    EmailMessage RenderTemplate(NotificationType type, Dictionary<string, string> data, string recipientEmail);
}