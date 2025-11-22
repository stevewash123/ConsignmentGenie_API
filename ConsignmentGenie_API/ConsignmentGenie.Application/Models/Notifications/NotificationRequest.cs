using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Models.Notifications;

public class NotificationRequest
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public NotificationRequest() { }

    public NotificationRequest(Guid userId, NotificationType type, Dictionary<string, string>? data = null, NotificationPriority priority = NotificationPriority.Normal)
    {
        UserId = userId;
        Type = type;
        Data = data ?? new Dictionary<string, string>();
        Priority = priority;
    }
}

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}

public class NotificationTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}