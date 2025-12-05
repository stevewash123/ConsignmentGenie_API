using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class InventoryReportService : IInventoryReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryReportService> _logger;

    public InventoryReportService(IUnitOfWork unitOfWork, ILogger<InventoryReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<InventoryAgingReportDto>> GetInventoryAgingReportAsync(Guid organizationId, InventoryAgingFilterDto filter)
    {
        try
        {
            var itemsQuery = await _unitOfWork.Items
                .GetAllAsync(i => i.OrganizationId == organizationId &&
                               i.Status == ItemStatus.Available,
                    includeProperties: "Consignor");

            var items = itemsQuery.AsQueryable();

            // Apply filters
            if (filter.Categories != null && filter.Categories.Any())
            {
                items = items.Where(i => filter.Categories.Contains(i.Category ?? ""));
            }

            if (filter.ConsignorIds != null && filter.ConsignorIds.Any())
            {
                items = items.Where(i => filter.ConsignorIds.Contains(i.ConsignorId));
            }

            if (filter.MinPrice.HasValue)
            {
                items = items.Where(i => i.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                items = items.Where(i => i.Price <= filter.MaxPrice.Value);
            }

            var itemList = items.ToList();
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            // Calculate aging
            var agingItems = itemList
                .Where(i => i.ListedDate.HasValue)
                .Select(i => new
                {
                    Item = i,
                    DaysListed = currentDate.DayNumber - i.ListedDate!.Value.DayNumber
                })
                .Where(x => x.DaysListed >= filter.AgeThreshold)
                .ToList();

            // Calculate metrics
            var totalAvailable = itemList.Count;
            var over30Days = agingItems.Count(x => x.DaysListed >= 30);
            var over60Days = agingItems.Count(x => x.DaysListed >= 60);
            var over90Days = agingItems.Count(x => x.DaysListed >= 90);
            var averageAge = agingItems.Any() ? agingItems.Average(x => x.DaysListed) : 0;

            // Generate aging buckets
            var agingBuckets = new List<AgingBucketDto>
            {
                new AgingBucketDto
                {
                    Bucket = "0-30",
                    Count = agingItems.Count(x => x.DaysListed >= 0 && x.DaysListed <= 30),
                    Value = agingItems.Where(x => x.DaysListed >= 0 && x.DaysListed <= 30).Sum(x => x.Item.Price)
                },
                new AgingBucketDto
                {
                    Bucket = "31-60",
                    Count = agingItems.Count(x => x.DaysListed > 30 && x.DaysListed <= 60),
                    Value = agingItems.Where(x => x.DaysListed > 30 && x.DaysListed <= 60).Sum(x => x.Item.Price)
                },
                new AgingBucketDto
                {
                    Bucket = "61-90",
                    Count = agingItems.Count(x => x.DaysListed > 60 && x.DaysListed <= 90),
                    Value = agingItems.Where(x => x.DaysListed > 60 && x.DaysListed <= 90).Sum(x => x.Item.Price)
                },
                new AgingBucketDto
                {
                    Bucket = "90+",
                    Count = agingItems.Count(x => x.DaysListed > 90),
                    Value = agingItems.Where(x => x.DaysListed > 90).Sum(x => x.Item.Price)
                }
            };

            // Generate aging items with suggested actions
            var agingItemDtos = agingItems
                .OrderByDescending(x => x.DaysListed)
                .Select(x => new AgingItemDto
                {
                    ItemId = x.Item.Id,
                    Name = x.Item.Title,
                    SKU = x.Item.Sku,
                    Category = x.Item.Category ?? "",
                    ConsignorName = x.Item.Consignor.DisplayName,
                    Price = x.Item.Price,
                    ListedDate = x.Item.ListedDate!.Value,
                    DaysListed = x.DaysListed,
                    SuggestedAction = GetSuggestedAction(x.DaysListed, x.Item.Price)
                })
                .ToList();

            var result = new InventoryAgingReportDto
            {
                TotalAvailable = totalAvailable,
                Over30Days = over30Days,
                Over60Days = over60Days,
                Over90Days = over90Days,
                AverageAge = averageAge,
                AgingBuckets = agingBuckets,
                Items = agingItemDtos
            };

            return ServiceResult<InventoryAgingReportDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory aging report for organization {OrganizationId}", organizationId);
            return ServiceResult<InventoryAgingReportDto>.FailureResult("Failed to generate inventory aging report", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<InventoryOverviewDto>> GetInventoryOverviewAsync(Guid organizationId)
    {
        try
        {
            var items = await _unitOfWork.Items
                .GetAllAsync(i => i.OrganizationId == organizationId, includeProperties: "Consignor");

            var itemList = items.ToList();

            var categoryBreakdown = itemList
                .GroupBy(i => i.Category ?? "Uncategorized")
                .Select(g => new CategoryBreakdownDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(i => i.Price),
                    SoldCount = g.Count(i => i.Status == ItemStatus.Sold)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var providerBreakdown = itemList
                .GroupBy(i => new { i.ConsignorId, i.Consignor.DisplayName })
                .Select(g => new ConsignorBreakdownDto
                {
                    ConsignorId = g.Key.ConsignorId,
                    ConsignorName = g.Key.DisplayName,
                    ItemCount = g.Count(),
                    AvailableCount = g.Count(i => i.Status == ItemStatus.Available),
                    SoldCount = g.Count(i => i.Status == ItemStatus.Sold)
                })
                .OrderByDescending(x => x.ItemCount)
                .ToList();

            var result = new InventoryOverviewDto
            {
                TotalItems = itemList.Count,
                AvailableItems = itemList.Count(i => i.Status == ItemStatus.Available),
                SoldItems = itemList.Count(i => i.Status == ItemStatus.Sold),
                AveragePrice = itemList.Any() ? itemList.Average(i => i.Price) : 0,
                TotalInventoryValue = itemList.Where(i => i.Status == ItemStatus.Available).Sum(i => i.Price),
                CategoryBreakdown = categoryBreakdown,
                ConsignorBreakdown = providerBreakdown
            };

            return ServiceResult<InventoryOverviewDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory overview for organization {OrganizationId}", organizationId);
            return ServiceResult<InventoryOverviewDto>.FailureResult("Failed to generate inventory overview", new List<string> { ex.Message });
        }
    }

    private static string GetSuggestedAction(int daysListed, decimal price)
    {
        return daysListed switch
        {
            > 180 => "Donate",
            > 120 => "Return to Consignor",
            > 90 when price > 50 => "Price Reduce",
            > 90 => "Return to Consignor",
            _ => "Monitor"
        };
    }
}