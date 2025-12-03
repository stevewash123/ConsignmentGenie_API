using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Application.Services;

public class SplitCalculationService : ISplitCalculationService
{
    private readonly ConsignmentGenieContext _context;

    public SplitCalculationService(ConsignmentGenieContext context)
    {
        _context = context;
    }

    public SplitResult CalculateSplit(decimal salePrice, decimal splitPercentage)
    {
        var providerAmount = Math.Round(salePrice * (splitPercentage / 100), 2);
        var shopAmount = salePrice - providerAmount;

        return new SplitResult
        {
            ConsignorAmount = providerAmount,
            ShopAmount = shopAmount,
            SplitPercentage = splitPercentage
        };
    }

    public async Task<PayoutSummary> CalculatePayoutsAsync(Guid providerId, DateTime periodStart, DateTime periodEnd)
    {
        var provider = await _context.Consignors
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null)
        {
            throw new ArgumentException("Consignor not found", nameof(providerId));
        }

        var transactions = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ConsignorId == providerId &&
                       t.SaleDate >= periodStart &&
                       t.SaleDate <= periodEnd)
            .OrderBy(t => t.SaleDate)
            .ToListAsync();

        var payoutTransactions = transactions.Select(t => new PayoutTransaction
        {
            TransactionId = t.Id,
            ItemSku = t.Item.Sku,
            ItemTitle = t.Item.Title,
            SalePrice = t.SalePrice,
            ConsignorAmount = t.ConsignorAmount,
            SaleDate = t.SaleDate
        }).ToList();

        return new PayoutSummary
        {
            ConsignorId = providerId,
            ConsignorName = provider.GetDisplayName(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalAmount = transactions.Sum(t => t.ConsignorAmount),
            TransactionCount = transactions.Count,
            Transactions = payoutTransactions
        };
    }
}