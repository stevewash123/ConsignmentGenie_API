using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class PayoutReportService : IPayoutReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PayoutReportService> _logger;

    public PayoutReportService(IUnitOfWork unitOfWork, ILogger<PayoutReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<PayoutSummaryReportDto>> GetPayoutSummaryReportAsync(Guid organizationId, PayoutSummaryFilterDto filter)
    {
        try
        {
            // Get transactions in date range
            var transactionsQuery = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == organizationId &&
                                t.SaleDate >= filter.StartDate &&
                                t.SaleDate <= filter.EndDate,
                    includeProperties: "Consignor,Payout");

            var transactions = transactionsQuery.AsQueryable();

            if (filter.ConsignorIds != null && filter.ConsignorIds.Any())
            {
                transactions = transactions.Where(t => t.Items.Any(ti => filter.ConsignorIds.Contains(ti.ConsignorId)));
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                if (filter.Status == "Pending")
                    transactions = transactions.Where(t => !t.ConsignorPaidOut);
                else if (filter.Status == "Paid")
                    transactions = transactions.Where(t => t.ConsignorPaidOut);
            }

            var transactionList = transactions.ToList();

            // Get payouts in date range
            var payouts = await _unitOfWork.Payouts
                .GetAllAsync(p => p.OrganizationId == organizationId &&
                               p.PayoutDate >= filter.StartDate &&
                               p.PayoutDate <= filter.EndDate);

            // Calculate metrics
            var totalPaid = payouts.Sum(p => p.Amount);
            var totalPending = transactionList
                .Where(t => !t.ConsignorPaidOut)
                .SelectMany(t => t.Items)
                .Sum(ti => ti.ConsignorAmount);
            var averagePayoutAmount = payouts.Any() ? totalPaid / payouts.Count() : 0;

            // Generate chart data
            var chartData = payouts
                .GroupBy(p => p.PayoutDate.Date)
                .Select(g => new PayoutChartPointDto
                {
                    Date = g.Key,
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Generate provider summaries
            var providerSummaries = transactionList
                .SelectMany(t => t.Items.Select(ti => new { Transaction = t, TransactionItem = ti }))
                .GroupBy(x => new { x.TransactionItem.ConsignorId, x.TransactionItem.Consignor.DisplayName })
                .Select(g =>
                {
                    var providerTransactions = g.ToList();
                    var totalSales = providerTransactions.Sum(x => x.TransactionItem.UnitPrice * x.TransactionItem.Quantity);
                    var providerCut = providerTransactions.Sum(x => x.TransactionItem.ConsignorAmount);
                    var alreadyPaid = providerTransactions
                        .Where(x => x.Transaction.ConsignorPaidOut)
                        .Sum(x => x.TransactionItem.ConsignorAmount);
                    var pendingBalance = providerTransactions
                        .Where(x => !x.Transaction.ConsignorPaidOut)
                        .Sum(x => x.TransactionItem.ConsignorAmount);

                    var lastPayoutDate = providerTransactions
                        .Where(x => x.Transaction.ConsignorPaidOut && x.Transaction.ConsignorPaidOutDate.HasValue)
                        .Max(x => (DateTime?)x.Transaction.ConsignorPaidOutDate) ?? DateTime.MinValue;

                    return new PayoutSummaryLineDto
                    {
                        ConsignorId = g.Key.ConsignorId,
                        ConsignorName = g.Key.DisplayName,
                        TotalSales = totalSales,
                        ConsignorCut = providerCut,
                        AlreadyPaid = alreadyPaid,
                        PendingBalance = pendingBalance,
                        LastPayoutDate = lastPayoutDate == DateTime.MinValue ? null : lastPayoutDate
                    };
                })
                .OrderByDescending(p => p.PendingBalance)
                .ToList();

            var providersWithPending = providerSummaries.Count(p => p.PendingBalance > 0);

            var result = new PayoutSummaryReportDto
            {
                TotalPaid = totalPaid,
                TotalPending = totalPending,
                ConsignorsWithPending = providersWithPending,
                AveragePayoutAmount = averagePayoutAmount,
                ChartData = chartData,
                Consignors = providerSummaries
            };

            return ServiceResult<PayoutSummaryReportDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payout summary report for organization {OrganizationId}", organizationId);
            return ServiceResult<PayoutSummaryReportDto>.FailureResult("Failed to generate payout summary report", new List<string> { ex.Message });
        }
    }
}