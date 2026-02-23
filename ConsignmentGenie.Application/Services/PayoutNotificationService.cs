using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;

namespace ConsignmentGenie.Application.Services;

public class PayoutNotificationService : IPayoutNotificationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly INotificationService _enhancedNotificationService;
    private readonly ILogger<PayoutNotificationService> _logger;

    public PayoutNotificationService(
        ConsignmentGenieContext context,
        INotificationService enhancedNotificationService,
        ILogger<PayoutNotificationService> logger)
    {
        _context = context;
        _enhancedNotificationService = enhancedNotificationService;
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
                    // 🚨 CRITICAL FIX: Use EnhancedNotificationService for proper email delivery and preference checking
                    await _enhancedNotificationService.CreateAsync(new CreateNotificationRequest
                    {
                        OrganizationId = summary.OrganizationId,
                        ToUserId = ownerUserId,
                        ToType = "owner",
                        Type = "payout_ready",  // Consistent naming convention
                        Title = "Consignor Ready for Payout",
                        Message = $"{summary.Consignor.Name} has ${summary.ClearedAmount:F2} ready for payout.",
                        ActionUrl = $"/owner/payouts?consignorId={summary.ConsignorId}",
                        // This now includes: preference checking, email sending, SMS prep, compliance validation
                    });
                }

                summary.NotificationSent = true;
                summary.NotificationSentAt = DateTime.UtcNow;
                summary.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("[PAYOUT_NOTIFICATION] Sent enhanced notification (email + system) for consignor {ConsignorName} in organization {OrganizationId}",
                    summary.Consignor.Name,
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