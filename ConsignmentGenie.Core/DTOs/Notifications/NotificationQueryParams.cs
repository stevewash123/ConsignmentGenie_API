using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Notifications;

public class NotificationQueryParams
{
    // Pagination
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    // Filtering
    public string? Type { get; set; }              // Filter by notification type
    public bool? IsRead { get; set; }              // Filter by read status (replaces UnreadOnly)
    public bool? HasAttachment { get; set; }       // Filter by attachment presence

    // Date range filtering
    public DateTime? From { get; set; }            // Created date from
    public DateTime? To { get; set; }              // Created date to

    // Sorting
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";

    // Legacy support for existing code
    public bool UnreadOnly
    {
        get => IsRead.HasValue && !IsRead.Value;
        set => IsRead = value ? false : (bool?)null;
    }
}