using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Hangfire background job for processing consolidated notifications
/// Implements race condition protection and grace period for late imports
/// </summary>
public class ConsolidatedNotificationJob
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConsignorNotificationService _notificationService;
    private readonly ILogger<ConsolidatedNotificationJob> _logger;
    private const int GRACE_PERIOD_SECONDS = 5;

    public ConsolidatedNotificationJob(
        ConsignmentGenieContext context,
        IConsignorNotificationService notificationService,
        ILogger<ConsolidatedNotificationJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a consolidated notification with race condition protection
    /// Implements 5-second grace period for items being imported right as job fires
    /// </summary>
    public async Task ProcessConsolidatedNotificationAsync(Guid pendingNotificationId)
    {
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Step 1: Claim the notification atomically
            var notification = await _context.PendingConsolidatedNotifications
                .Include(n => n.Consignor)
                .Include(n => n.Organization)
                .FirstOrDefaultAsync(n =>
                    n.Id == pendingNotificationId &&
                    n.SentAt == null &&
                    n.ProcessingStartedAt == null);

            if (notification == null)
            {
                _logger.LogWarning("Consolidated notification {NotificationId} not found or already processed", pendingNotificationId);
                return;
            }

            // Claim the notification to prevent race conditions
            notification.ProcessingStartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Starting processing of consolidated notification {NotificationId} for consignor {ConsignorId}",
                pendingNotificationId, notification.ConsignorId);

            // Step 2: Grace period delay for potential late imports
            await Task.Delay(TimeSpan.FromSeconds(GRACE_PERIOD_SECONDS));

            // Step 3: Check for any additional pending notifications that arrived during grace period
            await using var finalTransaction = await _context.Database.BeginTransactionAsync();

            var additionalNotifications = await _context.PendingConsolidatedNotifications
                .Where(n =>
                    n.ConsignorId == notification.ConsignorId &&
                    n.OrganizationId == notification.OrganizationId &&
                    n.NotificationType == notification.NotificationType &&
                    n.Id != notification.Id &&
                    n.SentAt == null &&
                    n.ProcessingStartedAt == null)
                .ToListAsync();

            // Merge any additional notifications
            var allItemIds = JsonSerializer.Deserialize<List<Guid>>(notification.ItemIds) ?? new List<Guid>();

            foreach (var additional in additionalNotifications)
            {
                var additionalItemIds = JsonSerializer.Deserialize<List<Guid>>(additional.ItemIds) ?? new List<Guid>();
                foreach (var itemId in additionalItemIds)
                {
                    if (!allItemIds.Contains(itemId))
                    {
                        allItemIds.Add(itemId);
                    }
                }

                // Mark additional notifications as processed
                additional.SentAt = DateTime.UtcNow;
                _logger.LogInformation("Merged additional notification {AdditionalId} into {MainId}",
                    additional.Id, notification.Id);
            }

            // Step 4: Send the consolidated notification
            await SendConsolidatedNotificationAsync(notification, allItemIds);

            // Step 5: Mark as sent
            notification.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await finalTransaction.CommitAsync();

            _logger.LogInformation("Successfully sent consolidated notification {NotificationId} for {ItemCount} items to consignor {ConsignorId}",
                pendingNotificationId, allItemIds.Count, notification.ConsignorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process consolidated notification {NotificationId}", pendingNotificationId);

            // Mark as failed
            try
            {
                var failedNotification = await _context.PendingConsolidatedNotifications
                    .FirstOrDefaultAsync(n => n.Id == pendingNotificationId);

                if (failedNotification != null)
                {
                    failedNotification.FailedAt = DateTime.UtcNow;
                    failedNotification.FailureReason = ex.Message.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception markFailedException)
            {
                _logger.LogError(markFailedException, "Failed to mark notification {NotificationId} as failed", pendingNotificationId);
            }
        }
    }

    /// <summary>
    /// Sends the actual consolidated notification email/SMS
    /// </summary>
    private async Task SendConsolidatedNotificationAsync(PendingConsolidatedNotification notification, List<Guid> itemIds)
    {
        // Get item details for the notification
        var items = await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Title, i.Price })
            .ToListAsync();

        var notificationType = Enum.Parse<NotificationType>(notification.NotificationType);

        // Create notification content
        var subject = itemIds.Count == 1
            ? "Your item has been activated"
            : $"{itemIds.Count} of your items have been activated";

        var message = itemIds.Count == 1
            ? $"Great news! Your item \"{items.First().Title}\" has been activated and is now available for sale."
            : $"Great news! {itemIds.Count} of your items have been activated and are now available for sale:\n\n" +
              string.Join("\n", items.Select(i => $"• {i.Title} - ${i.Price:F2}"));

        // Get the consignor's UserId for the notification (notifications must reference Users table)
        var consignorUserId = await _context.Consignors
            .Where(c => c.Id == notification.ConsignorId)
            .Select(c => c.UserId)
            .FirstOrDefaultAsync();

        if (!consignorUserId.HasValue)
        {
            _logger.LogWarning("Cannot send notification to consignor {ConsignorId} - no associated UserId found", notification.ConsignorId);
            return;
        }

        // Send via existing notification service
        var createRequest = new CreateNotificationRequest
        {
            OrganizationId = notification.OrganizationId,
            FromType = "system",
            ToUserId = consignorUserId.Value,
            ToType = "consignor",
            Type = notificationType.ToString(),
            Title = subject,
            Message = message,
            ActionUrl = "", // Will be set after we get the notification ID
            Payload = new
            {
                ItemIds = itemIds,
                ItemCount = itemIds.Count,
                NotificationDate = DateTime.UtcNow
            },
            ReferenceType = "consolidated_notification",
            ReferenceId = notification.Id
        };

        await _notificationService.CreateNotificationAsync(createRequest);

        // Find the created notification by ReferenceId to update the ActionUrl
        var actualNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.ReferenceId == notification.Id && n.ReferenceType == "consolidated_notification");

        if (actualNotification != null)
        {
            actualNotification.ActionUrl = $"/consignor/notifications/activated-items/{actualNotification.Id}";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated notification {NotificationId} ActionUrl to use correct notification ID",
                actualNotification.Id);
        }
    }
}