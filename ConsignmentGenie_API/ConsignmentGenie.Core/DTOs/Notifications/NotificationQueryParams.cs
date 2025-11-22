namespace ConsignmentGenie.Core.DTOs.Notifications;

public class NotificationQueryParams
{
    public bool? IsRead { get; set; }           // Filter by read status
    public string? Type { get; set; }           // Filter by type
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}