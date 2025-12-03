using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class ProviderReportService : IProviderReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProviderReportService> _logger;

    public ProviderReportService(IUnitOfWork unitOfWork, ILogger<ProviderReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<ProviderPerformanceReportDto>> GetProviderPerformanceReportAsync(Guid organizationId, ProviderPerformanceFilterDto filter)
    {
        try
        {
            // Get providers
            var providersQuery = await _unitOfWork.Consignors
                .GetAllAsync(p => p.OrganizationId == organizationId,
                    includeProperties: "Items,Transactions");

            var providers = providersQuery.AsQueryable();

            if (!filter.IncludeInactive)
            {
                providers = providers.Where(p => p.Status == ConsignorStatus.Active);
            }

            var providerList = providers.ToList();

            var providerPerformance = new List<ProviderPerformanceLineDto>();

            foreach (var provider in providerList)
            {
                var transactions = provider.Transactions
                    .Where(t => t.SaleDate >= filter.StartDate && t.SaleDate <= filter.EndDate)
                    .ToList();

                var totalItems = provider.Items.Count;
                var itemsSold = transactions.Count;
                var itemsAvailable = provider.Items.Count(i => i.Status == ItemStatus.Available);
                var totalSales = transactions.Sum(t => t.SalePrice);
                var sellThroughRate = totalItems > 0 ? (decimal)itemsSold / totalItems * 100 : 0;

                double avgDaysToSell = 0;
                if (transactions.Any())
                {
                    var daysToSell = transactions
                        .Where(t => t.Item.ListedDate.HasValue)
                        .Select(t => (t.SaleDate - t.Item.ListedDate!.Value.ToDateTime(TimeOnly.MinValue)).TotalDays)
                        .ToList();

                    if (daysToSell.Any())
                        avgDaysToSell = daysToSell.Average();
                }

                var pendingPayout = transactions
                    .Where(t => t.PayoutStatus == "Pending")
                    .Sum(t => t.ConsignorAmount);

                // Apply minimum items threshold
                if (filter.MinItemsThreshold.HasValue && totalItems < filter.MinItemsThreshold.Value)
                    continue;

                providerPerformance.Add(new ProviderPerformanceLineDto
                {
                    ConsignorId = provider.Id,
                    ConsignorName = provider.DisplayName,
                    ItemsConsigned = totalItems,
                    ItemsSold = itemsSold,
                    ItemsAvailable = itemsAvailable,
                    TotalSales = totalSales,
                    SellThroughRate = sellThroughRate,
                    AvgDaysToSell = avgDaysToSell,
                    PendingPayout = pendingPayout
                });
            }

            var orderedProviders = providerPerformance.OrderByDescending(p => p.TotalSales).ToList();
            var totalProviders = providerPerformance.Count;
            var totalSalesAmount = providerPerformance.Sum(p => p.TotalSales);
            var averageSalesPerProvider = totalProviders > 0 ? totalSalesAmount / totalProviders : 0;
            var topProvider = orderedProviders.FirstOrDefault();

            var result = new ProviderPerformanceReportDto
            {
                TotalProviders = totalProviders,
                TotalSales = totalSalesAmount,
                AverageSalesPerProvider = averageSalesPerProvider,
                TopProviderName = topProvider?.ConsignorName ?? "",
                TopProviderSales = topProvider?.TotalSales ?? 0,
                Consignors = orderedProviders
            };

            return ServiceResult<ProviderPerformanceReportDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating provider performance report for organization {OrganizationId}", organizationId);
            return ServiceResult<ProviderPerformanceReportDto>.FailureResult("Failed to generate provider performance report", new List<string> { ex.Message });
        }
    }
}