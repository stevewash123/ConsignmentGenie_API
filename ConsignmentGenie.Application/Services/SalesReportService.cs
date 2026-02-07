using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class SalesReportService : ISalesReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SalesReportService> _logger;

    public SalesReportService(IUnitOfWork unitOfWork, ILogger<SalesReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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
                    includeProperties: "Items.Item.ItemCategoryNavigation,Items.Consignor");

            // Apply filters
            var transactions = transactionsQuery.AsQueryable();

            if (filter.ConsignorIds != null && filter.ConsignorIds.Any())
            {
                transactions = transactions.Where(t => t.Items.Any(ti => filter.ConsignorIds.Contains(ti.ConsignorId)));
            }

            if (filter.Categories != null && filter.Categories.Any())
            {
                transactions = transactions.Where(t => t.Items.Any(ti => filter.Categories.Contains(ti.Item.ItemCategoryNavigation != null ? ti.Item.ItemCategoryNavigation.Name : "")));
            }

            if (filter.PaymentMethods != null && filter.PaymentMethods.Any())
            {
                transactions = transactions.Where(t => filter.PaymentMethods.Contains(t.PaymentType ?? ""));
            }

            var transactionList = transactions.ToList();

            // Calculate metrics
            var totalSales = transactionList.Sum(t => t.Total);
            var shopRevenue = transactionList.SelectMany(t => t.Items).Sum(ti => ti.StoreAmount);
            var providerPayable = transactionList.SelectMany(t => t.Items).Sum(ti => ti.ConsignorAmount);
            var transactionCount = transactionList.Count;
            var averageSale = transactionCount > 0 ? totalSales / transactionCount : 0;

            // Generate chart data
            var chartData = transactionList
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new SalesChartPointDto
                {
                    Date = g.Key,
                    GrossSales = g.Sum(t => t.Total),
                    ShopRevenue = g.SelectMany(t => t.Items).Sum(ti => ti.StoreAmount),
                    ConsignorPayable = g.SelectMany(t => t.Items).Sum(ti => ti.ConsignorAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Generate line items
            var lineItems = transactionList
                .SelectMany(t => t.Items.Select(ti => new { Transaction = t, TransactionItem = ti }))
                .OrderByDescending(x => x.Transaction.TransactionDate)
                .Select(x => new SalesLineItemDto
                {
                    TransactionId = x.Transaction.Id,
                    Date = x.Transaction.TransactionDate,
                    ItemName = x.TransactionItem.Item.Title,
                    Category = x.TransactionItem.Item.ItemCategoryNavigation?.Name ?? "",
                    ConsignorName = x.TransactionItem.Consignor.DisplayName,
                    SalePrice = x.TransactionItem.UnitPrice * x.TransactionItem.Quantity,
                    ShopCut = x.TransactionItem.StoreAmount,
                    ConsignorCut = x.TransactionItem.ConsignorAmount,
                    PaymentMethod = x.Transaction.PaymentType ?? ""
                })
                .ToList();

            var result = new SalesReportDto
            {
                TotalSales = totalSales,
                ShopRevenue = shopRevenue,
                ConsignorPayable = providerPayable,
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
                    Revenue = g.Sum(t => t.Total),
                    TransactionCount = g.Count(),
                    AverageTicket = g.Average(t => t.Total),
                    StartDate = FirstDateOfWeek(g.Key.Year, g.Key.Week)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Week)
                .ToList();

            // Category performance trends
            var categoryTrends = transactionList
                .SelectMany(t => t.Items.Where(ti => ti.Item.ItemCategoryNavigation != null))
                .GroupBy(ti => ti.Item.ItemCategoryNavigation!.Name)
                .Select(g => new CategoryTrendDto
                {
                    Category = g.Key,
                    TotalRevenue = g.Sum(ti => ti.UnitPrice * ti.Quantity),
                    ItemsSold = g.Sum(ti => ti.Quantity),
                    AveragePrice = g.Average(ti => ti.UnitPrice),
                    WeeklyData = g.GroupBy(ti => new
                    {
                        Year = ti.Transaction.TransactionDate.Year,
                        Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            ti.Transaction.TransactionDate, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
                    }).Select(wg => new WeeklyDataDto
                    {
                        Week = wg.Key.Week,
                        Year = wg.Key.Year,
                        Revenue = wg.Sum(ti => ti.UnitPrice * ti.Quantity),
                        Count = wg.Count()
                    }).OrderBy(x => x.Year).ThenBy(x => x.Week).ToList()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            var summary = new TrendsSummaryDto
            {
                TotalPeriods = weeklyTrends.Count(),
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

            var cashSales = transactionList
                .Where(t => t.PaymentType == "Cash")
                .Sum(t => t.Total);

            var cardSales = transactionList
                .Where(t => t.PaymentType != "Cash")
                .Sum(t => t.Total);

            var transactionDetails = transactionList
                .OrderBy(t => t.TransactionDate)
                .Select(t => new ReconciliationLineDto
                {
                    Time = t.TransactionDate,
                    TransactionId = t.Id.ToString(),
                    Items = string.Join(", ", t.Items.Select(ti => ti.Item.Title)),
                    PaymentMethod = t.PaymentType ?? "",
                    Amount = t.Total
                })
                .ToList();

            var result = new DailyReconciliationDto
            {
                Date = date,
                CashSales = cashSales,
                CardSales = cardSales,
                ExpectedCash = cashSales, // In a real system, this might include starting cash drawer amount
                Transactions = transactionDetails
            };

            return ServiceResult<DailyReconciliationDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily reconciliation for organization {OrganizationId} on {Date}", organizationId, date);
            return ServiceResult<DailyReconciliationDto>.FailureResult("Failed to generate daily reconciliation", new List<string> { ex.Message });
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
            report.ActualCash = request.ActualCash;
            report.Variance = request.ActualCash - report.ExpectedCash;
            report.Notes = request.Notes;

            // In a full implementation, you would save this to a DailyReconciliations table
            _logger.LogInformation("Daily reconciliation completed for organization {OrganizationId} on {Date}. Variance: {Variance}",
                organizationId, request.Date, report.Variance);

            return ServiceResult<DailyReconciliationDto>.SuccessResult(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving daily reconciliation for organization {OrganizationId}", organizationId);
            return ServiceResult<DailyReconciliationDto>.FailureResult("Failed to save daily reconciliation", new List<string> { ex.Message });
        }
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private static decimal CalculateGrowthRate(List<WeeklyTrendDto> weeklyTrends)
    {
        if (weeklyTrends.Count < 2) return 0;

        var firstPeriod = weeklyTrends.First().Revenue;
        var lastPeriod = weeklyTrends.Last().Revenue;

        if (firstPeriod == 0) return 0;

        return ((lastPeriod - firstPeriod) / firstPeriod) * 100;
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
}