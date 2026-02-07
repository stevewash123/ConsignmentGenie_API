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
        var consignorAmount = Math.Round(salePrice * (splitPercentage / 100), 2);
        var shopAmount = salePrice - consignorAmount;

        return new SplitResult
        {
            ConsignorAmount = consignorAmount,
            ShopAmount = shopAmount,
            SplitPercentage = splitPercentage
        };
    }

    public async Task<PayoutSummary> CalculatePayoutsAsync(Guid consignorId, DateTime periodStart, DateTime periodEnd)
    {
        var consignor = await _context.Consignors
            .FirstOrDefaultAsync(p => p.Id == consignorId);

        if (consignor == null)
        {
            throw new ArgumentException("Consignor not found", nameof(consignorId));
        }

        var transactions = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Where(t => t.Items.Any(ti => ti.ConsignorId == consignorId) &&
                       t.TransactionDate >= periodStart &&
                       t.TransactionDate <= periodEnd)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync();

        var payoutTransactions = transactions
            .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == consignorId)
                .Select(ti => new PayoutTransaction
                {
                    TransactionId = t.Id,
                    ItemSku = ti.Item.Sku,
                    ItemTitle = ti.Item.Title,
                    SalePrice = ti.UnitPrice * ti.Quantity,
                    ConsignorAmount = ti.ConsignorAmount,
                    SaleDate = t.TransactionDate
                }))
            .ToList();

        return new PayoutSummary
        {
            ConsignorId = consignorId,
            ConsignorName = consignor.GetDisplayName(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalAmount = transactions.SelectMany(t => t.Items.Where(ti => ti.ConsignorId == consignorId)).Sum(ti => ti.ConsignorAmount),
            TransactionCount = transactions.Count(),
            Transactions = payoutTransactions
        };
    }
}