using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ConsignmentGenieContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Notification> CreateAsync(CreateNotificationRequest request)
    {
        try
        {
            _logger.LogInformation("Creating notification {Type} for user {ToUserId}", request.Type, request.ToUserId);

            // Always store in database first
            var notification = new Notification
            {
                FromUserId = request.FromUserId,
                FromType = request.FromType,
                ToUserId = request.ToUserId,
                ToType = request.ToType,
                ActionStatus = request.ActionStatus,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                OrganizationId = request.OrganizationId,
                Payload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : null,
                AttachmentUrl = request.AttachmentUrl,
                AttachmentName = request.AttachmentName,
                AttachmentType = request.AttachmentType,
                ActionUrl = request.ActionUrl,
                ItemId = request.ItemId,
                TransactionId = request.TransactionId,
                PayoutId = request.PayoutId,
                StatementId = request.StatementId,
                ItemRequestId = request.ItemRequestId,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                IsRead = false,
                EmailSent = false,
                SmsSent = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Check user preferences for email/SMS
            var preferences = await GetPreferencesAsync(request.ToUserId, request.ToType);

            // TODO: Send email/SMS based on preferences (placeholders for future implementation)
            // if (preferences.EmailEnabled && ShouldSendForType(preferences, request.Type))
            //     await SendEmailAsync(notification);
            // if (preferences.SmsEnabled)
            //     await SendSmsAsync(notification);

            _logger.LogInformation("Notification {Type} created successfully for user {ToUserId} with ID {NotificationId}",
                request.Type, request.ToUserId, notification.Id);

            // Broadcast updated unread count via SignalR
            // Note: SignalR broadcasts are handled in the API layer

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification {Type} for user {ToUserId}", request.Type, request.ToUserId);
            throw;
        }
    }

    public async Task<IEnumerable<Notification>> CreateBulkAsync(IEnumerable<CreateNotificationRequest> requests)
    {
        try
        {
            var notifications = new List<Notification>();

            foreach (var request in requests)
            {
                var notification = new Notification
                {
                    FromUserId = request.FromUserId,
                    FromType = request.FromType,
                    ToUserId = request.ToUserId,
                    ToType = request.ToType,
                    ActionStatus = request.ActionStatus,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    OrganizationId = request.OrganizationId,
                    Payload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : null,
                    AttachmentUrl = request.AttachmentUrl,
                    AttachmentName = request.AttachmentName,
                    AttachmentType = request.AttachmentType,
                    ActionUrl = request.ActionUrl,
                    ItemId = request.ItemId,
                    TransactionId = request.TransactionId,
                    PayoutId = request.PayoutId,
                    StatementId = request.StatementId,
                    ItemRequestId = request.ItemRequestId,
                    ReferenceType = request.ReferenceType,
                    ReferenceId = request.ReferenceId,
                    IsRead = false,
                    EmailSent = false,
                    SmsSent = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                notifications.Add(notification);
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} notifications in bulk", notifications.Count);

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk notifications");
            throw;
        }
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, string role, NotificationQueryParams queryParams)
    {
        try
        {
            var queryable = _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && n.DeletedAt == null);

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Type))
                queryable = queryable.Where(n => n.Type == queryParams.Type);

            if (queryParams.IsRead.HasValue)
                queryable = queryable.Where(n => n.IsRead == queryParams.IsRead.Value);

            if (queryParams.HasAttachment.HasValue)
                queryable = queryable.Where(n => queryParams.HasAttachment.Value ?
                    !string.IsNullOrEmpty(n.AttachmentUrl) :
                    string.IsNullOrEmpty(n.AttachmentUrl));

            if (queryParams.From.HasValue)
                queryable = queryable.Where(n => n.CreatedAt >= queryParams.From.Value);

            if (queryParams.To.HasValue)
                queryable = queryable.Where(n => n.CreatedAt <= queryParams.To.Value);

            // Get total count
            var totalCount = await queryable.CountAsync();

            // Apply sorting
            switch (queryParams.SortBy?.ToLowerInvariant())
            {
                case "readat":
                    queryable = queryParams.SortDirection?.ToLowerInvariant() == "asc" ?
                        queryable.OrderBy(n => n.ReadAt) :
                        queryable.OrderByDescending(n => n.ReadAt);
                    break;
                case "createdat":
                default:
                    queryable = queryParams.SortDirection?.ToLowerInvariant() == "asc" ?
                        queryable.OrderBy(n => n.CreatedAt) :
                        queryable.OrderByDescending(n => n.CreatedAt);
                    break;
            }

            // Apply pagination and get notifications with raw data
            var notificationsQuery = await queryable
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt,
                    n.ReferenceType,
                    n.ReferenceId,
                    n.ActionUrl,
                    n.Payload,
                    n.AttachmentUrl
                })
                .ToListAsync();

            // Map to DTOs and deserialize payload
            var notifications = notificationsQuery.Select(n => new NotificationDto
            {
                NotificationId = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TimeAgo = CalculateTimeAgo(n.CreatedAt),
                RelatedEntityType = n.ReferenceType,
                RelatedEntityId = n.ReferenceId,
                ActionUrl = n.ActionUrl,
                Metadata = !string.IsNullOrEmpty(n.Payload) ?
                    JsonSerializer.Deserialize<NotificationMetadata>(n.Payload) :
                    null
            }).ToList();

            return new PagedResult<NotificationDto>(notifications, totalCount, queryParams.Page, queryParams.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, string role)
    {
        try
        {
            _logger.LogInformation("Getting unread notification count for user {UserId} with role {Role}", userId, role);

            // First check total notifications for this user
            var totalNotifications = await _context.Notifications
                .Where(n => n.ToUserId == userId)
                .CountAsync();

            _logger.LogInformation("User {UserId} has {TotalCount} total notifications", userId, totalNotifications);

            // Check notifications by role
            var notificationsForRole = await _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role)
                .CountAsync();

            _logger.LogInformation("User {UserId} has {RoleCount} notifications for role {Role}", userId, notificationsForRole, role);

            // Check unread notifications without DeletedAt filter first
            var unreadWithoutDeleteFilter = await _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && !n.IsRead)
                .CountAsync();

            _logger.LogInformation("User {UserId} has {UnreadCount} unread notifications for role {Role} (ignoring DeletedAt)",
                userId, unreadWithoutDeleteFilter, role);

            // Check deleted notifications
            var deletedNotifications = await _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && n.DeletedAt != null)
                .CountAsync();

            _logger.LogInformation("User {UserId} has {DeletedCount} deleted notifications for role {Role}",
                userId, deletedNotifications, role);

            // Final count with all filters
            var unreadCount = await _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && !n.IsRead && n.DeletedAt == null)
                .CountAsync();

            _logger.LogInformation("Final unread count for user {UserId} with role {Role}: {UnreadCount}",
                userId, role, unreadCount);

            // Sample some notifications for debugging
            var sampleNotifications = await _context.Notifications
                .Where(n => n.ToUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new { n.Id, n.ToType, n.IsRead, n.DeletedAt, n.Type, n.Title })
                .ToListAsync();

            _logger.LogInformation("Sample notifications for user {UserId}: {@Notifications}", userId, sampleNotifications);

            return unreadCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);

                // Broadcast updated unread count via SignalR
                // Note: SignalR broadcasts are handled in the API layer
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task MarkAsUnreadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (notification.IsRead)
            {
                notification.IsRead = false;
                notification.ReadAt = null;
                notification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} marked as unread for user {UserId}", notificationId, userId);

                // Broadcast updated unread count via SignalR
                // Note: SignalR broadcasts are handled in the API layer
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as unread for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, string role)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.ToUserId == userId && n.ToType == role && !n.IsRead && n.DeletedAt == null)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = now;
                    notification.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Marked {Count} notifications as read for user {UserId} with role {Role}",
                    unreadNotifications.Count, userId, role);

                // Broadcast updated unread count via SignalR
                // Note: SignalR broadcasts are handled in the API layer
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task DeleteAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            // Soft delete
            notification.DeletedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification {NotificationId} soft deleted for user {UserId}", notificationId, userId);

            // Broadcast updated unread count via SignalR
            // Note: SignalR broadcasts are handled in the API layer
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, string role)
    {
        try
        {
            var preferences = await _context.UserNotificationPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

            // Return default preferences if none exist
            if (!preferences.Any())
            {
                return new NotificationPreferencesDto
                {
                    UserId = userId,
                    Role = role,
                    EmailEnabled = true,
                    SmsEnabled = false,
                    PushEnabled = true,
                    EmailItemSold = true,
                    EmailPayoutProcessed = true,
                    EmailPayoutPending = true,
                    EmailItemExpired = true,
                    EmailStatementReady = true,
                    EmailAccountUpdate = true,
                    DigestEnabled = false,
                    DigestFrequency = "daily",
                    DigestTime = "09:00",
                    DigestDay = 1,
                    PayoutPendingThreshold = 0
                };
            }

            // Map existing preferences to DTO (simplified mapping for now)
            var firstPreference = preferences.First();
            return new NotificationPreferencesDto
            {
                UserId = userId,
                Role = role,
                EmailEnabled = firstPreference.EmailEnabled,
                SmsEnabled = firstPreference.SmsEnabled,
                PushEnabled = firstPreference.PushEnabled,
                EmailItemSold = true,
                EmailPayoutProcessed = true,
                EmailPayoutPending = true,
                EmailItemExpired = true,
                EmailStatementReady = true,
                EmailAccountUpdate = true,
                DigestEnabled = firstPreference.InstantDelivery == false,
                DigestFrequency = "daily",
                DigestTime = "09:00",
                DigestDay = 1,
                PayoutPendingThreshold = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task UpdatePreferencesAsync(Guid userId, string role, UpdateNotificationPreferencesRequest request)
    {
        try
        {
            // For now, create a basic preference entry
            // This would need to be expanded to handle all the specific type preferences
            var preference = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preference == null)
            {
                preference = new UserNotificationPreference
                {
                    UserId = userId,
                    NotificationType = Core.Enums.NotificationType.ConsignorApproved, // Default type
                    EmailEnabled = request.EmailEnabled,
                    SmsEnabled = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserNotificationPreferences.Add(preference);
            }
            else
            {
                preference.EmailEnabled = request.EmailEnabled;
                preference.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated notification preferences for user {UserId} with role {Role}", userId, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    private static string CalculateTimeAgo(DateTime createdAt)
    {
        var now = DateTime.UtcNow;
        var timeSpan = now - createdAt;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";

        return createdAt.ToString("MMM dd, yyyy");
    }

    public async Task MarkAsImportantAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (!notification.IsImportant)
            {
                notification.IsImportant = true;
                notification.MarkedImportantAt = DateTime.UtcNow;
                notification.MarkedImportantByUserId = userId;
                notification.UpdatedAt = DateTime.UtcNow;

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification {NotificationId} as important for user {UserId}", notificationId, userId);
                // Note: SignalR broadcasts are handled in the API layer
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as important for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task MarkAsNotImportantAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return;
            }

            if (notification.IsImportant)
            {
                notification.IsImportant = false;
                notification.MarkedImportantAt = null;
                notification.MarkedImportantByUserId = null;
                notification.UpdatedAt = DateTime.UtcNow;

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification {NotificationId} as not important for user {UserId}", notificationId, userId);
                // Note: SignalR broadcasts are handled in the API layer
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as not important for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task CompleteNotificationActionAsync(Guid referenceId, string referenceType, Guid completedByUserId)
    {
        try
        {
            _logger.LogInformation("Completing notification action for {ReferenceType} {ReferenceId} by user {UserId}",
                referenceType, referenceId, completedByUserId);

            var notifications = await _context.Notifications
                .Where(n => n.ReferenceId == referenceId &&
                           n.ReferenceType == referenceType &&
                           n.ActionStatus == "pending")
                .ToListAsync();

            if (!notifications.Any())
            {
                _logger.LogInformation("No pending notifications found for {ReferenceType} {ReferenceId}", referenceType, referenceId);
                return;
            }

            foreach (var notification in notifications)
            {
                notification.ActionStatus = "completed";
                notification.ActionCompletedAt = DateTime.UtcNow;
                notification.ActionCompletedByUserId = completedByUserId;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            _context.Notifications.UpdateRange(notifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed {Count} notification actions for {ReferenceType} {ReferenceId}",
                notifications.Count, referenceType, referenceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing notification action for {ReferenceType} {ReferenceId}", referenceType, referenceId);
            throw;
        }
    }

    public async Task AcknowledgeNotificationAsync(Guid notificationId, Guid acknowledgedByUserId)
    {
        try
        {
            _logger.LogInformation("Acknowledging notification {NotificationId} by user {UserId}",
                notificationId, acknowledgedByUserId);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found for acknowledgment", notificationId);
                return;
            }

            if (!notification.IsAcknowledged)
            {
                notification.IsAcknowledged = true;
                notification.AcknowledgedAt = DateTime.UtcNow;
                notification.AcknowledgedByUserId = acknowledgedByUserId;
                notification.UpdatedAt = DateTime.UtcNow;

                // Also mark as read if not already read
                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully acknowledged notification {NotificationId} by user {UserId}",
                    notificationId, acknowledgedByUserId);
            }
            else
            {
                _logger.LogInformation("Notification {NotificationId} was already acknowledged", notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging notification {NotificationId} by user {UserId}",
                notificationId, acknowledgedByUserId);
            throw;
        }
    }
}