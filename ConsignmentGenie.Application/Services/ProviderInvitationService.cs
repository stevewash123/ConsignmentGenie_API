using ConsignmentGenie.Application.DTOs.Provider;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ConsignmentGenie.Application.Services;

public class ProviderInvitationService : IProviderInvitationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProviderInvitationService> _logger;

    public ProviderInvitationService(ConsignmentGenieContext context, IEmailService emailService, IConfiguration configuration, ILogger<ProviderInvitationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ProviderInvitationResultDto> CreateInvitationAsync(CreateProviderInvitationDto request, Guid organizationId, Guid invitedById)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new ProviderInvitationResultDto
                {
                    Success = false,
                    Message = "A user with this email already exists in the system."
                };
            }

            // Check if there's already a pending invitation
            var existingInvitation = await _context.ProviderInvitations
                .FirstOrDefaultAsync(pi => pi.Email.ToLower() == request.Email.ToLower()
                                          && pi.OrganizationId == organizationId
                                          && pi.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                return new ProviderInvitationResultDto
                {
                    Success = false,
                    Message = "A pending invitation already exists for this email address."
                };
            }

            // Create new invitation
            var invitation = new ProviderInvitation
            {
                OrganizationId = organizationId,
                InvitedById = invitedById,
                Email = request.Email,
                Name = request.Name,
                Token = GenerateSecureToken(),
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7-day expiration
            };

            _context.ProviderInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Get organization and inviter details for email
            var organization = await _context.Organizations.FindAsync(organizationId);
            var inviter = await _context.Users.FindAsync(invitedById);

            // Generate invitation link
            var clientUrl = _configuration["ClientUrl"];

            _logger.LogInformation("Building provider invitation link - ClientUrl: '{ClientUrl}'", clientUrl);

            var baseUrl = clientUrl ?? "http://localhost:4200";
            var inviteLink = $"{baseUrl}/signup/provider?token={invitation.Token}&storeCode={organization?.StoreCode}";

            _logger.LogInformation("Generated provider invitation link: '{InviteLink}' for email: '{Email}'", inviteLink, invitation.Email);

            // Send invitation email
            var emailSent = await _emailService.SendProviderInvitationAsync(
                invitation.Email,
                invitation.Name,
                organization?.Name ?? "ConsignmentGenie Shop",
                inviteLink,
                invitation.ExpiresAt.ToString("MMMM dd, yyyy")
            );

            var result = await GetInvitationDtoAsync(invitation.Id);

            return new ProviderInvitationResultDto
            {
                Success = true,
                Message = "Invitation sent successfully.",
                Invitation = result
            };
        }
        catch (Exception ex)
        {
            return new ProviderInvitationResultDto
            {
                Success = false,
                Message = $"Failed to create invitation: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<ProviderInvitationDto>> GetPendingInvitationsAsync(Guid organizationId)
    {
        return await _context.ProviderInvitations
            .Where(pi => pi.OrganizationId == organizationId && pi.Status == InvitationStatus.Pending)
            .Include(pi => pi.InvitedBy)
            .Select(pi => new ProviderInvitationDto
            {
                Id = pi.Id,
                Name = pi.Name,
                Email = pi.Email,
                Status = pi.Status,
                ExpiresAt = pi.ExpiresAt,
                CreatedAt = pi.CreatedAt,
                InvitedByEmail = pi.InvitedBy.Email
            })
            .OrderByDescending(pi => pi.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProviderInvitationDto?> GetInvitationByTokenAsync(string token)
    {
        var invitation = await _context.ProviderInvitations
            .Include(pi => pi.InvitedBy)
            .Include(pi => pi.Organization)
            .FirstOrDefaultAsync(pi => pi.Token == token);

        if (invitation == null)
            return null;

        return new ProviderInvitationDto
        {
            Id = invitation.Id,
            Name = invitation.Name,
            Email = invitation.Email,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            CreatedAt = invitation.CreatedAt,
            InvitedByEmail = invitation.InvitedBy.Email
        };
    }

    public async Task<bool> CancelInvitationAsync(Guid invitationId, Guid organizationId)
    {
        var invitation = await _context.ProviderInvitations
            .FirstOrDefaultAsync(pi => pi.Id == invitationId && pi.OrganizationId == organizationId);

        if (invitation == null || invitation.Status != InvitationStatus.Pending)
            return false;

        invitation.Status = InvitationStatus.Cancelled;
        invitation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendInvitationAsync(Guid invitationId, Guid organizationId)
    {
        var invitation = await _context.ProviderInvitations
            .Include(pi => pi.Organization)
            .Include(pi => pi.InvitedBy)
            .FirstOrDefaultAsync(pi => pi.Id == invitationId && pi.OrganizationId == organizationId);

        if (invitation == null || invitation.Status != InvitationStatus.Pending)
            return false;

        // Extend expiration
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        invitation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Generate invitation link and send email
        var clientUrl = _configuration["ClientUrl"];

        _logger.LogInformation("Building provider resend invitation link - ClientUrl: '{ClientUrl}'", clientUrl);

        var baseUrl = clientUrl ?? "http://localhost:4200";
        var inviteLink = $"{baseUrl}/signup/provider?token={invitation.Token}&storeCode={invitation.Organization?.StoreCode}";

        _logger.LogInformation("Generated provider resend invitation link: '{InviteLink}' for email: '{Email}'", inviteLink, invitation.Email);

        var emailSent = await _emailService.SendProviderInvitationAsync(
            invitation.Email,
            invitation.Name,
            invitation.Organization?.Name ?? "ConsignmentGenie Shop",
            inviteLink,
            invitation.ExpiresAt.ToString("MMMM dd, yyyy")
        );

        return true;
    }

    private async Task<ProviderInvitationDto?> GetInvitationDtoAsync(Guid invitationId)
    {
        return await _context.ProviderInvitations
            .Where(pi => pi.Id == invitationId)
            .Include(pi => pi.InvitedBy)
            .Select(pi => new ProviderInvitationDto
            {
                Id = pi.Id,
                Name = pi.Name,
                Email = pi.Email,
                Status = pi.Status,
                ExpiresAt = pi.ExpiresAt,
                CreatedAt = pi.CreatedAt,
                InvitedByEmail = pi.InvitedBy.Email
            })
            .FirstOrDefaultAsync();
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
    }
}