using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface INotificationService
{
    // Create notifications
    Task<Guid> CreateNotificationAsync(Guid organizationId, Guid? userId, string title, string message,
        NotificationType type = NotificationType.Info, NotificationPriority priority = NotificationPriority.Normal);

    Task CreateBulkNotificationAsync(Guid organizationId, List<Guid> userIds, string title, string message,
        NotificationType type = NotificationType.Info, NotificationPriority priority = NotificationPriority.Normal);

    // Get notifications
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool includeRead = false, int limit = 50);
    Task<List<NotificationDto>> GetOrganizationNotificationsAsync(Guid organizationId, int limit = 50);
    Task<int> GetUnreadCountAsync(Guid userId);

    // Mark as read/dismissed
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
    Task DismissNotificationAsync(Guid notificationId, Guid userId);

    // Send via email/SMS
    Task SendEmailNotificationAsync(Guid notificationId);
    Task SendSmsNotificationAsync(Guid notificationId);

    // System notifications (for common scenarios)
    Task NotifyLowStockAsync(Guid organizationId, Guid providerId, string itemName, int currentStock, int minimumStock);
    Task NotifyPaymentFailedAsync(Guid organizationId, Guid userId, string errorMessage);
    Task NotifyPayoutReadyAsync(Guid organizationId, Guid providerId, decimal amount);
    Task NotifyItemSoldAsync(Guid organizationId, Guid providerId, string itemName, decimal salePrice);
    Task NotifyTrialExpiringAsync(Guid organizationId, int daysRemaining);
    Task NotifySubscriptionCancelledAsync(Guid organizationId, DateTime cancelDate);
}