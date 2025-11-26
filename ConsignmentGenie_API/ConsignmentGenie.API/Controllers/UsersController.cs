using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Interfaces;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Owner")]
public class UsersController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public UsersController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgId))
            return orgId;
        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    [HttpGet("pending-approval")]
    public async Task<ActionResult<List<PendingApprovalDto>>> GetPendingApprovals()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var pendingApprovals = await _registrationService.GetPendingProvidersAsync(organizationId);
            return Ok(pendingApprovals);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving pending approvals", error = ex.Message });
        }
    }

    [HttpGet("pending-approval/count")]
    public async Task<ActionResult<int>> GetPendingApprovalCount()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var count = await _registrationService.GetPendingApprovalCountAsync(organizationId);
            return Ok(count);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving pending approval count", error = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveUser(Guid id)
    {
        try
        {
            var approvedByUserId = GetUserId();
            await _registrationService.ApproveUserAsync(id, approvedByUserId);
            return Ok(new { message = "User approved successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while approving user", error = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectUser(Guid id, [FromBody] RejectUserRequest request)
    {
        try
        {
            var rejectedByUserId = GetUserId();
            await _registrationService.RejectUserAsync(id, rejectedByUserId, request.Reason);
            return Ok(new { message = "User rejected successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while rejecting user", error = ex.Message });
        }
    }
}