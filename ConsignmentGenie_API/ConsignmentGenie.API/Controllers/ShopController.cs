using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Interfaces;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/shop")]
[Authorize(Roles = "Owner")]
public class ShopController : ControllerBase
{
    private readonly IStoreCodeService _storeCodeService;

    public ShopController(IStoreCodeService storeCodeService)
    {
        _storeCodeService = storeCodeService;
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgId))
            return orgId;
        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    [HttpGet("store-code")]
    public async Task<ActionResult<StoreCodeDto>> GetStoreCode()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var storeCode = await _storeCodeService.GetStoreCodeAsync(organizationId);
            return Ok(storeCode);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving store code", error = ex.Message });
        }
    }

    [HttpPost("store-code/regenerate")]
    public async Task<ActionResult<StoreCodeDto>> RegenerateStoreCode()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var storeCode = await _storeCodeService.RegenerateStoreCodeAsync(organizationId);
            return Ok(storeCode);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while regenerating store code", error = ex.Message });
        }
    }

    [HttpPut("store-code/toggle")]
    public async Task<ActionResult> ToggleStoreCode([FromBody] ToggleStoreCodeRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _storeCodeService.ToggleStoreCodeAsync(organizationId, request.Enabled);
            return Ok(new { message = $"Store code registration {(request.Enabled ? "enabled" : "disabled")} successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while toggling store code", error = ex.Message });
        }
    }
}