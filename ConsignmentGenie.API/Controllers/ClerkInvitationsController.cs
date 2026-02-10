using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Application.DTOs.Clerk;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/owner/clerk-invitations")]
[Authorize]
public class ClerkInvitationsController : ControllerBase
{
    private readonly IClerkInvitationService _clerkInvitationService;
    private readonly ILogger<ClerkInvitationsController> _logger;

    public ClerkInvitationsController(
        IClerkInvitationService clerkInvitationService,
        ILogger<ClerkInvitationsController> logger)
    {
        _clerkInvitationService = clerkInvitationService;
        _logger = logger;
    }

    private Guid GetOrganizationId()
    {
        // Use standard organizationId claim (lowercase following JWT conventions)
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
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

    /// <summary>
    /// Create a new clerk invitation
    /// </summary>
    [HttpPost("invite")]
    public async Task<ActionResult> InviteClerk([FromBody] CreateClerkInvitationDto request)
    {
        var organizationId = GetOrganizationId();
        var invitedById = GetUserId();

        if (organizationId == Guid.Empty || invitedById == Guid.Empty)
        {
            return Unauthorized("Invalid user context");
        }

        _logger.LogInformation("[CLERK_INVITATION] Creating invitation for email: {Email}", request.Email);

        var result = await _clerkInvitationService.CreateInvitationAsync(request, organizationId, invitedById);

        if (result.Success)
        {
            return Ok(new { success = true, data = result.Invitation, message = result.Message });
        }

        return BadRequest(new { success = false, message = result.Message });
    }

    /// <summary>
    /// Get all pending clerk invitations for the organization
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult> GetPendingInvitations()
    {
        var organizationId = GetOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return Unauthorized("Invalid organization context");
        }

        var invitations = await _clerkInvitationService.GetPendingInvitationsAsync(organizationId);
        return Ok(invitations);
    }

    /// <summary>
    /// Resend a clerk invitation
    /// </summary>
    [HttpPost("{invitationId}/resend")]
    public async Task<ActionResult> ResendInvitation(Guid invitationId)
    {
        var organizationId = GetOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return Unauthorized("Invalid organization context");
        }

        _logger.LogInformation("[CLERK_INVITATION] Resending invitation: {InvitationId}", invitationId);

        var success = await _clerkInvitationService.ResendInvitationAsync(invitationId, organizationId);

        if (success)
        {
            return Ok(new { success = true, message = "Invitation resent successfully" });
        }

        return BadRequest(new { success = false, message = "Unable to resend invitation" });
    }

    /// <summary>
    /// Cancel a clerk invitation
    /// </summary>
    [HttpDelete("{invitationId}")]
    public async Task<ActionResult> CancelInvitation(Guid invitationId)
    {
        var organizationId = GetOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return Unauthorized("Invalid organization context");
        }

        _logger.LogInformation("[CLERK_INVITATION] Canceling invitation: {InvitationId}", invitationId);

        var success = await _clerkInvitationService.CancelInvitationAsync(invitationId, organizationId);

        if (success)
        {
            return Ok(new { success = true, message = "Invitation cancelled successfully" });
        }

        return BadRequest(new { success = false, message = "Unable to cancel invitation" });
    }
}

[ApiController]
[Route("api/owner/clerks")]
[Authorize]
public class ClerksController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ClerksController> _logger;

    public ClerksController(
        ConsignmentGenieContext context,
        ILogger<ClerksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetOrganizationId()
    {
        // Use standard organizationId claim (lowercase following JWT conventions)
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgId))
            return orgId;
        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    /// <summary>
    /// Get all active clerks for the organization
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetActiveClerks()
    {
        var organizationId = GetOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return Unauthorized("Invalid organization context");
        }

        var clerks = await _context.Users
            .Where(u => u.OrganizationId == organizationId
                       && u.Role == UserRole.Clerk
                       && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Phone,
                u.IsActive,
                u.CreatedAt
            })
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        return Ok(clerks);
    }

    /// <summary>
    /// Deactivate a clerk
    /// </summary>
    [HttpPut("{clerkId}/deactivate")]
    public async Task<ActionResult> DeactivateClerk(Guid clerkId)
    {
        var organizationId = GetOrganizationId();

        if (organizationId == Guid.Empty)
        {
            return Unauthorized("Invalid organization context");
        }

        _logger.LogInformation("[CLERK_MANAGEMENT] Deactivating clerk: {ClerkId}", clerkId);

        var clerk = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == clerkId
                                     && u.OrganizationId == organizationId
                                     && u.Role == UserRole.Clerk);

        if (clerk == null)
        {
            return BadRequest(new { success = false, message = "Clerk not found" });
        }

        clerk.IsActive = false;
        clerk.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Clerk deactivated successfully" });
    }
}