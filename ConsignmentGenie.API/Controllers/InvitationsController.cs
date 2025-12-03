using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Application.Services.Interfaces;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<InvitationsController> _logger;
    private readonly IProviderInvitationService _invitationService;

    public InvitationsController(
        ConsignmentGenieContext context,
        ILogger<InvitationsController> logger,
        IProviderInvitationService invitationService)
    {
        _context = context;
        _logger = logger;
        _invitationService = invitationService;
    }

    /// <summary>
    /// Validate a provider invitation token (public endpoint, no auth required)
    /// </summary>
    [HttpGet("validate/{token}")]
    public async Task<ActionResult<InvitationValidationDto>> ValidateInvitation(string token)
    {
        _logger.LogInformation("[INVITATION] Validating invitation token: {Token}", token);

        try
        {
            var invitation = await _context.ProviderInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
            {
                _logger.LogWarning("[INVITATION] Token not found: {Token}", token);
                return Ok(new InvitationValidationDto
                {
                    IsValid = false,
                    Message = "Invalid invitation token"
                });
            }

            // Check if invitation has expired
            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("[INVITATION] Token expired: {Token}, Expired: {ExpirationDate}",
                    token, invitation.ExpiresAt);
                return Ok(new InvitationValidationDto
                {
                    IsValid = false,
                    Message = "This invitation has expired"
                });
            }

            // Check if invitation is still pending
            if (invitation.Status != Core.Entities.InvitationStatus.Pending)
            {
                _logger.LogWarning("[INVITATION] Token not pending: {Token}, Status: {Status}",
                    token, invitation.Status);
                return Ok(new InvitationValidationDto
                {
                    IsValid = false,
                    Message = "This invitation is no longer valid"
                });
            }

            // Return valid invitation details
            _logger.LogInformation("[INVITATION] Valid token: {Token}, Organization: {OrganizationName}",
                token, invitation.Organization?.Name);

            return Ok(new InvitationValidationDto
            {
                IsValid = true,
                ShopName = invitation.Organization?.Name,
                InvitedName = invitation.Name,
                InvitedEmail = invitation.Email,
                ExpirationDate = invitation.ExpiresAt,
                Message = "Valid invitation"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INVITATION] Error validating invitation token: {Token}", token);
            return StatusCode(500, new InvitationValidationDto
            {
                IsValid = false,
                Message = "Unable to validate invitation"
            });
        }
    }

    /// <summary>
    /// Register a new provider from invitation (public endpoint, no auth required)
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterProviderFromInvitationResponse>> RegisterFromInvitation(
        [FromBody] RegisterProviderFromInvitationRequest request)
    {
        _logger.LogInformation("[INVITATION] Processing provider registration from invitation token: {Token}",
            request.InvitationToken);

        try
        {
            // Validate invitation first
            var invitation = await _context.ProviderInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == request.InvitationToken);

            if (invitation == null ||
                invitation.ExpiresAt < DateTime.UtcNow ||
                invitation.Status != Core.Entities.InvitationStatus.Pending)
            {
                _logger.LogWarning("[INVITATION] Invalid or expired invitation: {Token}", request.InvitationToken);
                return BadRequest(new RegisterProviderFromInvitationResponse
                {
                    Success = false,
                    Message = "Invalid or expired invitation"
                });
            }

            // Use the invitation service to complete registration
            var result = await _invitationService.RegisterFromInvitationAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("[INVITATION] Consignor registered successfully from invitation: {Token}, Email: {Email}",
                    request.InvitationToken, request.Email);
            }
            else
            {
                _logger.LogWarning("[INVITATION] Consignor registration failed: {Token}, Error: {Error}",
                    request.InvitationToken, result.Message);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INVITATION] Error during provider registration from invitation: {Token}",
                request.InvitationToken);

            return StatusCode(500, new RegisterProviderFromInvitationResponse
            {
                Success = false,
                Message = "Registration failed. Please try again."
            });
        }
    }
}