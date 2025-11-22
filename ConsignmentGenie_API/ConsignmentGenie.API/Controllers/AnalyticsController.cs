using ConsignmentGenie.API.Attributes;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IUnitOfWork unitOfWork, ILogger<AnalyticsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("revenue")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetRevenueAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var transactions = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == Guid.Parse(organizationId) &&
                               t.SaleDate >= start && t.SaleDate <= end);

            var analytics = new
            {
                totalRevenue = transactions.Sum(t => t.SalePrice),
                shopOwnerRevenue = transactions.Sum(t => t.ShopAmount),
                providerRevenue = transactions.Sum(t => t.ProviderAmount),
                transactionCount = transactions.Count(),
                averageTransaction = transactions.Any() ? transactions.Average(t => t.SalePrice) : 0,
                dailyBreakdown = transactions
                    .GroupBy(t => t.SaleDate.Date)
                    .Select(g => new
                    {
                        date = g.Key,
                        revenue = g.Sum(t => t.SalePrice),
                        transactionCount = g.Count()
                    })
                    .OrderBy(x => x.date)
                    .ToList()
            };

            return Ok(new { success = true, data = analytics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("provider-performance")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetProviderPerformance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var providers = await _unitOfWork.Providers
                .GetAllAsync(p => p.OrganizationId == Guid.Parse(organizationId),
                    includeProperties: "Transactions,Items");

            var performance = providers.Select(provider => new
            {
                providerId = provider.Id,
                providerName = provider.DisplayName,
                totalRevenue = provider.Transactions
                    .Where(t => t.SaleDate >= start && t.SaleDate <= end)
                    .Sum(t => t.SalePrice),
                itemsSold = provider.Transactions
                    .Where(t => t.SaleDate >= start && t.SaleDate <= end)
                    .Count(),
                averageSalePrice = provider.Transactions
                    .Where(t => t.SaleDate >= start && t.SaleDate <= end)
                    .Any() ? provider.Transactions
                        .Where(t => t.SaleDate >= start && t.SaleDate <= end)
                        .Average(t => t.SalePrice) : 0,
                activeItems = provider.Items.Count(i => i.Status == ItemStatus.Available),
                commissionRate = provider.DefaultSplitPercentage
            }).OrderByDescending(p => p.totalRevenue).ToList();

            return Ok(new { success = true, data = performance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider performance analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("inventory")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> GetInventoryAnalytics()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var items = await _unitOfWork.Items
                .GetAllAsync(i => i.OrganizationId == Guid.Parse(organizationId), includeProperties: "Provider");

            var analytics = new
            {
                totalItems = items.Count(),
                availableItems = items.Count(i => i.Status == ItemStatus.Available),
                soldItems = items.Count(i => i.Status == ItemStatus.Sold),
                averagePrice = items.Any() ? items.Average(i => i.Price) : 0,
                totalInventoryValue = items.Where(i => i.Status == ItemStatus.Available).Sum(i => i.Price),
                categoryBreakdown = items
                    .GroupBy(i => i.Category ?? "Uncategorized")
                    .Select(g => new
                    {
                        category = g.Key,
                        count = g.Count(),
                        value = g.Sum(i => i.Price),
                        soldCount = g.Count(i => i.Status == ItemStatus.Sold)
                    })
                    .OrderByDescending(x => x.count)
                    .ToList(),
                providerBreakdown = items
                    .GroupBy(i => new { i.ProviderId, i.Provider.DisplayName })
                    .Select(g => new
                    {
                        providerId = g.Key.ProviderId,
                        providerName = g.Key.DisplayName,
                        itemCount = g.Count(),
                        availableCount = g.Count(i => i.Status == ItemStatus.Available),
                        soldCount = g.Count(i => i.Status == ItemStatus.Sold)
                    })
                    .OrderByDescending(x => x.itemCount)
                    .ToList()
            };

            return Ok(new { success = true, data = analytics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("trends")]
    [RequiresTier(SubscriptionTier.Enterprise)]
    public async Task<IActionResult> GetTrends([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-90);
            var end = endDate ?? DateTime.UtcNow;

            var transactions = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == Guid.Parse(organizationId) &&
                               t.SaleDate >= start && t.SaleDate <= end,
                    includeProperties: "Item");

            // Weekly trends
            var weeklyTrends = transactions
                .GroupBy(t => new
                {
                    Year = t.SaleDate.Year,
                    Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        t.SaleDate, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
                })
                .Select(g => new
                {
                    year = g.Key.Year,
                    week = g.Key.Week,
                    revenue = g.Sum(t => t.SalePrice),
                    transactionCount = g.Count(),
                    averageTicket = g.Average(t => t.SalePrice),
                    startDate = FirstDateOfWeek(g.Key.Year, g.Key.Week)
                })
                .OrderBy(x => x.year).ThenBy(x => x.week)
                .ToList();

            // Category performance trends
            var categoryTrends = transactions
                .Where(t => !string.IsNullOrEmpty(t.Item.Category))
                .GroupBy(t => t.Item.Category)
                .Select(g => new
                {
                    category = g.Key,
                    totalRevenue = g.Sum(t => t.SalePrice),
                    itemsSold = g.Count(),
                    averagePrice = g.Average(t => t.SalePrice),
                    weeklyData = g.GroupBy(t => new
                    {
                        Year = t.SaleDate.Year,
                        Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            t.SaleDate, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
                    }).Select(wg => new
                    {
                        week = wg.Key.Week,
                        year = wg.Key.Year,
                        revenue = wg.Sum(t => t.SalePrice),
                        count = wg.Count()
                    }).OrderBy(x => x.year).ThenBy(x => x.week).ToList()
                })
                .OrderByDescending(x => x.totalRevenue)
                .Take(10)
                .ToList();

            var trends = new
            {
                weeklyTrends,
                categoryTrends,
                summary = new
                {
                    totalPeriods = weeklyTrends.Count,
                    averageWeeklyRevenue = weeklyTrends.Any() ? weeklyTrends.Average(w => w.revenue) : 0,
                    growthRate = 0, // TODO: Calculate growth rate
                    topCategory = categoryTrends.FirstOrDefault()?.category ?? "None"
                }
            };

            return Ok(new { success = true, data = trends });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trend analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("export")]
    [RequiresTier(SubscriptionTier.Pro)]
    public async Task<IActionResult> ExportAnalytics([FromQuery] string format = "csv", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var transactions = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == Guid.Parse(organizationId) &&
                               t.SaleDate >= start && t.SaleDate <= end,
                    includeProperties: "Item,Provider");

            if (format.ToLower() == "csv")
            {
                var csvContent = GenerateCSV(transactions);
                var fileName = $"analytics_{start:yyyy-MM-dd}_{end:yyyy-MM-dd}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }

            return BadRequest("Unsupported format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics");
            return StatusCode(500, "Internal server error");
        }
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

    // TODO: Implement CalculateGrowthRate method

    private static string GenerateCSV(IEnumerable<ConsignmentGenie.Core.Entities.Transaction> transactions)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Date,Item,Provider,Total Amount,Shop Owner Amount,Provider Amount,Category");

        foreach (var transaction in transactions)
        {
            csv.AppendLine($"{transaction.SaleDate:yyyy-MM-dd},{transaction.Item.Title},{transaction.Provider.DisplayName},{transaction.SalePrice},{transaction.ShopAmount},{transaction.ProviderAmount},{transaction.Item.Category}");
        }

        return csv.ToString();
    }
}