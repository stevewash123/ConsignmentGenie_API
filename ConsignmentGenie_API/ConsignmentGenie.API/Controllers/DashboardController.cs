using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class DashboardController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;

    public DashboardController(ConsignmentGenieContext context)
    {
        _context = context;
    }
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

    [HttpGet("organization/settings")]
    public async Task<ActionResult<object>> GetOrganizationSettings()
    {
        var organizationId = GetOrganizationId();

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        var settings = new
        {
            autoApproveProviders = organization.AutoApproveProviders,
            storeCodeEnabled = organization.StoreCodeEnabled,
            storeCode = organization.StoreCode
        };

        return Ok(new { success = true, data = settings });
    }

    [HttpPut("organization/settings/auto-approve")]
    public async Task<ActionResult<object>> UpdateAutoApproveProviders([FromBody] UpdateAutoApproveRequest request)
    {
        var organizationId = GetOrganizationId();

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
        {
            return NotFound("Organization not found");
        }

        organization.AutoApproveProviders = request.AutoApproveProviders;
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            message = request.AutoApproveProviders
                ? "Provider auto-approval enabled. New providers will be automatically approved."
                : "Provider auto-approval disabled. New providers will require manual approval.",
            data = new { autoApproveProviders = organization.AutoApproveProviders }
        });
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }
}

public class UpdateAutoApproveRequest
{
    public bool AutoApproveProviders { get; set; }
}