using ConsignmentGenie.Application.Models.Notifications;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class ConsignorNotificationService : IConsignorNotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService; // Email service for sending emails
    private readonly ILogger<ConsignorNotificationService> _logger;

    public ConsignorNotificationService(
        ConsignmentGenieContext context,
        IEmailService emailService,
        ILogger<ConsignorNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(CreateNotificationRequest request)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        try
        {
            // 🏗️ AGGREGATE ROOT PATTERN: Create notification aggregate root
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
                SmsSent = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Check user preferences and send email if enabled
            var requestType = request.Type; // Move string comparison out of LINQ query
            var userPreference = await _context.UserNotificationPreferences
                .Where(p => p.UserId == request.ToUserId)
                .ToListAsync(); // Execute query first
            var matchingPreference = userPreference.FirstOrDefault(p => p.NotificationType.ToString() == requestType);

            if (matchingPreference?.EmailEnabled ?? true) // Default to email enabled
            {
                try
                {
                    // Build data dictionary for email template
                    var emailData = BuildEmailData(request);

                    // Get user's email address for sending email
                    var user = await _context.Users
                        .Where(u => u.Id == request.ToUserId)
                        .FirstOrDefaultAsync();

                    if (user?.Email != null)
                    {
                        // Send email only - don't create duplicate notification
                        var emailSent = await _emailService.SendSimpleEmailAsync(
                            user.Email,
                            request.Title,
                            request.Message,
                            true
                        );

                        if (emailSent)
                        {
                            notification.EmailSent = true;
                            notification.EmailSentAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification for {Type} to user {UserId}", request.Type, request.ToUserId);
                }
            }

            _logger.LogInformation("Created notification {Type} for user {UserId}", request.Type, request.ToUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification {Type} for user {UserId}", request.Type, request.ToUserId);
            throw;
        }
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, NotificationQueryParams queryParams)
    {
        var query = _context.Notifications
            .Where(n => n.ToUserId == userId)
            .AsQueryable();

        if (queryParams.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrEmpty(queryParams.Type))
        {
            query = query.Where(n => n.Type == queryParams.Type);
        }

        // No need to filter expired notifications since ExpiresAt field was removed

        var totalCount = await query.CountAsync();

        var notificationEntities = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        var notifications = notificationEntities.Select(n => new NotificationDto
        {
            NotificationId = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            ReferenceType = n.ReferenceType, // Use new fields instead of legacy ones
            ReferenceId = n.ReferenceId,
            TimeAgo = CalculateTimeAgo(n.CreatedAt),
            ActionUrl = BuildActionUrl(n.ReferenceType, n.ReferenceId), // Updated to use new fields
            Metadata = n.Metadata != null ? JsonSerializer.Deserialize<NotificationMetadata>(n.Metadata) : null
        }).ToList();

        return new PagedResult<NotificationDto>(notifications, totalCount, queryParams.Page, queryParams.PageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.ToUserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var unreadNotifications = await _context.Notifications
            .Where(n => n.ToUserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid notificationId, Guid userId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.ToUserId == userId);

        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(Guid userId)
    {
        // Get existing NotificationPreferences entity if it exists
        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Return defaults
            return new NotificationPreferencesDto
            {
                EmailEnabled = true,
                EmailItemSold = true,
                EmailPayoutProcessed = true,
                EmailPayoutPending = false,
                EmailItemExpired = false,
                EmailStatementReady = true,
                EmailAccountUpdate = true,
                DigestFrequency = "instant",
                DigestTime = "09:00",
                DigestDay = 1,
                PayoutPendingThreshold = 50.00m
            };
        }

        return new NotificationPreferencesDto
        {
            EmailEnabled = preferences.EmailEnabled,
            EmailItemSold = preferences.EmailItemSold,
            EmailPayoutProcessed = preferences.EmailPayoutProcessed,
            EmailPayoutPending = preferences.EmailPayoutPending,
            EmailItemExpired = preferences.EmailItemExpired,
            EmailStatementReady = preferences.EmailStatementReady,
            EmailAccountUpdate = preferences.EmailAccountUpdate,
            DigestFrequency = preferences.DigestMode,
            DigestTime = preferences.DigestTime,
            DigestDay = preferences.DigestDay,
            PayoutPendingThreshold = preferences.PayoutPendingThreshold
        };
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferencesRequest request)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // 🏗️ AGGREGATE ROOT PATTERN: Create notification preferences aggregate
            preferences = new NotificationPreferences
            {
                UserId = userId
            };
            _context.NotificationPreferences.Add(preferences);
        }

        // Update preferences
        preferences.EmailEnabled = request.EmailEnabled;
        preferences.EmailItemSold = request.EmailItemSold;
        preferences.EmailPayoutProcessed = request.EmailPayoutProcessed;
        preferences.EmailPayoutPending = request.EmailPayoutPending;
        preferences.EmailItemExpired = request.EmailItemExpired;
        preferences.EmailStatementReady = request.EmailStatementReady;
        preferences.EmailAccountUpdate = request.EmailAccountUpdate;
        preferences.DigestMode = request.DigestMode;
        preferences.DigestTime = request.DigestTime;
        preferences.DigestDay = request.DigestDay ?? 1;
        preferences.PayoutPendingThreshold = request.PayoutPendingThreshold ?? 50.00m;
        preferences.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetPreferencesAsync(userId);
    }

    public async Task ProcessDigestNotificationsAsync()
    {
        // TODO: Implement digest processing for daily/weekly notifications
        // This would be called by a background job
        _logger.LogInformation("Processing digest notifications (not yet implemented)");
        await Task.CompletedTask;
    }

    private Dictionary<string, string> BuildEmailData(CreateNotificationRequest request)
    {
        var data = new Dictionary<string, string>
        {
            ["Title"] = request.Title,
            ["Message"] = request.Message
        };

        if (request.Metadata != null)
        {
            // Add metadata fields to email data
            if (!string.IsNullOrEmpty(request.Metadata.ItemTitle))
                data["ItemTitle"] = request.Metadata.ItemTitle;
            if (!string.IsNullOrEmpty(request.Metadata.ItemSku))
                data["ItemSku"] = request.Metadata.ItemSku;
            if (request.Metadata.SalePrice.HasValue)
                data["SalePrice"] = request.Metadata.SalePrice.Value.ToString("C");
            if (request.Metadata.EarningsAmount.HasValue)
                data["EarningsAmount"] = request.Metadata.EarningsAmount.Value.ToString("C");
            if (request.Metadata.PayoutAmount.HasValue)
                data["PayoutAmount"] = request.Metadata.PayoutAmount.Value.ToString("C");
            if (!string.IsNullOrEmpty(request.Metadata.PayoutMethod))
                data["PayoutMethod"] = request.Metadata.PayoutMethod;
            if (!string.IsNullOrEmpty(request.Metadata.PayoutNumber))
                data["PayoutNumber"] = request.Metadata.PayoutNumber;
            if (!string.IsNullOrEmpty(request.Metadata.StatementPeriod))
                data["StatementPeriod"] = request.Metadata.StatementPeriod;
        }

        return data;
    }

    private string CalculateTimeAgo(DateTime createdAt)
    {
        var diff = DateTime.UtcNow - createdAt;

        return diff.TotalMinutes switch
        {
            < 1 => "Just now",
            < 60 => $"{(int)diff.TotalMinutes} minutes ago",
            < 1440 => $"{(int)diff.TotalHours} hours ago",
            < 43200 => $"{(int)diff.TotalDays} days ago",
            _ => createdAt.ToString("MMM d, yyyy")
        };
    }

    private string? BuildActionUrl(string? relatedEntityType, Guid? relatedEntityId)
    {
        if (string.IsNullOrEmpty(relatedEntityType) || !relatedEntityId.HasValue)
            return null;

        return relatedEntityType.ToLower() switch
        {
            "item" => $"/provider/items/{relatedEntityId}",
            "transaction" => $"/provider/sales/{relatedEntityId}",
            "payout" => $"/provider/payouts/{relatedEntityId}",
            "statement" => $"/provider/statements/{relatedEntityId}",
            _ => null
        };
    }
}