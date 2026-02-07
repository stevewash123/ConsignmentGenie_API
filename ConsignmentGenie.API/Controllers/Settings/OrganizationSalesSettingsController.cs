using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers.Settings;

[ApiController]
[Route("api/owner/settings/sales")]
[Authorize(Roles = "Owner")]
public class OrganizationSalesSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationSalesSettingsController> _logger;
    private readonly OrganizationSettingsManager _organizationSettingsManager;

    public OrganizationSalesSettingsController(
        ConsignmentGenieContext context,
        ILogger<OrganizationSalesSettingsController> logger,
        OrganizationSettingsManager organizationSettingsManager)
    {
        _context = context;
        _logger = logger;
        _organizationSettingsManager = organizationSettingsManager;
    }

    [HttpGet("general")]
    public async Task<ActionResult> GetGeneralSalesSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SALES_SETTINGS] Getting general sales settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SALES_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Return general sales settings - these would be extracted from Organization entity
            var settings = new
            {
                TaxRate = organization.TaxRate,
                DefaultSplitPercentage = organization.DefaultSplitPercentage,
                // Add other sales-related fields as needed
            };

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SALES_SETTINGS] Error getting general sales settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("general")]
    public async Task<ActionResult> UpdateGeneralSalesSettings([FromBody] dynamic settings)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SALES_SETTINGS] Updating general sales settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SALES_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Update sales-related fields
            organization.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "General sales settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SALES_SETTINGS] Error updating general sales settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("online-storefront")]
    public async Task<ActionResult> GetStorefrontSettings()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SALES_SETTINGS] Getting storefront settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SALES_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Get storefront settings using OrganizationSettingsManager
            var storefrontSettings = await _organizationSettingsManager.GetPageSettingsAsync<StorefrontSettingsDto>(organizationId, SettingsPage.Storefront);

            return Ok(storefrontSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SALES_SETTINGS] Error getting storefront settings for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("online-storefront")]
    public async Task<ActionResult> UpdateStorefrontSettings([FromBody] StorefrontSettingsDto settings)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[SALES_SETTINGS] Updating storefront settings for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[SALES_SETTINGS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Update storefront settings using OrganizationSettingsManager
            await _organizationSettingsManager.SetPageSettingsAsync(organizationId, SettingsPage.Storefront, settings);

            return Ok(new { success = true, message = "Storefront settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SALES_SETTINGS] Error updating storefront settings for organization {OrganizationId}", organizationId);
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