using ConsignmentGenie.Application.DTOs.Payout;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// MVP Payout Service - Manual tracking only (no automation)
/// Owner manually pays providers and marks payouts as paid in the system
/// Phase 5+ will add automated PayPal/Stripe Connect payouts
/// </summary>
public class ManualPayoutService : IPayoutService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ManualPayoutService> _logger;

    public ManualPayoutService(ConsignmentGenieContext context, ILogger<ManualPayoutService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PayoutReportDto> GeneratePayoutAsync(Guid providerId, DateTime startDate, DateTime endDate)
    {
        var provider = await _context.Providers
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null)
            throw new ArgumentException($"Provider {providerId} not found");

        // Get all unpaid transactions for this provider in the date range
        var transactions = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ProviderId == providerId
                     && t.SaleDate >= startDate
                     && t.SaleDate <= endDate
                     && !t.ProviderPaidOut)
            .OrderBy(t => t.SaleDate)
            .ToListAsync();

        var totalAmount = transactions.Sum(t => t.ProviderAmount);
        var transactionCount = transactions.Count;

        return new PayoutReportDto
        {
            ProviderId = providerId,
            ProviderName = provider.GetDisplayName(),
            StartDate = startDate,
            EndDate = endDate,
            TotalAmount = totalAmount,
            TransactionCount = transactionCount,
            Status = "Pending",
            GeneratedAt = DateTime.UtcNow,
            Transactions = transactions.Select(t => new PayoutTransactionDto
            {
                TransactionId = t.Id,
                ItemName = t.Item.Title,
                SaleDate = t.SaleDate,
                SalePrice = t.SalePrice,
                ProviderAmount = t.ProviderAmount,
                ShopAmount = t.ShopAmount
            }).ToList()
        };
    }

    public async Task<List<PayoutReportDto>> GenerateAllPayoutsAsync(DateTime startDate, DateTime endDate)
    {
        var providers = await _context.Providers
            .Where(p => p.Status == Core.Enums.ProviderStatus.Active)
            .ToListAsync();

        var payouts = new List<PayoutReportDto>();

        foreach (var provider in providers)
        {
            var payout = await GeneratePayoutAsync(provider.Id, startDate, endDate);
            if (payout.TotalAmount > 0) // Only include providers with amounts owed
            {
                payouts.Add(payout);
            }
        }

        return payouts;
    }

    public async Task MarkPayoutAsPaidAsync(Guid payoutId, string paymentMethod, string? notes = null)
    {
        // In MVP, payoutId represents the provider ID for a date range
        // Mark all unpaid transactions for this provider as paid
        var transactions = await _context.Transactions
            .Where(t => t.ProviderId == payoutId && !t.ProviderPaidOut)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            transaction.ProviderPaidOut = true;
            transaction.ProviderPaidOutDate = DateTime.UtcNow;
            transaction.PayoutMethod = paymentMethod;
            transaction.PayoutNotes = notes;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[MANUAL PAYOUT] Marked {Count} transactions as paid for provider {ProviderId}\n" +
            "  Payment Method: {PaymentMethod}\n" +
            "  Notes: {Notes}",
            transactions.Count, payoutId, paymentMethod, notes ?? "None"
        );
    }

    public async Task<byte[]> ExportPayoutToCsvAsync(Guid payoutId)
    {
        // For MVP, this would need the provider ID and date range
        // This is a simplified implementation
        var csv = new StringBuilder();
        csv.AppendLine("Provider,Item,Sale Date,Sale Price,Provider Amount,Shop Amount");
        csv.AppendLine($"Sample Provider,Sample Item,{DateTime.Now:yyyy-MM-dd},100.00,50.00,50.00");

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportPayoutsToCsvAsync(List<Guid> payoutIds)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Provider,Item,Sale Date,Sale Price,Provider Amount,Shop Amount");

        foreach (var payoutId in payoutIds)
        {
            csv.AppendLine($"Provider {payoutId},Sample Item,{DateTime.Now:yyyy-MM-dd},100.00,50.00,50.00");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<decimal> GetPendingPayoutAmountAsync(Guid providerId)
    {
        var pendingAmount = await _context.Transactions
            .Where(t => t.ProviderId == providerId && !t.ProviderPaidOut)
            .SumAsync(t => t.ProviderAmount);

        return pendingAmount;
    }

    public async Task<List<PayoutSummaryDto>> GetPendingPayoutsAsync()
    {
        var pendingPayouts = await _context.Transactions
            .Include(t => t.Provider)
            .Where(t => !t.ProviderPaidOut)
            .GroupBy(t => t.ProviderId)
            .Select(g => new PayoutSummaryDto
            {
                ProviderId = g.Key,
                ProviderName = g.First().Provider.GetDisplayName(),
                PendingAmount = g.Sum(t => t.ProviderAmount),
                TransactionCount = g.Count(),
                OldestTransaction = g.Min(t => t.SaleDate)
            })
            .ToListAsync();

        return pendingPayouts;
    }

    public async Task<PayoutResultDto> ProcessAutomatedPayoutAsync(Guid payoutId)
    {
        // MVP doesn't support automated payouts
        _logger.LogWarning("Automated payouts not supported in MVP. Use MarkPayoutAsPaidAsync for manual tracking.");

        throw new NotImplementedException("Automated payouts will be available in Phase 5+. Use manual payout tracking for MVP.");
    }
}