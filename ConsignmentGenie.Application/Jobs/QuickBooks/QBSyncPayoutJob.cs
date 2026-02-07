using ConsignmentGenie.Core.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Jobs.QuickBooks;

public class QBSyncPayoutJob
{
    private readonly IQBSyncService _qbSyncService;
    private readonly ILogger<QBSyncPayoutJob> _logger;

    public QBSyncPayoutJob(IQBSyncService qbSyncService, ILogger<QBSyncPayoutJob> logger)
    {
        _qbSyncService = qbSyncService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ExecuteCreated(Guid payoutId)
    {
        _logger.LogInformation("Starting QB payout created sync for payout {PayoutId}", payoutId);

        try
        {
            await _qbSyncService.SyncPayoutCreated(payoutId);
            _logger.LogInformation("Successfully completed QB payout created sync for payout {PayoutId}", payoutId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync payout created {PayoutId} to QuickBooks", payoutId);
            throw; // Re-throw to trigger Hangfire retry mechanism
        }
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ExecutePaid(Guid payoutId)
    {
        _logger.LogInformation("Starting QB payout paid sync for payout {PayoutId}", payoutId);

        try
        {
            await _qbSyncService.SyncPayoutPaid(payoutId);
            _logger.LogInformation("Successfully completed QB payout paid sync for payout {PayoutId}", payoutId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync payout paid {PayoutId} to QuickBooks", payoutId);
            throw; // Re-throw to trigger Hangfire retry mechanism
        }
    }
}