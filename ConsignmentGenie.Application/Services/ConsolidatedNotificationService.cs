using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Manages consolidated notifications with Hangfire debounce logic
/// Prevents notification spam by batching related notifications
/// </summary>
public class ConsolidatedNotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ConsolidatedNotificationService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private const int DEBOUNCE_MINUTES = 5;

    public ConsolidatedNotificationService(
        ConsignmentGenieContext context,
        ILogger<ConsolidatedNotificationService> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Schedules a consolidated notification for item activation
    /// Uses debounce logic to batch multiple items within 5 minutes
    /// </summary>
    public async Task ScheduleItemActivatedNotificationAsync(Guid consignorId, Guid organizationId, Guid itemId)
    {
        var notificationType = NotificationType.ItemActivated.ToString();

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Check for existing pending notification for this consignor/org/type
            var existingNotification = await _context.PendingConsolidatedNotifications
                .FirstOrDefaultAsync(pcn =>
                    pcn.ConsignorId == consignorId &&
                    pcn.OrganizationId == organizationId &&
                    pcn.NotificationType == notificationType &&
                    pcn.SentAt == null &&
                    pcn.ProcessingStartedAt == null);

            if (existingNotification != null)
            {
                // Cancel existing Hangfire job
                _backgroundJobClient.Delete(existingNotification.HangfireJobId);

                // Add new item to the list
                var existingItemIds = JsonSerializer.Deserialize<List<Guid>>(existingNotification.ItemIds) ?? new List<Guid>();
                if (!existingItemIds.Contains(itemId))
                {
                    existingItemIds.Add(itemId);
                    existingNotification.ItemIds = JsonSerializer.Serialize(existingItemIds);
                }

                // Reschedule for 5 minutes from now
                existingNotification.ScheduledFor = DateTime.UtcNow.AddMinutes(DEBOUNCE_MINUTES);
                var newJobId = _backgroundJobClient.Schedule<ConsolidatedNotificationJob>(
                    job => job.ProcessConsolidatedNotificationAsync(existingNotification.Id),
                    TimeSpan.FromMinutes(DEBOUNCE_MINUTES));

                existingNotification.HangfireJobId = newJobId;

                _logger.LogInformation("Rescheduled consolidated notification {NotificationId} for consignor {ConsignorId}, now includes {ItemCount} items",
                    existingNotification.Id, consignorId, existingItemIds.Count);
            }
            else
            {
                // Create new pending notification
                var scheduledFor = DateTime.UtcNow.AddMinutes(DEBOUNCE_MINUTES);
                var itemIds = JsonSerializer.Serialize(new List<Guid> { itemId });

                var pendingNotification = new PendingConsolidatedNotification
                {
                    Id = Guid.NewGuid(),
                    ConsignorId = consignorId,
                    OrganizationId = organizationId,
                    NotificationType = notificationType,
                    ItemIds = itemIds,
                    ScheduledFor = scheduledFor,
                    HangfireJobId = "", // Set after job creation
                    CreatedAt = DateTime.UtcNow
                };

                _context.PendingConsolidatedNotifications.Add(pendingNotification);
                await _context.SaveChangesAsync();

                // Schedule Hangfire job
                var jobId = _backgroundJobClient.Schedule<ConsolidatedNotificationJob>(
                    job => job.ProcessConsolidatedNotificationAsync(pendingNotification.Id),
                    TimeSpan.FromMinutes(DEBOUNCE_MINUTES));

                // Update with job ID
                pendingNotification.HangfireJobId = jobId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Scheduled new consolidated notification {NotificationId} for consignor {ConsignorId} at {ScheduledTime}",
                    pendingNotification.Id, consignorId, scheduledFor);
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule consolidated notification for consignor {ConsignorId}, item {ItemId}",
                consignorId, itemId);
            throw;
        }
    }

    /// <summary>
    /// Cleans up completed and failed notifications older than 7 days
    /// Should be called periodically to prevent table growth
    /// </summary>
    public async Task CleanupOldNotificationsAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7);
            var oldNotifications = await _context.PendingConsolidatedNotifications
                .Where(pcn =>
                    (pcn.SentAt.HasValue || pcn.FailedAt.HasValue) &&
                    (pcn.SentAt < cutoffDate || pcn.FailedAt < cutoffDate))
                .ToListAsync();

            if (oldNotifications.Any())
            {
                _context.PendingConsolidatedNotifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old consolidated notifications", oldNotifications.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old consolidated notifications");
        }
    }
}