using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Core.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Creates a new notification and sends it via enabled channels
    /// </summary>
    Task<Notification> CreateAsync(CreateNotificationRequest request);

    /// <summary>
    /// Creates multiple notifications in bulk
    /// </summary>
    Task<IEnumerable<Notification>> CreateBulkAsync(IEnumerable<CreateNotificationRequest> requests);

    /// <summary>
    /// Gets paginated notifications for a user with role filtering
    /// </summary>
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, string role, NotificationQueryParams queryParams);

    /// <summary>
    /// Gets unread notification count for a user in a specific role
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, string role);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Marks a notification as unread
    /// </summary>
    Task MarkAsUnreadAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Marks all notifications as read for a user in a specific role
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, string role);

    /// <summary>
    /// Deletes a notification (soft delete)
    /// </summary>
    Task DeleteAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Gets notification preferences for a user in a specific role
    /// </summary>
    Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, string role);

    /// <summary>
    /// Updates notification preferences for a user in a specific role
    /// </summary>
    Task UpdatePreferencesAsync(Guid userId, string role, UpdateNotificationPreferencesRequest request);

    /// <summary>
    /// Marks a notification as important
    /// </summary>
    Task MarkAsImportantAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Marks a notification as not important
    /// </summary>
    Task MarkAsNotImportantAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Marks an actionable notification as completed
    /// </summary>
    Task CompleteNotificationActionAsync(Guid referenceId, string referenceType, Guid completedByUserId);

    /// <summary>
    /// Acknowledges receipt of a notification (explicit user acknowledgment)
    /// </summary>
    Task AcknowledgeNotificationAsync(Guid notificationId, Guid acknowledgedByUserId);
}

public interface IConsignorNotificationService
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