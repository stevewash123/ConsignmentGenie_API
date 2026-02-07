using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Infrastructure.Data;
using System.Security.Claims;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner")]
public class SalesController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<SalesController> _logger;

    public SalesController(ConsignmentGenieContext context, ILogger<SalesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetOrganizationId()
    {
        // Find organization claim by normalizing to lowercase
        var organizationClaim = User.Claims.FirstOrDefault(c =>
            c.Type.ToLowerInvariant() == "organizationid");

        if (organizationClaim == null || string.IsNullOrEmpty(organizationClaim.Value))
        {
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        return organizationClaim.Value;
    }
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

        return Ok(metrics);
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

        return Ok(paymentMethods);
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

        return Ok(sources);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<SalesSettingsDto>> GetSalesSettings()
    {
        var organizationIdString = GetOrganizationId();
        _logger.LogInformation("[SALES_SETTINGS] Getting sales settings for organization {OrganizationId}", organizationIdString);

        if (!Guid.TryParse(organizationIdString, out var organizationId))
        {
            return BadRequest("Invalid organization ID format");
        }

        try
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                return NotFound("Organization not found");
            }

            // Parse JSON settings or create defaults
            SalesSettingsDto? salesSettings = null;
            if (!string.IsNullOrEmpty(organization.SalesSettings))
            {
                try
                {
                    salesSettings = JsonSerializer.Deserialize<SalesSettingsDto>(organization.SalesSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[SALES_SETTINGS] Failed to parse sales settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Create defaults if parsing failed or no settings exist
            salesSettings ??= new SalesSettingsDto
            {
                ShopChoice = "consignmentgenie",
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("[SALES_SETTINGS] Sales settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(salesSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SALES_SETTINGS] Error getting sales settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<object>> UpdateSalesSettings([FromBody] SalesSettingsDto salesSettingsDto)
    {
        var organizationIdString = GetOrganizationId();
        _logger.LogInformation("[SALES_SETTINGS] Updating sales settings for organization {OrganizationId}", organizationIdString);

        if (!Guid.TryParse(organizationIdString, out var organizationId))
        {
            return BadRequest("Invalid organization ID format");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization == null)
            {
                return NotFound("Organization not found");
            }

            // Update timestamp
            salesSettingsDto.UpdatedAt = DateTime.UtcNow;

            // Store settings as JSON
            organization.SalesSettings = JsonSerializer.Serialize(salesSettingsDto);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[SALES_SETTINGS] Sales settings updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                message = "Sales settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SALES_SETTINGS] Error updating sales settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }
}