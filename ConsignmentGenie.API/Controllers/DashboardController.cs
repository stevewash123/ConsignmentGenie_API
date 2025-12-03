using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Onboarding;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class DashboardController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ConsignmentGenieContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
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
            autoApproveProviders = organization.AutoApproveConsignors,
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

        organization.AutoApproveConsignors = request.AutoApproveConsignors;
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            message = request.AutoApproveConsignors
                ? "Consignor auto-approval enabled. New providers will be automatically approved."
                : "Consignor auto-approval disabled. New providers will require manual approval.",
            data = new { autoApproveProviders = organization.AutoApproveConsignors }
        });
    }

    [HttpGet("organization/onboarding-status")]
    public async Task<ActionResult<object>> GetOnboardingStatus()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[ONBOARDING] Getting onboarding status for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .Include(o => o.Consignors)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[ONBOARDING] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            _logger.LogDebug("[ONBOARDING] Organization {OrganizationId} found: Name={OrganizationName}, OnboardingDismissed={OnboardingDismissed}, ProviderCount={ProviderCount}, ItemCount={ItemCount}, StoreEnabled={StoreEnabled}, StripeConnected={StripeConnected}, QuickBooksConnected={QuickBooksConnected}",
                organizationId, organization.Name, organization.OnboardingDismissed, organization.Consignors?.Count ?? 0, organization.Items?.Count ?? 0, organization.StoreEnabled, organization.StripeConnected, organization.QuickBooksConnected);

            var hasProviders = organization.Consignors.Any();
            var storefrontConfigured = organization.StoreEnabled ||
                                      organization.StripeConnected ||
                                      !string.IsNullOrEmpty(organization.ShopName);
            var hasInventory = organization.Items.Any();
            var quickBooksConnected = organization.QuickBooksConnected;

            // Calculate showModal based on specification logic
            var showModal = !organization.WelcomeGuideCompleted && (
                !hasProviders ||
                !storefrontConfigured ||
                !hasInventory ||
                !quickBooksConnected
            );

            var status = new OnboardingStatusDto
            {
                Dismissed = organization.OnboardingDismissed,
                WelcomeGuideCompleted = organization.WelcomeGuideCompleted,
                ShowModal = showModal,
                Steps = new OnboardingStepsDto
                {
                    HasProviders = hasProviders,
                    StorefrontConfigured = storefrontConfigured,
                    HasInventory = hasInventory,
                    QuickBooksConnected = quickBooksConnected
                }
            };

            _logger.LogInformation("[ONBOARDING] Onboarding status calculated for organization {OrganizationId}: Dismissed={Dismissed}, HasProviders={HasProviders}, StorefrontConfigured={StorefrontConfigured}, HasInventory={HasInventory}, QuickBooksConnected={QuickBooksConnected}",
                organizationId, status.Dismissed, hasProviders, storefrontConfigured, hasInventory, quickBooksConnected);

            return Ok(new { success = true, data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ONBOARDING] Error getting onboarding status for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("organization/dismiss-onboarding")]
    public async Task<ActionResult<object>> DismissOnboarding([FromBody] DismissOnboardingRequestDto request)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[ONBOARDING] Dismissing onboarding for organization {OrganizationId}, Dismissed={Dismissed}", organizationId, request.Dismissed);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[ONBOARDING] Organization {OrganizationId} not found during dismiss operation", organizationId);
                return NotFound("Organization not found");
            }

            var previousStatus = organization.OnboardingDismissed;
            organization.OnboardingDismissed = request.Dismissed;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[ONBOARDING] Onboarding dismissed updated for organization {OrganizationId}: {PreviousStatus} -> {NewStatus}",
                organizationId, previousStatus, request.Dismissed);

            return Ok(new {
                success = true,
                message = "Onboarding status updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ONBOARDING] Error dismissing onboarding for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (organizationIdClaim != null && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }

        throw new UnauthorizedAccessException("Organization ID not found in token");
    }
}

public class UpdateAutoApproveRequest
{
    public bool AutoApproveConsignors { get; set; }
}