using ConsignmentGenie.Application.DTOs.Clerk;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ConsignmentGenie.Application.Services;

public class ClerkInvitationService : IClerkInvitationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClerkInvitationService> _logger;

    public ClerkInvitationService(ConsignmentGenieContext context, IEmailService emailService, IConfiguration configuration, ILogger<ClerkInvitationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ClerkInvitationResultDto> CreateInvitationAsync(CreateClerkInvitationDto request, Guid organizationId, Guid invitedById)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new ClerkInvitationResultDto
                {
                    Success = false,
                    Message = "A user with this email already exists in the system."
                };
            }

            // Check if there's already a pending invitation
            var existingInvitation = await _context.ClerkInvitations
                .FirstOrDefaultAsync(ci => ci.Email.ToLower() == request.Email.ToLower()
                                          && ci.OrganizationId == organizationId
                                          && ci.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                return new ClerkInvitationResultDto
                {
                    Success = false,
                    Message = "An invitation to this email is already pending."
                };
            }

            // Generate secure token
            var token = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddDays(7); // 7 days to accept

            // Create invitation
            var invitation = new ClerkInvitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                InvitedById = invitedById,
                Email = request.Email,
                Name = request.Name,
                Phone = request.Phone,
                Token = token,
                Status = InvitationStatus.Pending,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClerkInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Get organization details for email
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            var inviter = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == invitedById);

            if (organization != null && inviter != null)
            {
                // Send invitation email
                await SendInvitationEmail(invitation, organization, inviter);
            }

            // Create result DTO
            var resultDto = new ClerkInvitationDto
            {
                Id = invitation.Id,
                Name = invitation.Name,
                Email = invitation.Email,
                Phone = invitation.Phone,
                Status = invitation.Status,
                ExpiresAt = invitation.ExpiresAt,
                CreatedAt = invitation.CreatedAt,
                InvitedByEmail = inviter?.Email ?? ""
            };

            return new ClerkInvitationResultDto
            {
                Success = true,
                Message = "Clerk invitation sent successfully.",
                Invitation = resultDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating clerk invitation for email {Email}", request.Email);
            return new ClerkInvitationResultDto
            {
                Success = false,
                Message = "An error occurred while creating the invitation."
            };
        }
    }

    public async Task<IEnumerable<ClerkInvitationDto>> GetPendingInvitationsAsync(Guid organizationId)
    {
        var invitations = await _context.ClerkInvitations
            .Include(ci => ci.InvitedBy)
            .Where(ci => ci.OrganizationId == organizationId && ci.Status == InvitationStatus.Pending)
            .OrderByDescending(ci => ci.CreatedAt)
            .Select(ci => new ClerkInvitationDto
            {
                Id = ci.Id,
                Name = ci.Name,
                Email = ci.Email,
                Phone = ci.Phone,
                Status = ci.Status,
                ExpiresAt = ci.ExpiresAt,
                CreatedAt = ci.CreatedAt,
                InvitedByEmail = ci.InvitedBy.Email
            })
            .ToListAsync();

        return invitations;
    }

    public async Task<ClerkInvitationDto?> GetInvitationByTokenAsync(string token)
    {
        var invitation = await _context.ClerkInvitations
            .Include(ci => ci.InvitedBy)
            .Include(ci => ci.Organization)
            .FirstOrDefaultAsync(ci => ci.Token == token && ci.Status == InvitationStatus.Pending);

        if (invitation == null || invitation.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return new ClerkInvitationDto
        {
            Id = invitation.Id,
            Name = invitation.Name,
            Email = invitation.Email,
            Phone = invitation.Phone,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            CreatedAt = invitation.CreatedAt,
            InvitedByEmail = invitation.InvitedBy.Email
        };
    }

    public async Task<bool> CancelInvitationAsync(Guid invitationId, Guid organizationId)
    {
        try
        {
            var invitation = await _context.ClerkInvitations
                .FirstOrDefaultAsync(ci => ci.Id == invitationId && ci.OrganizationId == organizationId);

            if (invitation == null || invitation.Status != InvitationStatus.Pending)
            {
                return false;
            }

            invitation.Status = InvitationStatus.Cancelled;
            invitation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling clerk invitation {InvitationId}", invitationId);
            return false;
        }
    }

    public async Task<bool> ResendInvitationAsync(Guid invitationId, Guid organizationId)
    {
        try
        {
            var invitation = await _context.ClerkInvitations
                .Include(ci => ci.Organization)
                .Include(ci => ci.InvitedBy)
                .FirstOrDefaultAsync(ci => ci.Id == invitationId && ci.OrganizationId == organizationId);

            if (invitation == null || invitation.Status != InvitationStatus.Pending)
            {
                return false;
            }

            // Extend expiration and resend
            invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
            invitation.UpdatedAt = DateTime.UtcNow;

            await SendInvitationEmail(invitation, invitation.Organization, invitation.InvitedBy);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending clerk invitation {InvitationId}", invitationId);
            return false;
        }
    }

    private async Task SendInvitationEmail(ClerkInvitation invitation, Organization organization, User inviter)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://app.consignmentgenie.com";
        var invitationUrl = $"{frontendUrl}/signup/clerk/accept/{invitation.Token}";

        var emailSubject = $"You're invited to join {organization.Name} as a staff member";
        var emailBody = $@"
Hi {invitation.Name},

You've been invited by {inviter.FullName ?? inviter.Email} to join {organization.Name} as a staff member on ConsignmentGenie.

As a staff member, you'll have access to the point-of-sale system to help with customer transactions.

To accept this invitation and create your account:
1. Click here: {invitationUrl}
2. Complete your account setup
3. Start helping customers!

This invitation expires on {invitation.ExpiresAt:MMM dd, yyyy} at {invitation.ExpiresAt:HH:mm} UTC.

Questions? Reply to this email or contact {inviter.Email}.

Welcome to the team!
- ConsignmentGenie
";

        await _emailService.SendSimpleEmailAsync(invitation.Email, emailSubject, emailBody, isHtml: false);
    }

    private static string GenerateSecureToken()
    {
        const int tokenLength = 32;
        var randomBytes = new byte[tokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
    }
}