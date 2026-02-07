using ConsignmentGenie.Core.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Jobs.QuickBooks;

public class QBSyncSaleJob
{
    private readonly IQBSyncService _qbSyncService;
    private readonly ILogger<QBSyncSaleJob> _logger;

    public QBSyncSaleJob(IQBSyncService qbSyncService, ILogger<QBSyncSaleJob> logger)
    {
        _qbSyncService = qbSyncService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task Execute(Guid transactionId)
    {
        _logger.LogInformation("Starting QB sale sync for transaction {TransactionId}", transactionId);

        try
        {
            await _qbSyncService.SyncSale(transactionId);
            _logger.LogInformation("Successfully completed QB sale sync for transaction {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync sale {TransactionId} to QuickBooks", transactionId);
            throw; // Re-throw to trigger Hangfire retry mechanism
        }
    }
}