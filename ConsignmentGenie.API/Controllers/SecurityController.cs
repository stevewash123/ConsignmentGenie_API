using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/owner/settings/security")]
[Authorize(Roles = "Owner")]
public class SecurityController : ControllerBase
{
    private readonly IOwnerApprovalService _approvalService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        IOwnerApprovalService approvalService,
        ILogger<SecurityController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// Set or change the owner PIN
    /// </summary>
    [HttpPost("pin")]
    public async Task<ActionResult> SetPin([FromBody] SetPinRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var userId = GetCurrentUserId();

            if (organizationId == null || userId == null)
                return Unauthorized();

            var result = await _approvalService.SetPinAsync(organizationId.Value, request.Pin, request.CurrentPassword, userId.Value);

            if (!result)
                return BadRequest("Unable to set PIN. Please verify your current password and that the PIN is 4-6 digits.");

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting PIN");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate owner PIN
    /// </summary>
    [HttpPost("validate-pin")]
    public async Task<ActionResult<ValidatePinResponse>> ValidatePin([FromBody] ValidatePinRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            if (organizationId == null)
                return Unauthorized();

            var result = await _approvalService.ValidatePinAsync(organizationId.Value, request.Pin, Core.Enums.ApprovalAction.TaxExempt);

            return Ok(new ValidatePinResponse { Valid = result.Approved });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get PIN requirements settings
    /// </summary>
    [HttpGet("pin-requirements")]
    public async Task<ActionResult<PinRequirementsDto>> GetPinRequirements()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            if (organizationId == null)
                return Unauthorized();

            var settings = await _approvalService.GetSecuritySettingsAsync(organizationId.Value);

            return Ok(settings.PinRequirements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PIN requirements");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update PIN requirements settings
    /// </summary>
    [HttpPut("pin-requirements")]
    public async Task<ActionResult> UpdatePinRequirements([FromBody] UpdatePinRequirementsRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            if (organizationId == null)
                return Unauthorized();

            var result = await _approvalService.UpdatePinRequirementsAsync(organizationId.Value, request);

            if (!result)
                return BadRequest("Unable to update PIN requirements.");

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PIN requirements");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get security settings overview
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SecuritySettingsDto>> GetSecuritySettings()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();

            if (organizationId == null)
                return Unauthorized();

            var settings = await _approvalService.GetSecuritySettingsAsync(organizationId.Value);

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security settings");
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid? GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}