using ConsignmentGenie.Application.Models.Notifications;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

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