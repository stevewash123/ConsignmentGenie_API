using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class UnifiedNotificationService : INotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<UnifiedNotificationService> _logger;

    public UnifiedNotificationService(
        ConsignmentGenieContext context,
        ILogger<UnifiedNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Notification> CreateAsync(CreateNotificationRequest request)
    {
        try
        {
            _logger.LogInformation("Creating notification {Type} for user {ToUserId} with role {ToType}",
                request.Type, request.ToUserId, request.ToType);

            // 1. Always store in database first
            var notification = new Notification
            {
                OrganizationId = request.OrganizationId,
                FromUserId = request.FromUserId,
                FromType = request.FromType,
                ToUserId = request.ToUserId,
                ToType = request.ToType,
                ActionStatus = request.ActionStatus,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
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

            _logger.LogInformation("Notification {NotificationId} created successfully", notification.Id);

            // 2. Check user preferences for email/SMS (placeholder for future implementation)
            // var preferences = await GetPreferencesAsync(request.UserId, request.Role);

            // 3. Send email/SMS based on preferences (placeholders for now)
            // TODO: Implement in email/SMS stories
            // if (preferences.ShouldSendEmail(request.Type)) await SendEmailAsync(notification);
            // if (preferences.ShouldSendSms(request.Type)) await SendSmsAsync(notification);

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
            var requestList = requests.ToList();

            _logger.LogInformation("Creating {Count} notifications in bulk", requestList.Count);

            foreach (var request in requestList)
            {
                var notification = new Notification
                {
                    OrganizationId = request.OrganizationId,
                    FromUserId = request.FromUserId,
                    FromType = request.FromType,
                    ToUserId = request.ToUserId,
                    ToType = request.ToType,
                    ActionStatus = request.ActionStatus,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
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

                notifications.Add(notification);
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created {Count} notifications in bulk", notifications.Count);

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
            var query = _context.Notifications
                .Where(n => n.ToUserId == userId &&
                           n.ToType == role &&
                           n.DeletedAt == null);

            // Apply filters
            if (queryParams.UnreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            if (!string.IsNullOrEmpty(queryParams.Type))
            {
                query = query.Where(n => n.Type == queryParams.Type);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            // Map to DTOs
            var notificationDtos = notifications.Select(n => new NotificationDto
            {
                NotificationId = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt),
                ReferenceType = n.ReferenceType,
                ReferenceId = n.ReferenceId,
                // Populate legacy properties for backward compatibility
                RelatedEntityType = n.ReferenceType,
                RelatedEntityId = n.ReferenceId,
                ActionUrl = n.ActionUrl
            }).ToList();

            return new PagedResult<NotificationDto>(
                notificationDtos,
                totalCount,
                queryParams.Page,
                queryParams.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, string role)
    {
        try
        {
            return await _context.Notifications
                .Where(n => n.ToUserId == userId &&
                           n.ToType == role &&
                           !n.IsRead &&
                           n.DeletedAt == null)
                .CountAsync();
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

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification {NotificationId} as read for user {UserId}", notificationId, userId);
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

                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification {NotificationId} as unread for user {UserId}", notificationId, userId);
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
                .Where(n => n.ToUserId == userId &&
                           n.ToType == role &&
                           !n.IsRead &&
                           n.DeletedAt == null)
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

                _context.Notifications.UpdateRange(unreadNotifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} notifications as read for user {UserId} with role {Role}",
                    unreadNotifications.Count, userId, role);
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

            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted notification {NotificationId} for user {UserId}", notificationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
            throw;
        }
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId, string role)
    {
        // TODO: Implement notification preferences logic
        // For now, return default preferences
        _logger.LogInformation("Getting notification preferences for user {UserId} with role {Role} - returning defaults", userId, role);

        return new NotificationPreferencesDto
        {
            UserId = userId,
            Role = role,
            EmailEnabled = true,
            SmsEnabled = false,
            PushEnabled = true,
            DigestEnabled = false,
            DigestFrequency = "daily"
        };
    }

    public async Task UpdatePreferencesAsync(Guid userId, string role, UpdateNotificationPreferencesRequest request)
    {
        try
        {
            _logger.LogInformation("Updating notification preferences for user {UserId} with role {Role}", userId, role);

            // TODO: Implement notification preferences storage and update logic
            // This is a placeholder that will be implemented when the preferences system is built

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user {UserId} with role {Role}", userId, role);
            throw;
        }
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return dateTime.ToString("MMM dd");
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