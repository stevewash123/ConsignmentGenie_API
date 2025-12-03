using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.DTOs;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/providers")]
[Authorize(Roles = "Owner")]
public class ProviderMetricsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ProviderMetricsController> _logger;

    public ProviderMetricsController(ConsignmentGenieContext context, ILogger<ProviderMetricsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET PROVIDER METRICS - Get detailed metrics for specific provider
    [HttpGet("{id:guid}/metrics")]
    public async Task<ActionResult<ApiResponse<ProviderMetricsDto>>> GetProviderMetrics(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();

            // Verify provider exists
            var providerExists = await _context.Consignors
                .AnyAsync(p => p.Id == id && p.OrganizationId == organizationId);

            if (!providerExists)
            {
                return NotFound(ApiResponse<ProviderMetricsDto>.ErrorResult("Consignor not found"));
            }

            var metrics = await CalculateProviderMetrics(id, organizationId);
            return Ok(ApiResponse<ProviderMetricsDto>.SuccessResult(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for provider {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ProviderMetricsDto>.ErrorResult("Failed to retrieve provider metrics"));
        }
    }

    // GET PROVIDER DASHBOARD - Get summary metrics for dashboard
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<ProviderDashboardMetricsDto>>> GetProviderDashboard()
    {
        try
        {
            var organizationId = GetOrganizationId();

            var totalProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId)
                .CountAsync();

            var activeProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == ConsignorStatus.Active)
                .CountAsync();

            var pendingProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .CountAsync();

            var deactivatedProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == ConsignorStatus.Deactivated)
                .CountAsync();

            // Get providers with highest pending balances
            var topProvidersData = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == ConsignorStatus.Active)
                .Include(p => p.Transactions)
                .Include(p => p.Items)
                .ToListAsync();

            var topProviders = new List<ProviderTopPerformerDto>();

            foreach (var provider in topProvidersData.Take(5))
            {
                var pendingBalance = await CalculatePendingBalance(provider.Id, organizationId);
                var totalEarnings = provider.Transactions.Sum(t => t.ConsignorAmount);

                topProviders.Add(new ProviderTopPerformerDto
                {
                    ConsignorId = provider.Id,
                    ConsignorName = $"{provider.FirstName} {provider.LastName}",
                    PendingBalance = pendingBalance,
                    TotalEarnings = totalEarnings,
                    ActiveItems = provider.Items.Count(i => i.Status == ItemStatus.Available),
                    TotalItems = provider.Items.Count
                });
            }

            // Sort by pending balance descending
            topProviders = topProviders.OrderByDescending(p => p.PendingBalance).Take(5).ToList();

            // Calculate monthly growth
            var now = DateTime.UtcNow;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            var thisMonthNewProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.CreatedAt >= startOfThisMonth)
                .CountAsync();

            var lastMonthNewProviders = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId &&
                           p.CreatedAt >= startOfLastMonth && p.CreatedAt < startOfThisMonth)
                .CountAsync();

            var providerGrowthRate = lastMonthNewProviders > 0
                ? ((decimal)(thisMonthNewProviders - lastMonthNewProviders) / lastMonthNewProviders) * 100
                : 0;

            var dashboardMetrics = new ProviderDashboardMetricsDto
            {
                TotalProviders = totalProviders,
                ActiveProviders = activeProviders,
                PendingProviders = pendingProviders,
                DeactivatedProviders = deactivatedProviders,
                NewProvidersThisMonth = thisMonthNewProviders,
                ProviderGrowthRate = providerGrowthRate,
                TopProvidersByBalance = topProviders
            };

            return Ok(ApiResponse<ProviderDashboardMetricsDto>.SuccessResult(dashboardMetrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider dashboard for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<ProviderDashboardMetricsDto>.ErrorResult("Failed to retrieve provider dashboard"));
        }
    }

    // GET PROVIDER ACTIVITY - Get recent activity for provider
    [HttpGet("{id:guid}/activity")]
    public async Task<ActionResult<ApiResponse<ProviderActivityDto>>> GetProviderActivity(
        Guid id,
        [FromQuery] int days = 30)
    {
        try
        {
            var organizationId = GetOrganizationId();

            // Verify provider exists
            var provider = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderActivityDto>.ErrorResult("Consignor not found"));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            // Get recent transactions
            var recentTransactions = await _context.Transactions
                .Include(t => t.Item)
                .Where(t => t.ConsignorId == id && t.OrganizationId == organizationId && t.SaleDate >= cutoffDate)
                .OrderByDescending(t => t.SaleDate)
                .Select(t => new ProviderActivityTransactionDto
                {
                    TransactionId = t.Id,
                    SaleDate = t.SaleDate,
                    ItemName = t.Item.Title,
                    SalePrice = t.SalePrice,
                    ConsignorAmount = t.ConsignorAmount,
                    PaymentMethod = t.PaymentMethod ?? ""
                })
                .ToListAsync();

            // Get recent items added
            var recentItems = await _context.Items
                .Where(i => i.ConsignorId == id && i.OrganizationId == organizationId && i.CreatedAt >= cutoffDate)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new ProviderActivityItemDto
                {
                    ItemId = i.Id,
                    ItemName = i.Title,
                    Price = i.Price,
                    Status = i.Status.ToString(),
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            // Get recent payouts
            var recentPayouts = await _context.Payouts
                .Where(p => p.ConsignorId == id && p.OrganizationId == organizationId && p.CreatedAt >= cutoffDate)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProviderActivityPayoutDto
                {
                    PayoutId = p.Id,
                    Amount = p.Amount,
                    PayoutDate = p.PayoutDate,
                    Method = p.PaymentMethod ?? "",
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            var activity = new ProviderActivityDto
            {
                ConsignorId = id,
                ConsignorName = $"{provider.FirstName} {provider.LastName}",
                DaysRange = days,
                RecentTransactions = recentTransactions,
                RecentItems = recentItems,
                RecentPayouts = recentPayouts,
                TotalTransactions = recentTransactions.Count,
                TotalItemsAdded = recentItems.Count,
                TotalPayouts = recentPayouts.Count
            };

            return Ok(ApiResponse<ProviderActivityDto>.SuccessResult(activity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity for provider {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ProviderActivityDto>.ErrorResult("Failed to retrieve provider activity"));
        }
    }

    #region Private Helper Methods

    private async Task<ProviderMetricsDto> CalculateProviderMetrics(Guid providerId, Guid organizationId)
    {
        var items = await _context.Items
            .Where(i => i.ConsignorId == providerId && i.OrganizationId == organizationId)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.ConsignorId == providerId && t.OrganizationId == organizationId)
            .ToListAsync();

        var payouts = await _context.Payouts
            .Where(p => p.ConsignorId == providerId && p.OrganizationId == organizationId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var totalEarnings = transactions.Sum(t => t.ConsignorAmount);
        var totalPaid = payouts.Sum(p => p.Amount);

        var thisMonthTransactions = transactions.Where(t => t.SaleDate >= startOfMonth).ToList();
        var lastMonthTransactions = transactions
            .Where(t => t.SaleDate >= startOfLastMonth && t.SaleDate < startOfMonth)
            .ToList();

        return new ProviderMetricsDto
        {
            TotalItems = items.Count,
            AvailableItems = items.Count(i => i.Status == ItemStatus.Available),
            SoldItems = items.Count(i => i.Status == ItemStatus.Sold),
            RemovedItems = items.Count(i => i.Status == ItemStatus.Removed),
            InventoryValue = items.Where(i => i.Status == ItemStatus.Available).Sum(i => i.Price),
            PendingBalance = await CalculatePendingBalance(providerId, organizationId),
            TotalEarnings = totalEarnings,
            TotalPaid = totalPaid,
            EarningsThisMonth = thisMonthTransactions.Sum(t => t.ConsignorAmount),
            EarningsLastMonth = lastMonthTransactions.Sum(t => t.ConsignorAmount),
            SalesThisMonth = thisMonthTransactions.Count,
            SalesLastMonth = lastMonthTransactions.Count,
            LastSaleDate = transactions.OrderByDescending(t => t.SaleDate).FirstOrDefault()?.SaleDate,
            LastPayoutDate = payouts.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.CreatedAt,
            LastPayoutAmount = payouts.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.Amount ?? 0,
            AverageItemPrice = items.Count > 0 ? items.Average(i => i.Price) : 0,
            AverageDaysToSell = CalculateAverageDaysToSell(items, transactions)
        };
    }

    private async Task<decimal> CalculatePendingBalance(Guid providerId, Guid organizationId)
    {
        var totalEarnings = await _context.Transactions
            .Where(t => t.ConsignorId == providerId && t.OrganizationId == organizationId)
            .SumAsync(t => t.ConsignorAmount);

        var totalPaid = await _context.Payouts
            .Where(p => p.ConsignorId == providerId && p.OrganizationId == organizationId)
            .SumAsync(p => p.Amount);

        return totalEarnings - totalPaid;
    }

    private static decimal CalculateAverageDaysToSell(List<Item> items, List<Transaction> transactions)
    {
        var soldItems = items.Where(i => i.Status == ItemStatus.Sold).ToList();
        if (!soldItems.Any()) return 0;

        var totalDays = 0m;
        var count = 0;

        foreach (var item in soldItems)
        {
            var transaction = transactions.FirstOrDefault(t => t.ItemId == item.Id);
            if (transaction != null)
            {
                var days = (decimal)(transaction.SaleDate - item.CreatedAt).TotalDays;
                if (days >= 0)
                {
                    totalDays += days;
                    count++;
                }
            }
        }

        return count > 0 ? totalDays / count : 0;
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        return orgIdClaim != null ? Guid.Parse(orgIdClaim) : Guid.Empty;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }

    #endregion
}