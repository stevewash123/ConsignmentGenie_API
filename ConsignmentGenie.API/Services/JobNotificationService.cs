using ConsignmentGenie.API.Hubs;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.API.Services;

public class JobNotificationService : IJobNotificationService
{
    private readonly IHubContext<JobNotificationHub> _hubContext;
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<JobNotificationService> _logger;

    public JobNotificationService(
        IHubContext<JobNotificationHub> hubContext,
        ConsignmentGenieContext context,
        ILogger<JobNotificationService> logger)
    {
        _hubContext = hubContext;
        _context = context;
        _logger = logger;
    }

    public async Task NotifySuccess(Guid organizationId, string title, string message, string? actionUrl = null)
    {
        _logger.LogInformation("Sending success notification to organization {OrganizationId}: {Title}",
            organizationId, title);

        await SendNotification(organizationId, new
        {
            Type = "success",
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyWarning(Guid organizationId, string title, string message, string? actionUrl = null)
    {
        _logger.LogWarning("Sending warning notification to organization {OrganizationId}: {Title}",
            organizationId, title);

        await SendNotification(organizationId, new
        {
            Type = "warning",
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyError(Guid organizationId, string title, string message, string? actionUrl = null)
    {
        _logger.LogError("Sending error notification to organization {OrganizationId}: {Title}",
            organizationId, title);

        await SendNotification(organizationId, new
        {
            Type = "error",
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyInfo(Guid organizationId, string title, string message, string? actionUrl = null)
    {
        _logger.LogInformation("Sending info notification to organization {OrganizationId}: {Title}",
            organizationId, title);

        await SendNotification(organizationId, new
        {
            Type = "info",
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyJobStarted(Guid organizationId, string jobName, string? description = null)
    {
        _logger.LogInformation("Job started notification for organization {OrganizationId}: {JobName}",
            organizationId, jobName);

        await SendNotification(organizationId, new
        {
            Type = "job_started",
            JobName = jobName,
            Description = description ?? $"{jobName} has started",
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyJobCompleted(Guid organizationId, string jobName, string? summary = null)
    {
        _logger.LogInformation("Job completed notification for organization {OrganizationId}: {JobName}",
            organizationId, jobName);

        await SendNotification(organizationId, new
        {
            Type = "job_completed",
            JobName = jobName,
            Summary = summary ?? $"{jobName} completed successfully",
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyJobFailed(Guid organizationId, string jobName, string errorMessage, string? actionUrl = null)
    {
        _logger.LogError("Job failed notification for organization {OrganizationId}: {JobName} - {Error}",
            organizationId, jobName, errorMessage);

        await SendNotification(organizationId, new
        {
            Type = "job_failed",
            JobName = jobName,
            ErrorMessage = errorMessage,
            ActionUrl = actionUrl,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyQBSyncProgress(Guid organizationId, string entityType, int processed, int total)
    {
        await SendNotification(organizationId, new
        {
            Type = "qb_sync_progress",
            EntityType = entityType,
            Processed = processed,
            Total = total,
            Percentage = total > 0 ? Math.Round((decimal)processed / total * 100, 1) : 0,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyPayoutProgress(Guid organizationId, string status, int processed, int total, decimal totalAmount)
    {
        await SendNotification(organizationId, new
        {
            Type = "payout_progress",
            Status = status,
            Processed = processed,
            Total = total,
            TotalAmount = totalAmount,
            Percentage = total > 0 ? Math.Round((decimal)processed / total * 100, 1) : 0,
            Timestamp = DateTime.UtcNow
        });
    }

    private async Task SendNotification(Guid organizationId, object notification)
    {
        try
        {
            // Send to all users in the organization group
            var groupName = $"org_{organizationId}";
            await _hubContext.Clients.Group(groupName).SendAsync("JobNotification", notification);

            // Optionally persist notifications to database for historical view
            // This could be implemented later if needed
            _logger.LogDebug("Sent notification to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to organization {OrganizationId}", organizationId);
            // Don't rethrow - notifications are nice-to-have, not critical
        }
    }
}