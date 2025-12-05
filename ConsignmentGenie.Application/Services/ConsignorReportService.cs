using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class ConsignorReportService : IConsignorReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConsignorReportService> _logger;

    public ConsignorReportService(IUnitOfWork unitOfWork, ILogger<ConsignorReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<ConsignorPerformanceReportDto>> GetConsignorPerformanceReportAsync(Guid organizationId, ConsignorPerformanceFilterDto filter)
    {
        try
        {
            // Get consignors
            var consignorsQuery = await _unitOfWork.Consignors
                .GetAllAsync(p => p.OrganizationId == organizationId,
                    includeProperties: "Items,Transactions");

            var consignors = consignorsQuery.AsQueryable();

            if (!filter.IncludeInactive)
            {
                consignors = consignors.Where(p => p.Status == ConsignorStatus.Active);
            }

            var consignorList = consignors.ToList();

            var consignorPerformance = new List<ConsignorPerformanceLineDto>();

            foreach (var consignor in consignorList)
            {
                var transactions = consignor.Transactions
                    .Where(t => t.SaleDate >= filter.StartDate && t.SaleDate <= filter.EndDate)
                    .ToList();

                var totalItems = consignor.Items.Count;
                var itemsSold = transactions.Count;
                var itemsAvailable = consignor.Items.Count(i => i.Status == ItemStatus.Available);
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

                consignorPerformance.Add(new ConsignorPerformanceLineDto
                {
                    ConsignorId = consignor.Id,
                    ConsignorName = consignor.DisplayName,
                    ItemsConsigned = totalItems,
                    ItemsSold = itemsSold,
                    ItemsAvailable = itemsAvailable,
                    TotalSales = totalSales,
                    SellThroughRate = sellThroughRate,
                    AvgDaysToSell = avgDaysToSell,
                    PendingPayout = pendingPayout
                });
            }

            var orderedConsignors = consignorPerformance.OrderByDescending(p => p.TotalSales).ToList();
            var totalConsignors = consignorPerformance.Count;
            var totalSalesAmount = consignorPerformance.Sum(p => p.TotalSales);
            var averageSalesPerConsignor = totalConsignors > 0 ? totalSalesAmount / totalConsignors : 0;
            var topConsignor = orderedConsignors.FirstOrDefault();

            var result = new ConsignorPerformanceReportDto
            {
                TotalConsignors = totalConsignors,
                TotalSales = totalSalesAmount,
                AverageSalesPerConsignor = averageSalesPerConsignor,
                TopConsignorName = topConsignor?.ConsignorName ?? "",
                TopConsignorSales = topConsignor?.TotalSales ?? 0,
                Consignors = orderedConsignors
            };

            return ServiceResult<ConsignorPerformanceReportDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating consignor performance report for organization {OrganizationId}", organizationId);
            return ServiceResult<ConsignorPerformanceReportDto>.FailureResult("Failed to generate consignor performance report", new List<string> { ex.Message });
        }
    }
}