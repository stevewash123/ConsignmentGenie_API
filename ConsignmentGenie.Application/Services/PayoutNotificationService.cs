using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;

namespace ConsignmentGenie.Application.Services;

public class PayoutNotificationService : IPayoutNotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<PayoutNotificationService> _logger;

    public PayoutNotificationService(ConsignmentGenieContext context, ILogger<PayoutNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendReadyForPayoutNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var readySummaries = await _context.PayoutSummaries
            .Include(s => s.Consignor)
            .Include(s => s.Organization)
            .Where(s => s.MeetsMinimumThreshold && s.ClearedAmount > 0 && !s.NotificationSent)
            .ToListAsync(cancellationToken);

        foreach (var summary in readySummaries)
        {
            try
            {
                // Get owners for this organization
                var ownerUserIds = await _context.UserRoleAssignments
                    .Where(ura => ura.OrganizationId == summary.OrganizationId &&
                                  ura.Role == UserRole.Owner &&
                                  ura.IsActive)
                    .Select(ura => ura.UserId)
                    .ToListAsync(cancellationToken);

                foreach (var ownerUserId in ownerUserIds)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = summary.OrganizationId,
                        ToUserId = ownerUserId,
                        ToType = "owner",
                        Type = "PayoutReady",
                        Title = "Consignor Ready for Payout",
                        Message = $"{summary.Consignor.Name} {summary.Consignor} has ${summary.ClearedAmount:F2} ready for payout.",
                        ActionUrl = $"/owner/payouts?consignorId={summary.ConsignorId}",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);
                }

                summary.NotificationSent = true;
                summary.NotificationSentAt = DateTime.UtcNow;
                summary.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Created payout notification for consignor {ConsignorName} in organization {OrganizationId}",
                    $"{summary.Consignor.Name} {summary.Consignor}",
                    summary.OrganizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create notification for consignor {ConsignorId} in organization {OrganizationId}",
                    summary.ConsignorId,
                    summary.OrganizationId);
                // Continue with other notifications even if one fails
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sent payout notifications for {Count} consignors", readySummaries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save payout notifications to database");
            throw;
        }
    }
}