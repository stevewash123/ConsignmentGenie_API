using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Jobs;

public class AutoPayoutJob
{
    private readonly ConsignmentGenieContext _context;
    private readonly IPlaidService _plaidService;
    private readonly IACHService _achService;
    private readonly IJobNotificationService _notifications;
    private readonly ILogger<AutoPayoutJob> _logger;

    public AutoPayoutJob(
        ConsignmentGenieContext context,
        IPlaidService plaidService,
        IACHService achService,
        IJobNotificationService notifications,
        ILogger<AutoPayoutJob> logger)
    {
        _context = context;
        _plaidService = plaidService;
        _achService = achService;
        _notifications = notifications;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task Execute(Guid organizationId)
    {
        _logger.LogInformation("Starting auto-payout job for organization {OrganizationId}", organizationId);

        try
        {
            var settings = await GetPayoutSettings(organizationId);
            if (!ShouldRunAutoPayout(settings))
            {
                _logger.LogInformation("Auto-payout not enabled for organization {OrganizationId}", organizationId);
                return;
            }

            // Check bank balance
            var currentBalance = await _plaidService.GetAccountBalance(
                settings.PlaidAccessToken,
                settings.PlaidAccountId);

            // Get eligible payouts
            var eligiblePayouts = await GetEligiblePayouts(organizationId, settings);

            if (!eligiblePayouts.Any())
            {
                _logger.LogInformation("No eligible payouts found for organization {OrganizationId}", organizationId);
                return;
            }

            var totalPayoutAmount = eligiblePayouts.Sum(p => p.Amount);

            // Check minimum balance protection
            if (currentBalance - totalPayoutAmount < settings.MinimumBalanceProtection)
            {
                await HandleInsufficientBalance(organizationId, totalPayoutAmount, currentBalance, settings.MinimumBalanceProtection);
                return;
            }

            // Generate batch ID for this auto-payout run
            var batchId = $"AUTO-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow.Ticks % 1000:D3}";
            var batchCreatedAt = DateTime.UtcNow;

            _logger.LogInformation("Starting auto-payout batch {BatchId} with {PayoutCount} payouts",
                batchId, eligiblePayouts.Count);

            // Process payouts
            var successfulPayouts = new List<EligiblePayout>();
            var failedPayouts = new List<(EligiblePayout payout, string error)>();

            foreach (var eligiblePayout in eligiblePayouts)
            {
                try
                {
                    var achRequest = new ACHTransferRequest
                    {
                        Amount = eligiblePayout.Amount,
                        FromAccountToken = settings.PlaidAccessToken,
                        ToAccountId = eligiblePayout.ConsignorAccountId,
                        Description = $"Consignment payout {eligiblePayout.PayoutNumber}",
                        Reference = eligiblePayout.PayoutNumber,
                        PayoutId = eligiblePayout.PayoutId
                    };

                    var result = await _achService.InitiateTransfer(achRequest);

                    if (result.Success)
                    {
                        await MarkPayoutPaid(eligiblePayout.PayoutId, result.TransferId!, batchId, batchCreatedAt);
                        successfulPayouts.Add(eligiblePayout);
                    }
                    else
                    {
                        failedPayouts.Add((eligiblePayout, result.ErrorMessage!));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process auto-payout {PayoutId}", eligiblePayout.PayoutId);
                    failedPayouts.Add((eligiblePayout, ex.Message));
                }
            }

            // Send notifications
            if (successfulPayouts.Any())
            {
                var totalAmount = successfulPayouts.Sum(p => p.Amount);
                await _notifications.NotifySuccess(
                    organizationId,
                    "Auto-Payouts Complete",
                    $"Batch {batchId}: Paid {successfulPayouts.Count} consignors, ${totalAmount:N2}",
                    "/owner/payouts"
                );
            }

            if (failedPayouts.Any())
            {
                await _notifications.NotifyWarning(
                    organizationId,
                    "Some Auto-Payouts Failed",
                    $"{failedPayouts.Count} payouts failed. Check payout history for details.",
                    "/owner/payouts"
                );
            }

            _logger.LogInformation("Auto-payout job completed for organization {OrganizationId}. Successful: {SuccessCount}, Failed: {FailCount}",
                organizationId, successfulPayouts.Count, failedPayouts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-payout job failed for organization {OrganizationId}", organizationId);

            await _notifications.NotifyError(
                organizationId,
                "Auto-Payout Failed",
                ex.Message,
                "/owner/settings/payouts/direct-deposit"
            );

            throw;
        }
    }

    private async Task<PayoutSettings> GetPayoutSettings(Guid organizationId)
    {
        var settings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            throw new InvalidOperationException($"No payout settings found for organization {organizationId}");
        }

        return settings;
    }

    private bool ShouldRunAutoPayout(PayoutSettings settings)
    {
        if (!settings.AutoPayEnabled || !settings.BankAccountConnected)
            return false;

        var today = DateTime.Today.DayOfWeek;

        return today switch
        {
            DayOfWeek.Monday => settings.AutoPayMonday,
            DayOfWeek.Tuesday => settings.AutoPayTuesday,
            DayOfWeek.Wednesday => settings.AutoPayWednesday,
            DayOfWeek.Thursday => settings.AutoPayThursday,
            DayOfWeek.Friday => settings.AutoPayFriday,
            DayOfWeek.Saturday => settings.AutoPaySaturday,
            DayOfWeek.Sunday => settings.AutoPaySunday,
            _ => false
        };
    }

    private async Task<List<EligiblePayout>> GetEligiblePayouts(Guid organizationId, PayoutSettings settings)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-settings.HoldPeriodDays);

        // Get transactions that are ready for payout
        var eligibleTransactions = await _context.Transactions
            .Where(t => t.OrganizationId == organizationId &&
                       t.TransactionDate <= cutoffDate &&
                       t.PayoutId == null) // Not already included in a payout
            .Include(t => t.Items)
            .ToListAsync();

        // Group by consignor and calculate amounts
        var payoutsByConsignor = eligibleTransactions
            .SelectMany(t => t.Items!)
            .Where(ti => ti.ConsignorId != Guid.Empty)
            .GroupBy(ti => ti.ConsignorId)
            .Select(g => new
            {
                ConsignorId = g.Key,
                TotalAmount = g.Sum(ti => ti.UnitPrice * ti.Quantity * ti.ConsignorSplitPercentage)
            })
            .Where(p => p.TotalAmount >= settings.MinimumPayoutThreshold)
            .ToList();

        // Get consignor details for those with bank accounts
        var eligibleConsignors = await _context.Consignors
            .Where(c => payoutsByConsignor.Select(p => p.ConsignorId).Contains(c.Id) &&
                       !string.IsNullOrEmpty(c.PaymentDetails)) // Assuming bank account info stored here
            .ToListAsync();

        // Build eligible payout list
        var eligiblePayouts = new List<EligiblePayout>();

        foreach (var consignor in eligibleConsignors)
        {
            var payoutAmount = payoutsByConsignor.First(p => p.ConsignorId == consignor.Id).TotalAmount;

            eligiblePayouts.Add(new EligiblePayout
            {
                PayoutId = Guid.NewGuid(),
                ConsignorId = consignor.Id,
                ConsignorName = $"{consignor.FirstName} {consignor.LastName}",
                ConsignorAccountId = ExtractAccountId(consignor.PaymentDetails!),
                Amount = payoutAmount,
                PayoutNumber = GeneratePayoutNumber()
            });
        }

        return eligiblePayouts;
    }

    private async Task HandleInsufficientBalance(Guid organizationId, decimal totalPayout, decimal currentBalance, decimal minBalance)
    {
        _logger.LogWarning("Insufficient balance for auto-payout. Organization: {OrganizationId}, Required: {Required}, Available: {Available}, MinBalance: {MinBalance}",
            organizationId, totalPayout, currentBalance, minBalance);

        await _notifications.NotifyWarning(
            organizationId,
            "Auto-Payout Skipped",
            $"Today's payout of ${totalPayout:N2} would have dropped your account below your ${minBalance:N2} protection threshold.",
            "/owner/payouts"
        );
    }

    private async Task MarkPayoutPaid(Guid payoutId, string transferId, string batchId, DateTime batchCreatedAt)
    {
        // Create payout record
        var payout = new Payout
        {
            Id = payoutId,
            PaymentReference = transferId,
            Status = Core.Enums.PayoutStatus.Paid,
            PaidAt = DateTime.UtcNow,
            PaymentMethod = "ACH",
            BatchId = batchId,
            BatchCreatedAt = batchCreatedAt
        };

        // Note: This is simplified - in reality you'd need to create the full payout
        // with proper transaction associations, amounts, etc.

        _context.Payouts.Add(payout);
        await _context.SaveChangesAsync();
    }

    private string ExtractAccountId(string paymentDetails)
    {
        // This would extract the bank account ID from stored payment details
        // Implementation depends on how bank account info is stored
        return paymentDetails; // Simplified
    }

    private string GeneratePayoutNumber()
    {
        return $"PO{DateTime.UtcNow:yyyyMMdd}{DateTime.UtcNow.Ticks % 1000:D3}";
    }

    private record EligiblePayout
    {
        public Guid PayoutId { get; init; }
        public Guid ConsignorId { get; init; }
        public string ConsignorName { get; init; } = "";
        public string ConsignorAccountId { get; init; } = "";
        public decimal Amount { get; init; }
        public string PayoutNumber { get; init; } = "";
    }
}