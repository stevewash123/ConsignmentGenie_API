using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Application.Services;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations/storefront")]
[Authorize(Roles = "Owner")]
public class OrganizationStorefrontController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationStorefrontController> _logger;
    private readonly StoreCodeService _storeCodeService;

    public OrganizationStorefrontController(
        ConsignmentGenieContext context,
        ILogger<OrganizationStorefrontController> logger,
        StoreCodeService storeCodeService)
    {
        _context = context;
        _logger = logger;
        _storeCodeService = storeCodeService;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<StorefrontSettingsDto>> GetStorefrontSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[STOREFRONT_SETTINGS] Getting storefront settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[STOREFRONT_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse JSON settings or create defaults
            StorefrontSettingsDto? storefrontSettings = null;
            if (!string.IsNullOrEmpty(organization.StorefrontSettings))
            {
                try
                {
                    storefrontSettings = JsonSerializer.Deserialize<StorefrontSettingsDto>(organization.StorefrontSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[STOREFRONT_SETTINGS] Failed to parse storefront settings JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Create defaults based on existing Organization fields
            storefrontSettings ??= new StorefrontSettingsDto
            {
                SelectedChannel = "cg-storefront",
                Square = new SquareSettingsDto
                {
                    Connected = false,
                    CategoryMappings = new List<CategoryMappingDto>()
                },
                Shopify = new ShopifySettingsDto
                {
                    Connected = false,
                    PushInventory = true,
                    ImportOrders = true,
                    SyncImages = true,
                    AutoMarkSold = true,
                    CollectionMappings = new List<CollectionMappingDto>()
                },
                CgStorefront = new CgStorefrontSettingsDto
                {
                    StoreSlug = organization.Slug ?? "",
                    DnsVerified = false,
                    StripeConnected = organization.StripeConnected,
                    BannerImageUrl = organization.ShopBannerUrl,
                    PrimaryColor = "#2563eb",
                    AccentColor = "#1d4ed8",
                    MetaTitle = organization.Name,
                    MetaDescription = organization.ShopDescription
                },
                InStore = new InStoreSettingsDto
                {
                    UseReceiptNumbers = true,
                    NextReceiptNumber = 1,
                    RequireManagerApproval = false,
                    AllowLayaway = false
                }
            };

            _logger.LogDebug("[STOREFRONT_SETTINGS] Storefront settings retrieved for organization {OrganizationId}", organizationId);
            return Ok(storefrontSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOREFRONT_SETTINGS] Error getting storefront settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<object>> UpdateStorefrontSettings([FromBody] StorefrontSettingsDto storefrontSettingsDto)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[STOREFRONT_SETTINGS] Updating storefront settings for organization {OrganizationId}", organizationId);

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
                _logger.LogWarning("[STOREFRONT_SETTINGS] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Update basic fields that exist in Organization entity
            if (storefrontSettingsDto.CgStorefront != null)
            {
                organization.Slug = storefrontSettingsDto.CgStorefront.StoreSlug;
                organization.ShopBannerUrl = storefrontSettingsDto.CgStorefront.BannerImageUrl;
                organization.StripeConnected = storefrontSettingsDto.CgStorefront.StripeConnected;
            }

            // Store full settings as JSON
            organization.StorefrontSettings = JsonSerializer.Serialize(storefrontSettingsDto);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[STOREFRONT_SETTINGS] Storefront settings updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                message = "Storefront settings updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOREFRONT_SETTINGS] Error updating storefront settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
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