using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class SalesController : ControllerBase
{
    [HttpGet("metrics")]
    public IActionResult GetSalesMetrics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        // Default to last 30 days if no dates provided
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        // Stubbed sales metrics
        var metrics = new
        {
            totalSales = new
            {
                amount = 15750.25m,
                transactions = 45,
                trend = "up",
                percentChange = 12.5m
            },
            shopRevenue = new
            {
                amount = 7875.13m, // After commissions
                percentage = 50.0m,
                trend = "up"
            },
            providerPayouts = new
            {
                amount = 7875.12m, // Commissions owed
                percentage = 50.0m,
                trend = "stable"
            },
            averageSale = new
            {
                amount = 350.01m,
                trend = "up",
                percentChange = 8.2m
            },
            dateRange = new
            {
                startDate = start,
                endDate = end,
                days = (end - start).Days
            }
        };

        return Ok(new { success = true, data = metrics });
    }

    [HttpGet("transactions")]
    public IActionResult GetRecentTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? paymentMethod = null)
    {
        // Stubbed transaction data
        var transactions = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                date = DateTime.UtcNow.AddHours(-2),
                itemName = "Vintage Designer Handbag",
                providerName = "Sarah Johnson",
                salePrice = 285.00m,
                commission = 142.50m,
                shopAmount = 142.50m,
                paymentMethod = "Credit Card",
                source = "In-Store"
            },
            new
            {
                id = Guid.NewGuid(),
                date = DateTime.UtcNow.AddHours(-5),
                itemName = "Antique Jewelry Set",
                providerName = "Mike Chen",
                salePrice = 450.00m,
                commission = 225.00m,
                shopAmount = 225.00m,
                paymentMethod = "Cash",
                source = "Walk-In"
            },
            new
            {
                id = Guid.NewGuid(),
                date = DateTime.UtcNow.AddDays(-1),
                itemName = "Designer Shoes",
                providerName = "Lisa Martinez",
                salePrice = 175.00m,
                commission = 87.50m,
                shopAmount = 87.50m,
                paymentMethod = "Debit Card",
                source = "Online"
            }
        };

        // Apply filters (stubbed)
        var filteredTransactions = transactions;
        if (!string.IsNullOrEmpty(paymentMethod))
        {
            filteredTransactions = transactions.Where(t =>
                t.paymentMethod.Equals(paymentMethod, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        // Pagination (stubbed)
        var totalCount = filteredTransactions.Length;
        var paginatedTransactions = filteredTransactions
            .Skip((page - 1) * limit)
            .Take(limit);

        return Ok(new
        {
            success = true,
            data = paginatedTransactions,
            pagination = new
            {
                page,
                limit,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / limit)
            }
        });
    }

    [HttpGet("payment-methods")]
    public IActionResult GetPaymentMethods()
    {
        var paymentMethods = new[]
        {
            "All Payment Methods",
            "Cash",
            "Credit Card",
            "Debit Card",
            "Check",
            "Store Credit"
        };

        return Ok(new { success = true, data = paymentMethods });
    }

    [HttpGet("sources")]
    public IActionResult GetSources()
    {
        var sources = new[]
        {
            "All Sources",
            "In-Store",
            "Walk-In",
            "Online",
            "Phone Order",
            "Consignment Event"
        };

        return Ok(new { success = true, data = sources });
    }
}