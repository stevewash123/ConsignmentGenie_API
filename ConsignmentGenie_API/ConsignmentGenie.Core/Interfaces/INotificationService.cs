using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.DTOs;

namespace ConsignmentGenie.Core.Interfaces;

public interface IProviderNotificationService
{
    /// <summary>
    /// Creates a new notification and sends it via enabled channels
    /// </summary>
    Task CreateNotificationAsync(CreateNotificationRequest request);

    /// <summary>
    /// Gets paginated notifications for a user
    /// </summary>
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, NotificationQueryParams queryParams);

    /// <summary>
    /// Gets unread notification count for a user
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId);

    /// <summary>
    /// Deletes a notification
    /// </summary>
    Task DeleteAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Gets notification preferences for a user
    /// </summary>
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId);

    /// <summary>
    /// Updates notification preferences for a user
    /// </summary>
    Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferencesRequest request);

    /// <summary>
    /// Processes digest notifications (for background job)
    /// </summary>
    Task ProcessDigestNotificationsAsync();
}