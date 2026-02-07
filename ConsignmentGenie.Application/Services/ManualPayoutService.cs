using ConsignmentGenie.Application.DTOs.Payout;
using ConsignmentGenie.Application.Helpers;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Constants;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// MVP Payout Service - Manual tracking only (no automation)
/// Owner manually pays consignors and marks payouts as paid in the system
/// Phase 5+ will add automated PayPal/Stripe Connect payouts
/// </summary>
public class ManualPayoutService : IPayoutService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ManualPayoutService> _logger;
    private readonly IConsignorNotificationService _notificationService;
    private readonly IClearDateCalculator _clearDateCalculator;

    public ManualPayoutService(
        ConsignmentGenieContext context,
        ILogger<ManualPayoutService> logger,
        IConsignorNotificationService notificationService,
        IClearDateCalculator clearDateCalculator)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _clearDateCalculator = clearDateCalculator;
    }

    public async Task<PayoutReportDto> GeneratePayoutAsync(Guid consignorId, DateTime startDate, DateTime endDate)
    {
        var consignor = await _context.Consignors
            .FirstOrDefaultAsync(p => p.Id == consignorId);

        if (consignor == null)
            throw new ArgumentException($"Consignor {consignorId} not found");

        // Get all unpaid transactions for this consignor in the date range
        var transactions = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Where(t => t.Items.Any(ti => ti.ConsignorId == consignorId)
                     && t.TransactionDate >= startDate
                     && t.TransactionDate <= endDate
                     && !t.ConsignorPaidOut)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync();

        var totalAmount = transactions
            .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == consignorId))
            .Sum(ti => ti.ConsignorAmount);
        var transactionCount = transactions.Count;

        return new PayoutReportDto
        {
            ConsignorId = consignorId,
            ConsignorName = consignor.GetDisplayName(),
            StartDate = startDate,
            EndDate = endDate,
            TotalAmount = totalAmount,
            TransactionCount = transactionCount,
            Status = "Pending",
            GeneratedAt = DateTime.UtcNow,
            Transactions = transactions
                .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == consignorId))
                .Select(ti => new PayoutTransactionDto
                {
                    TransactionId = ti.TransactionId,
                    ItemName = ti.Item.Title,
                    SaleDate = transactions.First(t => t.Id == ti.TransactionId).TransactionDate,
                    SalePrice = ti.UnitPrice * ti.Quantity,
                    ConsignorAmount = ti.ConsignorAmount,
                    ShopAmount = ti.StoreAmount
                }).ToList()
        };
    }

    public async Task<List<PayoutReportDto>> GenerateAllPayoutsAsync(DateTime startDate, DateTime endDate)
    {
        var consignors = await _context.Consignors
            .Where(p => p.Status == Core.Enums.ConsignorStatus.Active)
            .ToListAsync();

        var payouts = new List<PayoutReportDto>();

        foreach (var consignor in consignors)
        {
            var payout = await GeneratePayoutAsync(consignor.Id, startDate, endDate);
            if (payout.TotalAmount > 0) // Only include consignors with amounts owed
            {
                payouts.Add(payout);
            }
        }

        return payouts;
    }

    public async Task MarkPayoutAsPaidAsync(Guid payoutId, string paymentMethod, string? notes = null)
    {
        // In MVP, payoutId represents the consignor ID for a date range
        // Mark all unpaid transactions for this consignor as paid
        var transactions = await _context.Transactions
            .Include(t => t.Items)
            .Where(t => t.Items.Any(ti => ti.ConsignorId == payoutId) && !t.ConsignorPaidOut)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            transaction.ConsignorPaidOut = true;
            transaction.ConsignorPaidOutDate = DateTime.UtcNow;
            transaction.PayoutMethod = paymentMethod;
            transaction.PayoutNotes = notes;
        }

        await _context.SaveChangesAsync();

        // Send notification to consignor about the payout
        if (transactions.Any())
        {
            try
            {
                var consignor = await _context.Consignors
                    .FirstOrDefaultAsync(p => p.Id == payoutId);

                if (consignor != null && consignor.UserId.HasValue)
                {
                    var totalAmount = transactions
                        .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == payoutId))
                        .Sum(ti => ti.ConsignorAmount);

                    var payoutReference = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{payoutId.ToString()[..8].ToUpper()}";

                    var notification = NotificationHelper.CreateManualPayoutProcessedNotification(
                        consignor, totalAmount, paymentMethod, payoutReference);

                    await _notificationService.CreateNotificationAsync(notification);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the payout if notification fails
                _logger.LogError(ex, "Failed to send payout notification for consignor {ConsignorId}", payoutId);
            }
        }

        _logger.LogInformation(
            "[MANUAL PAYOUT] Marked {Count} transactions as paid for consignor {ConsignorId}\n" +
            "  Payment Method: {PaymentMethod}\n" +
            "  Notes: {Notes}",
            transactions.Count, payoutId, paymentMethod, notes ?? "None"
        );
    }

    public async Task<byte[]> ExportPayoutToCsvAsync(Guid payoutId)
    {
        // For MVP, this would need the consignor ID and date range
        // This is a simplified implementation
        var csv = new StringBuilder();
        csv.AppendLine("Consignor,Item,Sale Date,Sale Price,Consignor Amount,Shop Amount");
        csv.AppendLine($"Sample Consignor,Sample Item,{DateTime.Now:yyyy-MM-dd},100.00,50.00,50.00");

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportPayoutsToCsvAsync(List<Guid> payoutIds)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Consignor,Item,Sale Date,Sale Price,Consignor Amount,Shop Amount");

        foreach (var payoutId in payoutIds)
        {
            csv.AppendLine($"Consignor {payoutId},Sample Item,{DateTime.Now:yyyy-MM-dd},100.00,50.00,50.00");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<decimal> GetPendingPayoutAmountAsync(Guid consignorId)
    {
        var pendingAmount = await _context.Transactions
            .Include(t => t.Items)
            .Where(t => t.Items.Any(ti => ti.ConsignorId == consignorId) && !t.ConsignorPaidOut)
            .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == consignorId))
            .SumAsync(ti => ti.ConsignorAmount);

        return pendingAmount;
    }

    public async Task<List<PayoutSummaryDto>> GetPendingPayoutsAsync()
    {
        var today = DateTime.UtcNow.Date;

        // Get unpaid transactions grouped by consignor
        var transactions = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Consignor)
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Where(t => t.PayoutId == null && !t.ConsignorPaidOut)  // Use both new and legacy fields
            .ToListAsync();

        var pendingPayouts = transactions
            .SelectMany(t => t.Items.Select(ti => new { Transaction = t, TransactionItem = ti }))
            .GroupBy(x => x.TransactionItem.ConsignorId)
            .Select(g =>
            {
                var consignor = g.First().TransactionItem.Consignor;
                var transactionItems = g.ToList();

                // Calculate clear dates if not set
                foreach (var item in transactionItems.Where(x => !x.Transaction.ClearDate.HasValue))
                {
                    // Get organization's payout settings or use defaults
                    var settings = _context.PayoutSettings
                        .FirstOrDefault(s => s.OrganizationId == item.Transaction.OrganizationId)
                        ?? new PayoutSettings { Id = Guid.NewGuid(), OrganizationId = item.Transaction.OrganizationId };

                    item.Transaction.ClearDate = _clearDateCalculator.CalculateClearDate(
                        item.Transaction.TransactionDate,
                        item.Transaction.PaymentType ?? "Other",
                        settings
                    );
                }

                var cleared = transactionItems.Where(x => x.Transaction.ClearDate.HasValue && x.Transaction.ClearDate.Value.Date <= today).ToList();
                var uncleared = transactionItems.Where(x => !x.Transaction.ClearDate.HasValue || x.Transaction.ClearDate.Value.Date > today).ToList();

                return new PayoutSummaryDto
                {
                    ConsignorId = g.Key,
                    ConsignorName = consignor.GetDisplayName(),
                    ConsignorNumber = consignor.ConsignorNumber,

                    // Total (backwards compatible)
                    PendingAmount = transactionItems.Sum(x => x.TransactionItem.ConsignorAmount),
                    TransactionCount = transactionItems.Count,

                    // Breakdown
                    ClearedAmount = cleared.Sum(x => x.TransactionItem.ConsignorAmount),
                    UnclearedAmount = uncleared.Sum(x => x.TransactionItem.ConsignorAmount),
                    ClearedTransactionCount = cleared.Count,
                    UnclearedTransactionCount = uncleared.Count,

                    // Transaction list
                    Transactions = transactionItems.Select(x => new PayoutTransactionDto
                    {
                        TransactionId = x.Transaction.Id,
                        SaleDate = x.Transaction.TransactionDate,
                        ClearDate = x.Transaction.ClearDate,
                        IsCleared = x.Transaction.ClearDate.HasValue && x.Transaction.ClearDate.Value.Date <= today,
                        ItemName = x.TransactionItem.Item?.Title,
                        ConsignorAmount = x.TransactionItem.ConsignorAmount,
                        SalePrice = x.TransactionItem.LineTotal,
                        ShopAmount = x.TransactionItem.LineTotal - x.TransactionItem.ConsignorAmount,
                        PaymentMethod = x.Transaction.PaymentType
                    }).OrderBy(x => x.ClearDate).ToList(),

                    EarliestSale = transactionItems.Min(x => x.Transaction.TransactionDate),
                    LatestSale = transactionItems.Max(x => x.Transaction.TransactionDate),
                    EarliestClearDate = uncleared.Any()
                        ? uncleared.Where(x => x.Transaction.ClearDate.HasValue).Min(x => x.Transaction.ClearDate)
                        : null
                };
            })
            .Where(p => p.TransactionCount > 0)
            .OrderByDescending(p => p.ClearedAmount)
            .ToList();

        return pendingPayouts;
    }

    public async Task<PayoutResultDto> ProcessAutomatedPayoutAsync(Guid payoutId)
    {
        // MVP doesn't support automated payouts
        _logger.LogWarning("Automated payouts not supported in MVP. Use MarkPayoutAsPaidAsync for manual tracking.");

        throw new NotImplementedException("Automated payouts will be available in Phase 5+. Use manual payout tracking for MVP.");
    }
}