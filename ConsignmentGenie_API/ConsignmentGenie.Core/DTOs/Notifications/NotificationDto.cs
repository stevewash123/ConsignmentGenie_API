namespace ConsignmentGenie.Core.DTOs.Notifications;

public class NotificationDto
{
    public Guid NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty; // "2 hours ago", "Yesterday"

    // For navigation
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? ActionUrl { get; set; } // "/provider/items/xxx" or "/provider/payouts/xxx"

    // Type-specific data
    public NotificationMetadata? Metadata { get; set; }
}