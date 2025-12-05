using ConsignmentGenie.Application.DTOs.Consignor;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services;

public class ConsignorInvitationService : IConsignorInvitationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsignorInvitationService> _logger;

    public ConsignorInvitationService(ConsignmentGenieContext context, IEmailService emailService, IConfiguration configuration, ILogger<ConsignorInvitationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ConsignorInvitationResultDto> CreateInvitationAsync(CreateConsignorInvitationDto request, Guid organizationId, Guid invitedById)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new ConsignorInvitationResultDto
                {
                    Success = false,
                    Message = "A user with this email already exists in the system."
                };
            }

            // Check if there's already a pending invitation
            var existingInvitation = await _context.ConsignorInvitations
                .FirstOrDefaultAsync(pi => pi.Email.ToLower() == request.Email.ToLower()
                                          && pi.OrganizationId == organizationId
                                          && pi.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                return new ConsignorInvitationResultDto
                {
                    Success = false,
                    Message = "A pending invitation already exists for this email address."
                };
            }

            // Create new invitation
            var invitation = new ConsignorInvitation
            {
                OrganizationId = organizationId,
                InvitedById = invitedById,
                Email = request.Email,
                Name = request.Name,
                Token = GenerateSecureToken(),
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7-day expiration
            };

            _context.ConsignorInvitations.Add(invitation);
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
            var emailSent = await _emailService.SendConsignorInvitationAsync(
                invitation.Email,
                invitation.Name,
                organization?.Name ?? "ConsignmentGenie Shop",
                inviteLink,
                invitation.ExpiresAt.ToString("MMMM dd, yyyy")
            );

            var result = await GetInvitationDtoAsync(invitation.Id);

            return new ConsignorInvitationResultDto
            {
                Success = true,
                Message = "Invitation sent successfully.",
                Invitation = result
            };
        }
        catch (Exception ex)
        {
            return new ConsignorInvitationResultDto
            {
                Success = false,
                Message = $"Failed to create invitation: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<ConsignorInvitationDto>> GetPendingInvitationsAsync(Guid organizationId)
    {
        return await _context.ConsignorInvitations
            .Where(pi => pi.OrganizationId == organizationId && pi.Status == InvitationStatus.Pending)
            .Include(pi => pi.InvitedBy)
            .Select(pi => new ConsignorInvitationDto
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

    public async Task<ConsignorInvitationDto?> GetInvitationByTokenAsync(string token)
    {
        var invitation = await _context.ConsignorInvitations
            .Include(pi => pi.InvitedBy)
            .Include(pi => pi.Organization)
            .FirstOrDefaultAsync(pi => pi.Token == token);

        if (invitation == null)
            return null;

        return new ConsignorInvitationDto
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
        var invitation = await _context.ConsignorInvitations
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
        var invitation = await _context.ConsignorInvitations
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

        var emailSent = await _emailService.SendConsignorInvitationAsync(
            invitation.Email,
            invitation.Name,
            invitation.Organization?.Name ?? "ConsignmentGenie Shop",
            inviteLink,
            invitation.ExpiresAt.ToString("MMMM dd, yyyy")
        );

        return true;
    }

    public async Task<RegisterConsignorFromInvitationResponse> RegisterFromInvitationAsync(RegisterConsignorFromInvitationRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate invitation
            var invitation = await _context.ConsignorInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == request.InvitationToken &&
                                         i.Status == InvitationStatus.Pending &&
                                         i.ExpiresAt > DateTime.UtcNow);

            if (invitation == null)
            {
                return new RegisterConsignorFromInvitationResponse
                {
                    Success = false,
                    Message = "Invalid or expired invitation"
                };
            }

            // Check if email matches invitation
            if (!string.Equals(invitation.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                return new RegisterConsignorFromInvitationResponse
                {
                    Success = false,
                    Message = "Email address does not match invitation"
                };
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new RegisterConsignorFromInvitationResponse
                {
                    Success = false,
                    Message = "A user with this email already exists"
                };
            }

            // Generate consignor number
            var consignorNumber = await GenerateProviderNumberAsync(invitation.OrganizationId);

            // Create user account
            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Consignor,
                OrganizationId = invitation.OrganizationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Split full name
            var nameParts = request.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            // Create consignor record
            var consignor = new Consignor
            {
                Id = Guid.NewGuid(),
                OrganizationId = invitation.OrganizationId,
                UserId = user.Id,
                ConsignorNumber = consignorNumber,
                FirstName = firstName,
                LastName = lastName,
                Email = request.Email,
                Phone = request.Phone,
                Status = invitation.Organization?.AutoApproveConsignors == true
                    ? ConsignorStatus.Active
                    : ConsignorStatus.Pending,
                ApprovalStatus = invitation.Organization?.AutoApproveConsignors == true
                    ? "Approved"
                    : "Pending",
                CommissionRate = invitation.Organization?.DefaultSplitPercentage ?? 60.00m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id
            };

            if (consignor.Status == ConsignorStatus.Active)
            {
                consignor.ApprovedAt = DateTime.UtcNow;
                consignor.ApprovedBy = user.Id; // Self-approved via auto-approval
            }

            _context.Consignors.Add(consignor);

            // Mark invitation as accepted
            invitation.Status = InvitationStatus.Accepted;
            invitation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("[INVITATION] Consignor registered successfully: {Email}, ConsignorNumber: {ConsignorNumber}",
                request.Email, consignorNumber);

            return new RegisterConsignorFromInvitationResponse
            {
                Success = true,
                Message = "Registration completed successfully",
                ConsignorId = consignor.Id,
                ConsignorNumber = consignorNumber
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[INVITATION] Failed to register provider from invitation: {Email}", request.Email);

            return new RegisterConsignorFromInvitationResponse
            {
                Success = false,
                Message = "Registration failed. Please try again."
            };
        }
    }

    private async Task<string> GenerateProviderNumberAsync(Guid organizationId)
    {
        var lastNumber = await _context.Consignors
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.ConsignorNumber)
            .Select(p => p.ConsignorNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNumber != null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length > 1 && int.TryParse(parts[1], out int num))
            {
                nextNumber = num + 1;
            }
        }

        return $"PRV-{nextNumber:D5}";
    }

    private async Task<ConsignorInvitationDto?> GetInvitationDtoAsync(Guid invitationId)
    {
        return await _context.ConsignorInvitations
            .Where(pi => pi.Id == invitationId)
            .Include(pi => pi.InvitedBy)
            .Select(pi => new ConsignorInvitationDto
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