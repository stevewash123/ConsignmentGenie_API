using ConsignmentGenie.Core.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Jobs.QuickBooks;

public class QBSyncVendorJob
{
    private readonly IQBSyncService _qbSyncService;
    private readonly ILogger<QBSyncVendorJob> _logger;

    public QBSyncVendorJob(IQBSyncService qbSyncService, ILogger<QBSyncVendorJob> logger)
    {
        _qbSyncService = qbSyncService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task Execute(Guid consignorId)
    {
        _logger.LogInformation("Starting QB vendor sync for consignor {ConsignorId}", consignorId);

        try
        {
            await _qbSyncService.SyncVendor(consignorId);
            _logger.LogInformation("Successfully completed QB vendor sync for consignor {ConsignorId}", consignorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync vendor {ConsignorId} to QuickBooks", consignorId);
            throw; // Re-throw to trigger Hangfire retry mechanism
        }
    }
}