using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;

namespace ConsignmentGenie.Application.Services;

public class PayoutSummaryComputationService : IPayoutSummaryComputationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IClearDateCalculator _clearDateCalculator;
    private readonly ILogger<PayoutSummaryComputationService> _logger;

    public PayoutSummaryComputationService(
        ConsignmentGenieContext context,
        IClearDateCalculator clearDateCalculator,
        ILogger<PayoutSummaryComputationService> logger)
    {
        _context = context;
        _clearDateCalculator = clearDateCalculator;
        _logger = logger;
    }

    public async Task ComputeAllSummariesAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("=== STARTING PAYOUT SUMMARY COMPUTATION FOR ALL ORGANIZATIONS ===");
        _logger.LogInformation("Start Time: {StartTime}", startTime);

        try
        {
            _logger.LogInformation("Querying for active organizations...");
            var organizations = await _context.Organizations
                .Where(o => o.CachedSubscriptionStatus == "active" || o.CachedSubscriptionStatus == "trialing") // Only process active organizations
                .Select(o => new { o.Id, o.Name })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} active organizations to process", organizations.Count);

            if (organizations.Count == 0)
            {
                _logger.LogWarning("No active organizations found - computation completed with no work");
                return;
            }

            var successCount = 0;
            var errorCount = 0;

            foreach (var org in organizations)
            {
                _logger.LogInformation("Processing organization: {OrganizationName} ({OrganizationId})", org.Name, org.Id);

                try
                {
                    await ComputeOrganizationSummariesAsync(org.Id, cancellationToken);
                    successCount++;
                    _logger.LogInformation("Successfully processed organization: {OrganizationName}", org.Name);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "ERROR processing organization: {OrganizationName} ({OrganizationId})", org.Name, org.Id);
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            _logger.LogInformation("=== COMPLETED PAYOUT SUMMARY COMPUTATION ===");
            _logger.LogInformation("Total Organizations: {Total}, Successful: {Success}, Errors: {Errors}",
                organizations.Count, successCount, errorCount);
            _logger.LogInformation("Duration: {Duration:F2} seconds", duration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL ERROR in ComputeAllSummariesAsync");
            throw;
        }
    }

    public async Task ComputeOrganizationSummariesAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var orgStartTime = DateTime.UtcNow;
        _logger.LogInformation("--- Computing payout summaries for organization {OrganizationId} ---", organizationId);

        try
        {
            var today = DateTime.UtcNow.Date;
            _logger.LogInformation("Processing date: {Today}", today);

            // Get payout settings
            _logger.LogInformation("Checking for payout settings...");
            var payoutSettings = await _context.PayoutSettings
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

            // Calculate minimum threshold
            decimal lowestThreshold = 0;
            if (payoutSettings != null)
            {
                lowestThreshold = payoutSettings.MinimumPayoutThreshold;
                _logger.LogInformation("Using payout settings - Threshold: ${Threshold}", lowestThreshold);
            }
            else
            {
                _logger.LogWarning("NO payout settings found for organization {OrganizationId} - using threshold of $0", organizationId);
            }

            // Get unpaid transactions with items
            _logger.LogInformation("Querying unpaid transactions...");
            var unpaidTransactions = await _context.Transactions
                .Include(t => t.Items)
                .Where(t => t.OrganizationId == organizationId && t.PayoutId == null && t.Items.Any())
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {TransactionCount} unpaid transactions with items", unpaidTransactions.Count);

            if (unpaidTransactions.Count == 0)
            {
                _logger.LogInformation("No unpaid transactions found - will zero out all existing summaries");
            }

            // Group by consignor ID - handle multiple consignors per transaction by flattening
            _logger.LogInformation("Grouping transactions by consignor...");
            var transactionConsignorPairs = unpaidTransactions
                .SelectMany(t => t.Items.Select(item => new { Transaction = t, ConsignorId = item.ConsignorId }))
                .GroupBy(pair => pair.ConsignorId)
                .ToList();

            _logger.LogInformation("Found transactions for {ConsignorCount} unique consignors", transactionConsignorPairs.Count);

            // Get existing summaries
            _logger.LogInformation("Loading existing payout summaries...");
            var existingSummaries = await _context.PayoutSummaries
                .Where(s => s.OrganizationId == organizationId)
                .ToDictionaryAsync(s => s.ConsignorId, cancellationToken);

            _logger.LogInformation("Found {ExistingSummaryCount} existing payout summaries", existingSummaries.Count);

            var summariesUpdated = 0;
            var summariesCreated = 0;

            foreach (var group in transactionConsignorPairs)
            {
                var consignorId = group.Key;
                var transactions = group.Select(pair => pair.Transaction).Distinct().ToList();

                _logger.LogInformation("Processing consignor {ConsignorId} with {TransactionCount} transactions", consignorId, transactions.Count);

                // Calculate cleared status dynamically using HoldPeriodDays
                var holdPeriodDays = GetHoldPeriodDays(payoutSettings);
                _logger.LogInformation("  Using hold period: {HoldPeriodDays} days", holdPeriodDays);

                var cleared = new List<Transaction>();
                var uncleared = new List<Transaction>();

                foreach (var transaction in transactions)
                {
                    var calculatedClearDate = transaction.TransactionDate.Date.AddDays(holdPeriodDays);
                    var isCleared = calculatedClearDate <= today;

                    if (isCleared)
                    {
                        cleared.Add(transaction);
                    }
                    else
                    {
                        uncleared.Add(transaction);
                    }

                    _logger.LogDebug("    Transaction {TransactionDate:yyyy-MM-dd} -> ClearDate {ClearDate:yyyy-MM-dd}, IsCleared: {IsCleared}",
                        transaction.TransactionDate, calculatedClearDate, isCleared);
                }

                _logger.LogInformation("  Transactions breakdown: {ClearedCount} cleared, {UnclearedCount} uncleared", cleared.Count, uncleared.Count);

                var clearedAmount = cleared.Sum(t => GetConsignorAmount(t, consignorId));
                var unclearedAmount = uncleared.Sum(t => GetConsignorAmount(t, consignorId));

                _logger.LogInformation("  Calculated amounts: Cleared=${ClearedAmount}, Uncleared=${UnclearedAmount}", clearedAmount, unclearedAmount);

                var isNewSummary = false;
                if (!existingSummaries.TryGetValue(consignorId, out var summary))
                {
                    summary = new PayoutSummary { OrganizationId = organizationId, ConsignorId = consignorId };
                    _context.PayoutSummaries.Add(summary);
                    existingSummaries[consignorId] = summary;
                    isNewSummary = true;
                    summariesCreated++;
                    _logger.LogInformation("  Created NEW payout summary for consignor {ConsignorId}", consignorId);
                }
                else
                {
                    summariesUpdated++;
                    _logger.LogInformation("  Updating EXISTING payout summary for consignor {ConsignorId} (Previous: Cleared=${PreviousCleared}, Uncleared=${PreviousUncleared})",
                        consignorId, summary.ClearedAmount, summary.UnclearedAmount);
                }

                var wasReady = summary.MeetsMinimumThreshold && summary.ClearedAmount > 0;

                summary.ClearedAmount = clearedAmount;
                summary.UnclearedAmount = unclearedAmount;
                summary.TotalPendingAmount = clearedAmount + unclearedAmount;
                summary.ClearedTransactionCount = cleared.Count;
                summary.UnclearedTransactionCount = uncleared.Count;
                summary.TotalTransactionCount = transactions.Count;
                summary.EarliestSaleDate = transactions.Any() ? transactions.Min(t => t.TransactionDate) : null;
                summary.LatestSaleDate = transactions.Any() ? transactions.Max(t => t.TransactionDate) : null;
                summary.NextClearDate = uncleared.Any()
                    ? uncleared.Min(t => t.TransactionDate.Date.AddDays(holdPeriodDays))
                    : (DateTime?)null;
                summary.MeetsMinimumThreshold = clearedAmount >= lowestThreshold;
                summary.LowestMinimumThreshold = lowestThreshold;
                summary.LastComputedAt = DateTime.UtcNow;
                summary.UpdatedAt = DateTime.UtcNow;

                // Reset notification if no longer ready
                var isNowReady = summary.MeetsMinimumThreshold && clearedAmount > 0;
                if (!isNowReady)
                {
                    summary.NotificationSent = false;
                }

                var readyStatusChange = wasReady != isNowReady ? (isNowReady ? " (NOW READY for payout)" : " (NO LONGER ready)") : "";
                _logger.LogInformation("  Final: Cleared=${ClearedAmount}, MeetsThreshold={MeetsThreshold}, TotalTransactions={TotalTransactions}{ReadyStatusChange}",
                    clearedAmount, summary.MeetsMinimumThreshold, transactions.Count, readyStatusChange);
            }

            // Zero out summaries for consignors with no pending transactions
            _logger.LogInformation("Checking for summaries to zero out...");
            var consignorsWithTransactions = transactionConsignorPairs.Select(g => g.Key).ToHashSet();
            var summariesZeroed = 0;

            foreach (var summary in existingSummaries.Values.Where(s => !consignorsWithTransactions.Contains(s.ConsignorId)))
            {
                _logger.LogInformation("Zeroing out summary for consignor {ConsignorId} (no pending transactions)", summary.ConsignorId);
                summary.ClearedAmount = 0;
                summary.UnclearedAmount = 0;
                summary.TotalPendingAmount = 0;
                summary.ClearedTransactionCount = 0;
                summary.UnclearedTransactionCount = 0;
                summary.TotalTransactionCount = 0;
                summary.MeetsMinimumThreshold = false;
                summary.NotificationSent = false;
                summary.LastComputedAt = DateTime.UtcNow;
                summary.UpdatedAt = DateTime.UtcNow;
                summariesZeroed++;
            }

            _logger.LogInformation("Saving changes to database... (Created: {Created}, Updated: {Updated}, Zeroed: {Zeroed})",
                summariesCreated, summariesUpdated, summariesZeroed);

            var saveStartTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            var saveDuration = DateTime.UtcNow - saveStartTime;

            var orgEndTime = DateTime.UtcNow;
            var orgDuration = orgEndTime - orgStartTime;

            _logger.LogInformation("--- COMPLETED organization {OrganizationId} in {Duration:F2}s (Save took {SaveDuration:F0}ms) ---",
                organizationId, orgDuration.TotalSeconds, saveDuration.TotalMilliseconds);
            _logger.LogInformation("Summary: {ProcessedCount} consignors processed, {CreatedCount} created, {UpdatedCount} updated, {ZeroedCount} zeroed",
                transactionConsignorPairs.Count, summariesCreated, summariesUpdated, summariesZeroed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR in ComputeOrganizationSummariesAsync for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task ComputeConsignorSummaryAsync(Guid organizationId, Guid consignorId, CancellationToken cancellationToken = default)
    {
        // Single consignor - useful after payout
        var today = DateTime.UtcNow.Date;

        // Get payout settings
        var payoutSettings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, cancellationToken);

        decimal lowestThreshold = 0;
        if (payoutSettings != null)
        {
            lowestThreshold = payoutSettings.MinimumPayoutThreshold;
        }
        else
        {
            // No payout settings found - use default threshold of 0
            lowestThreshold = 0;
        }

        var transactions = await _context.Transactions
            .Include(t => t.Items)
            .Where(t => t.OrganizationId == organizationId && t.Items.Any(ti => ti.ConsignorId == consignorId) && t.PayoutId == null)
            .ToListAsync(cancellationToken);

        var summary = await _context.PayoutSummaries
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.ConsignorId == consignorId, cancellationToken);

        if (summary == null)
        {
            summary = new PayoutSummary { OrganizationId = organizationId, ConsignorId = consignorId };
            _context.PayoutSummaries.Add(summary);
        }

        // Calculate cleared status dynamically using HoldPeriodDays
        var holdPeriodDays = GetHoldPeriodDays(payoutSettings);

        var cleared = new List<Transaction>();
        var uncleared = new List<Transaction>();

        foreach (var transaction in transactions)
        {
            var calculatedClearDate = transaction.TransactionDate.Date.AddDays(holdPeriodDays);
            var isCleared = calculatedClearDate <= today;

            if (isCleared)
            {
                cleared.Add(transaction);
            }
            else
            {
                uncleared.Add(transaction);
            }
        }

        summary.ClearedAmount = cleared.Sum(t => GetConsignorAmount(t, consignorId));
        summary.UnclearedAmount = uncleared.Sum(t => GetConsignorAmount(t, consignorId));
        summary.TotalPendingAmount = summary.ClearedAmount + summary.UnclearedAmount;
        summary.ClearedTransactionCount = cleared.Count;
        summary.UnclearedTransactionCount = uncleared.Count;
        summary.TotalTransactionCount = transactions.Count;
        summary.EarliestSaleDate = transactions.Any() ? transactions.Min(t => t.TransactionDate) : null;
        summary.LatestSaleDate = transactions.Any() ? transactions.Max(t => t.TransactionDate) : null;
        summary.NextClearDate = uncleared.Any()
            ? uncleared.Min(t => t.TransactionDate.Date.AddDays(holdPeriodDays))
            : (DateTime?)null;
        summary.MeetsMinimumThreshold = summary.ClearedAmount >= lowestThreshold;
        summary.LowestMinimumThreshold = lowestThreshold;
        summary.LastComputedAt = DateTime.UtcNow;
        summary.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private decimal GetConsignorAmount(Transaction t)
    {
        return t.Items?.Sum(ti => ti.ConsignorAmount) ?? 0;
    }

    private decimal GetConsignorAmount(Transaction t, Guid consignorId)
    {
        return t.Items?.Where(ti => ti.ConsignorId == consignorId).Sum(ti => ti.ConsignorAmount) ?? 0;
    }

    private int GetHoldPeriodDays(PayoutSettings? payoutSettings)
    {
        if (payoutSettings != null)
        {
            return payoutSettings.HoldPeriodDays;
        }

        // Default hold period if no settings found
        return 7;
    }
}