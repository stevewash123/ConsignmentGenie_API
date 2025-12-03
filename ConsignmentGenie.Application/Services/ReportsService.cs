using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConsignmentGenie.Application.Services;

public class ReportsService : IReportsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportsService> _logger;

    public ReportsService(IUnitOfWork unitOfWork, ILogger<ReportsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;

        // Configure QuestPDF for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ServiceResult<SalesReportDto>> GetSalesReportAsync(Guid organizationId, SalesReportFilterDto filter)
    {
        try
        {
            // Get transactions based on filters
            var transactionsQuery = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == organizationId &&
                                t.SaleDate >= filter.StartDate &&
                                t.SaleDate <= filter.EndDate,
                    includeProperties: "Item,Consignor");

            // Apply filters
            var transactions = transactionsQuery.AsQueryable();

            if (filter.ConsignorIds != null && filter.ConsignorIds.Any())
            {
                transactions = transactions.Where(t => filter.ConsignorIds.Contains(t.ConsignorId));
            }

            if (filter.Categories != null && filter.Categories.Any())
            {
                transactions = transactions.Where(t => filter.Categories.Contains(t.Item.Category ?? ""));
            }

            if (filter.PaymentMethods != null && filter.PaymentMethods.Any())
            {
                transactions = transactions.Where(t => filter.PaymentMethods.Contains(t.PaymentMethod ?? ""));
            }

            var transactionList = transactions.ToList();

            // Calculate metrics
            var totalSales = transactionList.Sum(t => t.SalePrice);
            var shopRevenue = transactionList.Sum(t => t.ShopAmount);
            var providerPayable = transactionList.Sum(t => t.ConsignorAmount);
            var transactionCount = transactionList.Count;
            var averageSale = transactionCount > 0 ? totalSales / transactionCount : 0;

            // Generate chart data
            var chartData = transactionList
                .GroupBy(t => t.SaleDate.Date)
                .Select(g => new SalesChartPointDto
                {
                    Date = g.Key,
                    GrossSales = g.Sum(t => t.SalePrice),
                    ShopRevenue = g.Sum(t => t.ShopAmount),
                    ProviderPayable = g.Sum(t => t.ConsignorAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Generate line items
            var lineItems = transactionList
                .OrderByDescending(t => t.SaleDate)
                .Select(t => new SalesLineItemDto
                {
                    TransactionId = t.Id,
                    Date = t.SaleDate,
                    ItemName = t.Item.Title,
                    Category = t.Item.Category ?? "",
                    ConsignorName = t.Consignor.DisplayName,
                    SalePrice = t.SalePrice,
                    ShopCut = t.ShopAmount,
                    ProviderCut = t.ConsignorAmount,
                    PaymentMethod = t.PaymentMethod ?? ""
                })
                .ToList();

            var result = new SalesReportDto
            {
                TotalSales = totalSales,
                ShopRevenue = shopRevenue,
                ProviderPayable = providerPayable,
                TransactionCount = transactionCount,
                AverageSale = averageSale,
                ChartData = chartData,
                Transactions = lineItems
            };

            return ServiceResult<SalesReportDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales report for organization {OrganizationId}", organizationId);
            return ServiceResult<SalesReportDto>.FailureResult("Failed to generate sales report", new List<string> { ex.Message });
        }
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
                transactions = transactions.Where(t => filter.ConsignorIds.Contains(t.ConsignorId));
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
                .Sum(t => t.ConsignorAmount);
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
                .GroupBy(t => new { t.ConsignorId, t.Consignor.DisplayName })
                .Select(g =>
                {
                    var providerTransactions = g.ToList();
                    var totalSales = providerTransactions.Sum(t => t.SalePrice);
                    var providerCut = providerTransactions.Sum(t => t.ConsignorAmount);
                    var alreadyPaid = providerTransactions
                        .Where(t => t.PayoutStatus == "Paid")
                        .Sum(t => t.ConsignorAmount);
                    var pendingBalance = providerTransactions
                        .Where(t => t.PayoutStatus == "Pending")
                        .Sum(t => t.ConsignorAmount);

                    var lastPayoutDate = providerTransactions
                        .Where(t => t.Payout != null)
                        .Max(t => t.Payout!.PayoutDate);

                    return new PayoutSummaryLineDto
                    {
                        ConsignorId = g.Key.ConsignorId,
                        ConsignorName = g.Key.DisplayName,
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

    public async Task<ServiceResult<DailyReconciliationDto>> GetDailyReconciliationReportAsync(Guid organizationId, DateOnly date)
    {
        try
        {
            var startDate = date.ToDateTime(TimeOnly.MinValue);
            var endDate = date.ToDateTime(TimeOnly.MaxValue);

            var transactions = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == organizationId &&
                                t.SaleDate >= startDate &&
                                t.SaleDate <= endDate,
                    includeProperties: "Item");

            var transactionList = transactions.ToList();

            // Calculate sales by payment method
            var cashSales = transactionList.Where(t => t.PaymentMethod == "Cash").Sum(t => t.SalePrice);
            var cardSales = transactionList.Where(t => t.PaymentMethod == "Card").Sum(t => t.SalePrice);
            var checkSales = transactionList.Where(t => t.PaymentMethod == "Check").Sum(t => t.SalePrice);
            var otherSales = transactionList.Where(t => !new[] { "Cash", "Card", "Check" }.Contains(t.PaymentMethod)).Sum(t => t.SalePrice);
            var totalSales = transactionList.Sum(t => t.SalePrice);

            // Generate transaction lines
            var reconciliationLines = transactionList
                .OrderBy(t => t.SaleDate)
                .Select(t => new ReconciliationLineDto
                {
                    Time = t.SaleDate,
                    TransactionId = t.Id.ToString(),
                    Items = t.Item.Title,
                    PaymentMethod = t.PaymentMethod ?? "",
                    Amount = t.SalePrice
                })
                .ToList();

            var result = new DailyReconciliationDto
            {
                Date = date,
                OpeningBalance = 0, // This would need to be stored somewhere
                CashSales = cashSales,
                CardSales = cardSales,
                CheckSales = checkSales,
                OtherSales = otherSales,
                TotalSales = totalSales,
                ExpectedCash = cashSales, // Opening balance + cash sales
                ActualCash = null,
                Variance = null,
                Notes = "",
                Transactions = reconciliationLines
            };

            return ServiceResult<DailyReconciliationDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily reconciliation report for organization {OrganizationId}", organizationId);
            return ServiceResult<DailyReconciliationDto>.FailureResult("Failed to generate daily reconciliation report", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<DailyReconciliationDto>> SaveDailyReconciliationAsync(Guid organizationId, DailyReconciliationRequestDto request)
    {
        try
        {
            // For now, we'll just calculate the variance and return the result
            // In a full implementation, you might want to store reconciliation data
            var reportResult = await GetDailyReconciliationReportAsync(organizationId, request.Date);

            if (!reportResult.Success)
                return ServiceResult<DailyReconciliationDto>.FailureResult("Failed to get reconciliation data");

            var report = reportResult.Data;
            report.OpeningBalance = request.OpeningBalance;
            report.ActualCash = request.ActualCash;
            report.ExpectedCash = request.OpeningBalance + report.CashSales;
            report.Variance = request.ActualCash - report.ExpectedCash;
            report.Notes = request.Notes;

            return ServiceResult<DailyReconciliationDto>.SuccessResult(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving daily reconciliation for organization {OrganizationId}", organizationId);
            return ServiceResult<DailyReconciliationDto>.FailureResult("Failed to save daily reconciliation", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<TrendsReportDto>> GetTrendsReportAsync(Guid organizationId, TrendsFilterDto filter)
    {
        try
        {
            var transactions = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == organizationId &&
                                t.SaleDate >= filter.StartDate &&
                                t.SaleDate <= filter.EndDate,
                    includeProperties: "Item");

            var transactionList = transactions.ToList();

            // Weekly trends
            var weeklyTrends = transactionList
                .GroupBy(t => new
                {
                    Year = t.SaleDate.Year,
                    Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        t.SaleDate, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
                })
                .Select(g => new WeeklyTrendDto
                {
                    Year = g.Key.Year,
                    Week = g.Key.Week,
                    Revenue = g.Sum(t => t.SalePrice),
                    TransactionCount = g.Count(),
                    AverageTicket = g.Average(t => t.SalePrice),
                    StartDate = FirstDateOfWeek(g.Key.Year, g.Key.Week)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Week)
                .ToList();

            // Category performance trends
            var categoryTrends = transactionList
                .Where(t => !string.IsNullOrEmpty(t.Item.Category))
                .GroupBy(t => t.Item.Category!)
                .Select(g => new CategoryTrendDto
                {
                    Category = g.Key,
                    TotalRevenue = g.Sum(t => t.SalePrice),
                    ItemsSold = g.Count(),
                    AveragePrice = g.Average(t => t.SalePrice),
                    WeeklyData = g.GroupBy(t => new
                    {
                        Year = t.SaleDate.Year,
                        Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            t.SaleDate, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
                    }).Select(wg => new WeeklyDataDto
                    {
                        Week = wg.Key.Week,
                        Year = wg.Key.Year,
                        Revenue = wg.Sum(t => t.SalePrice),
                        Count = wg.Count()
                    }).OrderBy(x => x.Year).ThenBy(x => x.Week).ToList()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            var summary = new TrendsSummaryDto
            {
                TotalPeriods = weeklyTrends.Count,
                AverageWeeklyRevenue = weeklyTrends.Any() ? weeklyTrends.Average(w => w.Revenue) : 0,
                GrowthRate = CalculateGrowthRate(weeklyTrends),
                TopCategory = categoryTrends.FirstOrDefault()?.Category ?? "None"
            };

            var result = new TrendsReportDto
            {
                WeeklyTrends = weeklyTrends,
                CategoryTrends = categoryTrends,
                Summary = summary
            };

            return ServiceResult<TrendsReportDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating trends report for organization {OrganizationId}", organizationId);
            return ServiceResult<TrendsReportDto>.FailureResult("Failed to generate trends report", new List<string> { ex.Message });
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
                .Select(g => new ProviderBreakdownDto
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
                ProviderBreakdown = providerBreakdown
            };

            return ServiceResult<InventoryOverviewDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory overview for organization {OrganizationId}", organizationId);
            return ServiceResult<InventoryOverviewDto>.FailureResult("Failed to generate inventory overview", new List<string> { ex.Message });
        }
    }

    // Export methods
    public async Task<ServiceResult<byte[]>> ExportSalesReportAsync(Guid organizationId, SalesReportFilterDto filter, string format)
    {
        try
        {
            var reportResult = await GetSalesReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return ServiceResult<byte[]>.FailureResult(reportResult.Message, reportResult.Errors);

            var data = reportResult.Data;

            if (format.ToLower() == "csv")
            {
                var csv = GenerateSalesReportCsv(data);
                return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
            }
            else if (format.ToLower() == "pdf")
            {
                var pdf = GenerateSalesReportPdf(data, filter);
                return ServiceResult<byte[]>.SuccessResult(pdf);
            }

            return ServiceResult<byte[]>.FailureResult("Unsupported format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sales report for organization {OrganizationId}", organizationId);
            return ServiceResult<byte[]>.FailureResult("Failed to export sales report", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportProviderPerformanceReportAsync(Guid organizationId, ProviderPerformanceFilterDto filter, string format)
    {
        try
        {
            var reportResult = await GetProviderPerformanceReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return ServiceResult<byte[]>.FailureResult(reportResult.Message, reportResult.Errors);

            var data = reportResult.Data;

            if (format.ToLower() == "csv")
            {
                var csv = GenerateProviderPerformanceCsv(data);
                return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
            }
            else if (format.ToLower() == "pdf")
            {
                var pdf = GenerateProviderPerformancePdf(data, filter);
                return ServiceResult<byte[]>.SuccessResult(pdf);
            }

            return ServiceResult<byte[]>.FailureResult("Unsupported format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting provider performance report for organization {OrganizationId}", organizationId);
            return ServiceResult<byte[]>.FailureResult("Failed to export provider performance report", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportInventoryAgingReportAsync(Guid organizationId, InventoryAgingFilterDto filter, string format)
    {
        try
        {
            var reportResult = await GetInventoryAgingReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return ServiceResult<byte[]>.FailureResult(reportResult.Message, reportResult.Errors);

            var data = reportResult.Data;

            if (format.ToLower() == "csv")
            {
                var csv = GenerateInventoryAgingCsv(data);
                return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
            }
            else if (format.ToLower() == "pdf")
            {
                var pdf = GenerateInventoryAgingPdf(data, filter);
                return ServiceResult<byte[]>.SuccessResult(pdf);
            }

            return ServiceResult<byte[]>.FailureResult("Unsupported format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting inventory aging report for organization {OrganizationId}", organizationId);
            return ServiceResult<byte[]>.FailureResult("Failed to export inventory aging report", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportPayoutSummaryReportAsync(Guid organizationId, PayoutSummaryFilterDto filter, string format)
    {
        try
        {
            var reportResult = await GetPayoutSummaryReportAsync(organizationId, filter);
            if (!reportResult.Success)
                return ServiceResult<byte[]>.FailureResult(reportResult.Message, reportResult.Errors);

            var data = reportResult.Data;

            if (format.ToLower() == "csv")
            {
                var csv = GeneratePayoutSummaryCsv(data);
                return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
            }
            else if (format.ToLower() == "pdf")
            {
                var pdf = GeneratePayoutSummaryPdf(data, filter);
                return ServiceResult<byte[]>.SuccessResult(pdf);
            }

            return ServiceResult<byte[]>.FailureResult("Unsupported format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payout summary report for organization {OrganizationId}", organizationId);
            return ServiceResult<byte[]>.FailureResult("Failed to export payout summary report", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportDailyReconciliationReportAsync(Guid organizationId, DateOnly date, string format)
    {
        try
        {
            var reportResult = await GetDailyReconciliationReportAsync(organizationId, date);
            if (!reportResult.Success)
                return ServiceResult<byte[]>.FailureResult(reportResult.Message, reportResult.Errors);

            var data = reportResult.Data;

            if (format.ToLower() == "csv")
            {
                var csv = GenerateDailyReconciliationCsv(data);
                return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
            }
            else if (format.ToLower() == "pdf")
            {
                var pdf = GenerateDailyReconciliationPdf(data);
                return ServiceResult<byte[]>.SuccessResult(pdf);
            }

            return ServiceResult<byte[]>.FailureResult("Unsupported format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting daily reconciliation report for organization {OrganizationId}", organizationId);
            return ServiceResult<byte[]>.FailureResult("Failed to export daily reconciliation report", new List<string> { ex.Message });
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

    private static DateTime FirstDateOfWeek(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        var firstMonday = jan1.AddDays(daysOffset);
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        var firstWeek = cal.GetWeekOfYear(jan1, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        if (firstWeek <= 1)
        {
            weekOfYear -= 1;
        }

        return firstMonday.AddDays(weekOfYear * 7);
    }

    private static decimal CalculateGrowthRate(List<WeeklyTrendDto> weeklyTrends)
    {
        if (weeklyTrends.Count < 2) return 0;

        var firstWeek = weeklyTrends.FirstOrDefault();
        var lastWeek = weeklyTrends.LastOrDefault();

        if (firstWeek?.Revenue > 0 && lastWeek?.Revenue > 0)
        {
            return ((lastWeek.Revenue - firstWeek.Revenue) / firstWeek.Revenue) * 100;
        }

        return 0;
    }

    // CSV Generation Methods
    private static string GenerateSalesReportCsv(SalesReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Date,Item Name,Category,Consignor Name,Sale Price,Shop Cut,Consignor Cut,Payment Method");

        foreach (var transaction in data.Transactions)
        {
            csv.AppendLine($"{transaction.Date:yyyy-MM-dd},{EscapeCsv(transaction.ItemName)},{EscapeCsv(transaction.Category)},{EscapeCsv(transaction.ConsignorName)},{transaction.SalePrice:F2},{transaction.ShopCut:F2},{transaction.ProviderCut:F2},{EscapeCsv(transaction.PaymentMethod)}");
        }

        return csv.ToString();
    }

    private static string GenerateProviderPerformanceCsv(ProviderPerformanceReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Consignor Name,Items Consigned,Items Sold,Items Available,Total Sales,Sell-Through Rate %,Avg Days to Sell,Pending Payout");

        foreach (var provider in data.Consignors)
        {
            csv.AppendLine($"{EscapeCsv(provider.ConsignorName)},{provider.ItemsConsigned},{provider.ItemsSold},{provider.ItemsAvailable},{provider.TotalSales:F2},{provider.SellThroughRate:F1},{provider.AvgDaysToSell:F0},{provider.PendingPayout:F2}");
        }

        return csv.ToString();
    }

    private static string GenerateInventoryAgingCsv(InventoryAgingReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Item Name,SKU,Category,Consignor Name,Price,Listed Date,Days Listed,Suggested Action");

        foreach (var item in data.Items)
        {
            csv.AppendLine($"{EscapeCsv(item.Name)},{EscapeCsv(item.SKU)},{EscapeCsv(item.Category)},{EscapeCsv(item.ConsignorName)},{item.Price:F2},{item.ListedDate:yyyy-MM-dd},{item.DaysListed},{EscapeCsv(item.SuggestedAction)}");
        }

        return csv.ToString();
    }

    private static string GeneratePayoutSummaryCsv(PayoutSummaryReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Consignor Name,Total Sales,Consignor Cut,Already Paid,Pending Balance,Last Payout Date");

        foreach (var provider in data.Consignors)
        {
            var lastPayoutDate = provider.LastPayoutDate?.ToString("yyyy-MM-dd") ?? "";
            csv.AppendLine($"{EscapeCsv(provider.ConsignorName)},{provider.TotalSales:F2},{provider.ProviderCut:F2},{provider.AlreadyPaid:F2},{provider.PendingBalance:F2},{lastPayoutDate}");
        }

        return csv.ToString();
    }

    private static string GenerateDailyReconciliationCsv(DailyReconciliationDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Time,Transaction ID,Items,Payment Method,Amount");

        foreach (var transaction in data.Transactions)
        {
            csv.AppendLine($"{transaction.Time:HH:mm:ss},{EscapeCsv(transaction.TransactionId)},{EscapeCsv(transaction.Items)},{EscapeCsv(transaction.PaymentMethod)},{transaction.Amount:F2}");
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    // PDF Generation Methods
    private static byte[] GenerateSalesReportPdf(SalesReportDto data, SalesReportFilterDto filter)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Sales Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Period: {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}").FontSize(12);
                        column.Item().PaddingVertical(5);

                        // Summary metrics
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Sales").FontSize(10);
                                col.Item().Text($"${data.TotalSales:F2}").FontSize(16).SemiBold();
                            });

                            row.RelativeItem().Border(1).Padding(10).Column(col =>
                            {
                                col.Item().Text("Shop Revenue").FontSize(10);
                                col.Item().Text($"${data.ShopRevenue:F2}").FontSize(16).SemiBold();
                            });

                            row.RelativeItem().Border(1).Padding(10).Column(col =>
                            {
                                col.Item().Text("Consignor Payable").FontSize(10);
                                col.Item().Text($"${data.ProviderPayable:F2}").FontSize(16).SemiBold();
                            });
                        });

                        column.Item().PaddingVertical(10);

                        // Transactions table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date");
                                header.Cell().Element(CellStyle).Text("Item");
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Amount");
                                header.Cell().Element(CellStyle).Text("Payment");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var transaction in data.Transactions.Take(50)) // Limit for PDF
                            {
                                table.Cell().Element(CellStyle).Text(transaction.Date.ToString("MM/dd/yyyy"));
                                table.Cell().Element(CellStyle).Text(transaction.ItemName);
                                table.Cell().Element(CellStyle).Text(transaction.ConsignorName);
                                table.Cell().Element(CellStyle).Text($"${transaction.SalePrice:F2}");
                                table.Cell().Element(CellStyle).Text(transaction.PaymentMethod);

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GenerateProviderPerformancePdf(ProviderPerformanceReportDto data, ProviderPerformanceFilterDto filter)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Consignor Performance Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Period: {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}").FontSize(12);
                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Consigned");
                                header.Cell().Element(CellStyle).Text("Sold");
                                header.Cell().Element(CellStyle).Text("Available");
                                header.Cell().Element(CellStyle).Text("Sales");
                                header.Cell().Element(CellStyle).Text("Sell %");
                                header.Cell().Element(CellStyle).Text("Pending");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var provider in data.Consignors)
                            {
                                table.Cell().Element(CellStyle).Text(provider.ConsignorName);
                                table.Cell().Element(CellStyle).Text(provider.ItemsConsigned.ToString());
                                table.Cell().Element(CellStyle).Text(provider.ItemsSold.ToString());
                                table.Cell().Element(CellStyle).Text(provider.ItemsAvailable.ToString());
                                table.Cell().Element(CellStyle).Text($"${provider.TotalSales:F0}");
                                table.Cell().Element(CellStyle).Text($"{provider.SellThroughRate:F0}%");
                                table.Cell().Element(CellStyle).Text($"${provider.PendingPayout:F0}");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GenerateInventoryAgingPdf(InventoryAgingReportDto data, InventoryAgingFilterDto filter)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Inventory Aging Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Age Threshold: {filter.AgeThreshold}+ days").FontSize(12);
                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Item");
                                header.Cell().Element(CellStyle).Text("SKU");
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Price");
                                header.Cell().Element(CellStyle).Text("Listed");
                                header.Cell().Element(CellStyle).Text("Days");
                                header.Cell().Element(CellStyle).Text("Action");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var item in data.Items.Take(50)) // Limit for PDF
                            {
                                table.Cell().Element(CellStyle).Text(item.Name);
                                table.Cell().Element(CellStyle).Text(item.SKU);
                                table.Cell().Element(CellStyle).Text(item.ConsignorName);
                                table.Cell().Element(CellStyle).Text($"${item.Price:F0}");
                                table.Cell().Element(CellStyle).Text(item.ListedDate.ToString("MM/dd/yyyy"));
                                table.Cell().Element(CellStyle).Text(item.DaysListed.ToString());
                                table.Cell().Element(CellStyle).Text(item.SuggestedAction);

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GeneratePayoutSummaryPdf(PayoutSummaryReportDto data, PayoutSummaryFilterDto filter)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Payout Summary Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Period: {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}").FontSize(12);
                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Sales");
                                header.Cell().Element(CellStyle).Text("Cut");
                                header.Cell().Element(CellStyle).Text("Paid");
                                header.Cell().Element(CellStyle).Text("Pending");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var provider in data.Consignors)
                            {
                                table.Cell().Element(CellStyle).Text(provider.ConsignorName);
                                table.Cell().Element(CellStyle).Text($"${provider.TotalSales:F0}");
                                table.Cell().Element(CellStyle).Text($"${provider.ProviderCut:F0}");
                                table.Cell().Element(CellStyle).Text($"${provider.AlreadyPaid:F0}");
                                table.Cell().Element(CellStyle).Text($"${provider.PendingBalance:F0}");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GenerateDailyReconciliationPdf(DailyReconciliationDto data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Daily Reconciliation Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Date: {data.Date:yyyy-MM-dd}").FontSize(12);
                        column.Item().PaddingVertical(10);

                        // Summary section
                        column.Item().Border(1).Padding(10).Column(summaryColumn =>
                        {
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Cash Sales: ${data.CashSales:F2}");
                                row.RelativeItem().Text($"Card Sales: ${data.CardSales:F2}");
                            });
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Expected Cash: ${data.ExpectedCash:F2}");
                                row.RelativeItem().Text($"Actual Cash: ${data.ActualCash:F2}");
                            });
                            if (data.Variance.HasValue)
                            {
                                summaryColumn.Item().Text($"Variance: ${data.Variance:F2}").SemiBold();
                            }
                        });

                        column.Item().PaddingVertical(10);

                        // Transactions table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Time");
                                header.Cell().Element(CellStyle).Text("Items");
                                header.Cell().Element(CellStyle).Text("Method");
                                header.Cell().Element(CellStyle).Text("Amount");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var transaction in data.Transactions)
                            {
                                table.Cell().Element(CellStyle).Text(transaction.Time.ToString("HH:mm"));
                                table.Cell().Element(CellStyle).Text(transaction.Items);
                                table.Cell().Element(CellStyle).Text(transaction.PaymentMethod);
                                table.Cell().Element(CellStyle).Text($"${transaction.Amount:F2}");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }
}