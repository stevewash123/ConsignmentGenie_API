using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class DashboardController : ControllerBase
{
    [HttpGet("metrics")]
    public IActionResult GetDashboardMetrics()
    {
        // Stubbed dashboard metrics
        var metrics = new
        {
            activeProviders = new
            {
                count = 15,
                newThisWeek = 2,
                trend = "up"
            },
            inventoryValue = new
            {
                total = 42750.80m,
                itemsOnFloor = 342,
                trend = "up"
            },
            last30Days = new
            {
                revenue = 8450.25m,
                transactions = 23,
                trend = "up"
            },
            pendingPayouts = new
            {
                amount = 3247.60m,
                providersWaiting = 8,
                trend = "down"
            }
        };

        return Ok(new { success = true, data = metrics });
    }

    [HttpGet("recent-activity")]
    public IActionResult GetRecentActivity()
    {
        // Stubbed recent activity
        var activities = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                type = "sale",
                description = "Item sold: Vintage Handbag",
                amount = (decimal?)125.00m,
                provider = "Sarah Johnson",
                timestamp = DateTime.UtcNow.AddMinutes(-15)
            },
            new
            {
                id = Guid.NewGuid(),
                type = "item_added",
                description = "New item added by provider",
                amount = (decimal?)null,
                provider = "Mike Chen",
                timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new
            {
                id = Guid.NewGuid(),
                type = "payout",
                description = "Payout processed",
                amount = (decimal?)450.75m,
                provider = "Lisa Martinez",
                timestamp = DateTime.UtcNow.AddHours(-4)
            }
        };

        return Ok(new { success = true, data = activities });
    }
}