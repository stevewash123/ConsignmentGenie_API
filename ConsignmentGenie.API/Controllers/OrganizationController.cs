using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Onboarding;
using ConsignmentGenie.Core.DTOs.Organization;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class OrganizationController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(ConsignmentGenieContext context, ILogger<OrganizationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("setup-status")]
    public async Task<ActionResult<object>> GetSetupStatus()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SETUP] Getting setup status for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .Include(o => o.Providers)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SETUP] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            _logger.LogDebug("[SETUP] Organization {OrganizationId} found: Name={OrganizationName}, WelcomeGuideCompleted={WelcomeGuideCompleted}, ProviderCount={ProviderCount}, ItemCount={ItemCount}, StoreEnabled={StoreEnabled}, StripeConnected={StripeConnected}, QuickBooksConnected={QuickBooksConnected}",
                organizationId, organization.Name, organization.WelcomeGuideCompleted, organization.Providers?.Count ?? 0, organization.Items?.Count ?? 0, organization.StoreEnabled, organization.StripeConnected, organization.QuickBooksConnected);

            var hasProviders = organization.Providers.Any();
            var storefrontConfigured = organization.StoreEnabled ||
                                      organization.StripeConnected;
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

            _logger.LogInformation("[SETUP] Setup status calculated for organization {OrganizationId}: WelcomeGuideCompleted={WelcomeGuideCompleted}, ShowModal={ShowModal}, HasProviders={HasProviders}, StorefrontConfigured={StorefrontConfigured}, HasInventory={HasInventory}, QuickBooksConnected={QuickBooksConnected}",
                organizationId, status.WelcomeGuideCompleted, showModal, hasProviders, storefrontConfigured, hasInventory, quickBooksConnected);

            return Ok(new { success = true, data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETUP] Error getting setup status for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("dismiss-welcome-guide")]
    public async Task<ActionResult<object>> DismissWelcomeGuide()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SETUP] Dismissing welcome guide for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SETUP] Organization {OrganizationId} not found during dismiss operation", organizationId);
                return NotFound("Organization not found");
            }

            var previousStatus = organization.WelcomeGuideCompleted;
            organization.WelcomeGuideCompleted = true;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[SETUP] Welcome guide dismissed for organization {OrganizationId}: {PreviousStatus} -> {NewStatus}",
                organizationId, previousStatus, true);

            return Ok(new {
                success = true,
                welcomeGuideCompleted = true,
                message = "Welcome guide dismissed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETUP] Error dismissing welcome guide for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ShopProfileDto>> GetShopProfile()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[PROFILE] Getting shop profile for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[PROFILE] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            var profile = new ShopProfileDto
            {
                ShopName = organization.ShopName ?? organization.Name,
                ShopDescription = organization.ShopDescription,
                ShopLogoUrl = organization.ShopLogoUrl,
                ShopBannerUrl = organization.ShopBannerUrl,
                ShopAddress1 = organization.ShopAddress1,
                ShopAddress2 = organization.ShopAddress2,
                ShopCity = organization.ShopCity,
                ShopState = organization.ShopState,
                ShopZip = organization.ShopZip,
                ShopCountry = organization.ShopCountry,
                ShopPhone = organization.ShopPhone,
                ShopEmail = organization.ShopEmail,
                ShopWebsite = organization.ShopWebsite,
                ShopTimezone = organization.ShopTimezone
            };

            _logger.LogDebug("[PROFILE] Shop profile retrieved for organization {OrganizationId}: {ShopName}",
                organizationId, profile.ShopName);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROFILE] Error getting shop profile for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult<object>> UpdateShopProfile([FromBody] ShopProfileDto profileDto)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[PROFILE] Updating shop profile for organization {OrganizationId}", organizationId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[PROFILE] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Update shop profile fields
            organization.ShopName = profileDto.ShopName;
            organization.ShopDescription = profileDto.ShopDescription;
            organization.ShopLogoUrl = profileDto.ShopLogoUrl;
            organization.ShopBannerUrl = profileDto.ShopBannerUrl;
            organization.ShopAddress1 = profileDto.ShopAddress1;
            organization.ShopAddress2 = profileDto.ShopAddress2;
            organization.ShopCity = profileDto.ShopCity;
            organization.ShopState = profileDto.ShopState;
            organization.ShopZip = profileDto.ShopZip;
            organization.ShopCountry = profileDto.ShopCountry;
            organization.ShopPhone = profileDto.ShopPhone;
            organization.ShopEmail = profileDto.ShopEmail;
            organization.ShopWebsite = profileDto.ShopWebsite;
            organization.ShopTimezone = profileDto.ShopTimezone;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[PROFILE] Shop profile updated for organization {OrganizationId}: {ShopName}",
                organizationId, profileDto.ShopName);

            return Ok(new {
                success = true,
                message = "Shop profile updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROFILE] Error updating shop profile for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("store-code/regenerate")]
    public async Task<ActionResult<StoreCodeRegenerationDto>> RegenerateStoreCode()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[STORE_CODE] Regenerating store code for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[STORE_CODE] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            var oldStoreCode = organization.StoreCode;

            // Generate new 8-character alphanumeric store code
            var newStoreCode = GenerateStoreCode();

            // Ensure uniqueness
            while (await _context.Organizations.AnyAsync(o => o.StoreCode == newStoreCode))
            {
                newStoreCode = GenerateStoreCode();
            }

            organization.StoreCode = newStoreCode;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[STORE_CODE] Store code regenerated for organization {OrganizationId}: {OldCode} -> {NewCode}",
                organizationId, oldStoreCode, newStoreCode);

            var result = new StoreCodeRegenerationDto
            {
                NewStoreCode = newStoreCode,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STORE_CODE] Error regenerating store code for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private string GenerateStoreCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
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