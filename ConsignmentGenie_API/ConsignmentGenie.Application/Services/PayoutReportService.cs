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
                    includeProperties: "Provider,Payout");

            var transactions = transactionsQuery.AsQueryable();

            if (filter.ProviderIds != null && filter.ProviderIds.Any())
            {
                transactions = transactions.Where(t => filter.ProviderIds.Contains(t.ProviderId));
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                transactions = transactions.Where(t => t.PayoutStatus == filter.Status);
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
                .Where(t => t.PayoutStatus == "Pending")
                .Sum(t => t.ProviderAmount);
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
                .GroupBy(t => new { t.ProviderId, t.Provider.DisplayName })
                .Select(g =>
                {
                    var providerTransactions = g.ToList();
                    var totalSales = providerTransactions.Sum(t => t.SalePrice);
                    var providerCut = providerTransactions.Sum(t => t.ProviderAmount);
                    var alreadyPaid = providerTransactions
                        .Where(t => t.PayoutStatus == "Paid")
                        .Sum(t => t.ProviderAmount);
                    var pendingBalance = providerTransactions
                        .Where(t => t.PayoutStatus == "Pending")
                        .Sum(t => t.ProviderAmount);

                    var lastPayoutDate = providerTransactions
                        .Where(t => t.Payout != null)
                        .Max(t => t.Payout!.PayoutDate);

                    return new PayoutSummaryLineDto
                    {
                        ProviderId = g.Key.ProviderId,
                        ProviderName = g.Key.DisplayName,
                        TotalSales = totalSales,
                        ProviderCut = providerCut,
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
                ProvidersWithPending = providersWithPending,
                AveragePayoutAmount = averagePayoutAmount,
                ChartData = chartData,
                Providers = providerSummaries
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