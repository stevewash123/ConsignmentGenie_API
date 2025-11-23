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

public class ProviderNotificationService : IProviderNotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly INotificationService _emailNotificationService; // Existing service for emails
    private readonly ILogger<ProviderNotificationService> _logger;

    public ProviderNotificationService(
        ConsignmentGenieContext context,
        INotificationService emailNotificationService,
        ILogger<ProviderNotificationService> logger)
    {
        _context = context;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(CreateNotificationRequest request)
    {
        try
        {
            // Create in-app notification
            var notification = new Notification
            {
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
                ProviderId = request.ProviderId,
                Type = request.Type.ToString(),
                Title = request.Title,
                Message = request.Message,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                ExpiresAt = request.ExpiresAt,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                IsRead = false,
                EmailSent = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Check user preferences and send email if enabled
            var userPreference = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.NotificationType == request.Type);

            if (userPreference?.EmailEnabled ?? true) // Default to email enabled
            {
                try
                {
                    // Build data dictionary for email template
                    var emailData = BuildEmailData(request);

                    // Send via existing email notification service
                    var emailRequest = new NotificationRequest
                    {
                        Type = request.Type,
                        UserId = request.UserId,
                        Data = emailData
                    };

                    var emailSent = await _emailNotificationService.SendAsync(emailRequest);

                    if (emailSent)
                    {
                        notification.EmailSent = true;
                        notification.EmailSentAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification for {Type} to user {UserId}", request.Type, request.UserId);
                }
            }

            _logger.LogInformation("Created notification {Type} for user {UserId}", request.Type, request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification {Type} for user {UserId}", request.Type, request.UserId);
            throw;
        }
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, NotificationQueryParams queryParams)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (queryParams.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrEmpty(queryParams.Type))
        {
            query = query.Where(n => n.Type == queryParams.Type);
        }

        // Filter out expired notifications
        query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);

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
            RelatedEntityType = n.RelatedEntityType,
            RelatedEntityId = n.RelatedEntityId,
            TimeAgo = CalculateTimeAgo(n.CreatedAt),
            ActionUrl = BuildActionUrl(n.RelatedEntityType, n.RelatedEntityId),
            Metadata = n.Metadata != null ? JsonSerializer.Deserialize<NotificationMetadata>(n.Metadata) : null
        }).ToList();

        return new PagedResult<NotificationDto>(notifications, totalCount, queryParams.Page, queryParams.PageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
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
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

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
                DigestMode = "instant",
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
            DigestMode = preferences.DigestMode,
            DigestTime = preferences.DigestTime.ToString(@"hh\:mm"),
            DigestDay = preferences.DigestDay,
            PayoutPendingThreshold = preferences.PayoutPendingThreshold
        };
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferencesRequest request)
    {
        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
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
        preferences.DigestTime = TimeSpan.Parse(request.DigestTime);
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