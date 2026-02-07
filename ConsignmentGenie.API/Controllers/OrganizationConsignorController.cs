using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Core.DTOs.Organization;
using ConsignmentGenie.Core.DTOs.Settings;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/organizations/consignors")]
[Authorize(Roles = "Owner")]
public class OrganizationConsignorController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationConsignorController> _logger;

    public OrganizationConsignorController(ConsignmentGenieContext context, ILogger<OrganizationConsignorController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("default-permissions")]
    public async Task<ActionResult<object>> GetDefaultConsignorPermissions()
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Getting default consignor permissions for organization {OrganizationId}", organizationId);

        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogWarning("[CONSIGNOR_PERMISSIONS] Organization {OrganizationId} not found", organizationId);
                return NotFound("Organization not found");
            }

            // Parse JSON settings or create defaults
            object? permissions = null;
            if (!string.IsNullOrEmpty(organization.ConsignorPermissions))
            {
                try
                {
                    permissions = JsonSerializer.Deserialize<object>(organization.ConsignorPermissions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "[CONSIGNOR_PERMISSIONS] Failed to parse consignor permissions JSON for organization {OrganizationId}", organizationId);
                }
            }

            // Create defaults if parsing failed or no settings exist
            permissions ??= new
            {
                canViewOwnItems = true,
                canEditItemDetails = true,
                canRequestPayout = true,
                canViewSalesHistory = true,
                canUpdateContactInfo = true,
                maxItemsPerSubmission = 50,
                requireApprovalForChanges = false
            };

            _logger.LogDebug("[CONSIGNOR_PERMISSIONS] Default consignor permissions retrieved for organization {OrganizationId}", organizationId);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_PERMISSIONS] Error getting default consignor permissions for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("default-permissions")]
    public async Task<ActionResult<object>> UpdateDefaultConsignorPermissions([FromBody] object permissions)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Updating default consignor permissions for organization {OrganizationId}", organizationId);

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
                _logger.LogWarning("[CONSIGNOR_PERMISSIONS] Organization {OrganizationId} not found during update", organizationId);
                return NotFound("Organization not found");
            }

            // Store permissions as JSON
            organization.ConsignorPermissions = JsonSerializer.Serialize(permissions);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Default consignor permissions updated for organization {OrganizationId}", organizationId);

            return Ok(new {
                success = true,
                message = "Default consignor permissions updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_PERMISSIONS] Error updating default consignor permissions for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPatch("default-permissions")]
    public async Task<ActionResult<object>> UpdateDefaultConsignorPermissionsPartial([FromBody] Dictionary<string, object> updates)
    {
        var organizationId = GetOrganizationId();
        _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Updating default consignor permissions (partial) for organization {OrganizationId}", organizationId);

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
                _logger.LogWarning("[CONSIGNOR_PERMISSIONS] Organization {OrganizationId} not found during partial update", organizationId);
                return NotFound("Organization not found");
            }

            // Parse existing permissions or create defaults
            Dictionary<string, object> currentPermissions;
            if (!string.IsNullOrEmpty(organization.ConsignorPermissions))
            {
                try
                {
                    currentPermissions = JsonSerializer.Deserialize<Dictionary<string, object>>(organization.ConsignorPermissions) ?? new();
                }
                catch
                {
                    currentPermissions = new();
                }
            }
            else
            {
                currentPermissions = new();
            }

            // Apply partial updates
            foreach (var update in updates)
            {
                currentPermissions[update.Key] = update.Value;
            }

            // Store updated permissions as JSON
            organization.ConsignorPermissions = JsonSerializer.Serialize(currentPermissions);
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONSIGNOR_PERMISSIONS] Default consignor permissions partially updated for organization {OrganizationId}", organizationId);

            return Ok(currentPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CONSIGNOR_PERMISSIONS] Error partially updating default consignor permissions for organization {OrganizationId}", organizationId);
            return StatusCode(500, "Internal server error");
        }
    }


    private Dictionary<string, object> CreateDefaultPermissions()
    {
        return new Dictionary<string, object>
        {
            { "canViewInventory", true },
            { "canEditItems", false },
            { "canViewSales", true },
            { "canViewPayouts", true },
            { "canReceiveNotifications", true }
        };
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