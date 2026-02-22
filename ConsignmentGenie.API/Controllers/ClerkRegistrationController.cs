using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/auth/clerk")]
public class ClerkRegistrationController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly IClerkInvitationService _clerkInvitationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClerkRegistrationController> _logger;

    public ClerkRegistrationController(
        ConsignmentGenieContext context,
        IClerkInvitationService clerkInvitationService,
        IConfiguration configuration,
        ILogger<ClerkRegistrationController> logger)
    {
        _context = context;
        _clerkInvitationService = clerkInvitationService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validate a clerk invitation token (public endpoint, no auth required)
    /// </summary>
    [HttpGet("invite/{token}/validate")]
    public async Task<ActionResult<InvitationValidationDto>> ValidateInvitation(string token)
    {
        _logger.LogInformation("[CLERK_INVITATION] Validating clerk invitation token: {Token}", token);

        try
        {
            var invitation = await _context.ClerkInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
            {
                _logger.LogWarning("[CLERK_INVITATION] Token not found: {Token}", token);
                return Ok(new InvitationValidationDto
                {
                    IsValid = false,
                    Message = "Invalid invitation token"
                });
            }

            // Check if invitation has expired
            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("[CLERK_INVITATION] Token expired: {Token}, Expired: {ExpirationDate}",
                    token, invitation.ExpiresAt);
                return Ok(new InvitationValidationDto
                {
                    IsValid = false,
                    Message = "This invitation has expired. Contact your manager to resend."
                });
            }

            // Check if invitation is still pending
            if (invitation.Status != InvitationStatus.Pending)
            {
                _logger.LogWarning("[CLERK_INVITATION] Token not pending: {Token}, Status: {Status}",
                    token, invitation.Status);
                return Ok(new InvitationValidationDto
                {
                    IsValid = false,
                    Message = "This invitation is no longer valid."
                });
            }

            // Return valid invitation details
            _logger.LogInformation("[CLERK_INVITATION] Valid token: {Token}, Organization: {OrganizationName}",
                token, invitation.Organization?.Name);

            // Split the stored name into first and last names
            var nameParts = invitation.Name?.Trim().Split(' ', 2) ?? Array.Empty<string>();
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            return Ok(new InvitationValidationDto
            {
                IsValid = true,
                ShopName = invitation.Organization?.Name,
                InvitedName = firstName,
                InvitedPreferredName = lastName,
                InvitedEmail = invitation.Email,
                ExpirationDate = invitation.ExpiresAt,
                Role = "clerk",
                Message = "Valid invitation"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CLERK_INVITATION] Error validating invitation token: {Token}", token);
            return StatusCode(500, new InvitationValidationDto
            {
                IsValid = false,
                Message = "Unable to validate invitation"
            });
        }
    }

    /// <summary>
    /// Register a new clerk from invitation (public endpoint, no auth required)
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterClerkFromInvitationResponse>> RegisterFromInvitation(
        [FromBody] RegisterClerkFromInvitationRequest request)
    {
        _logger.LogInformation("[CLERK_INVITATION] Processing clerk registration from invitation token: {Token}",
            request.Token);

        try
        {
            // Validate invitation first
            var invitation = await _context.ClerkInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == request.Token);

            if (invitation == null ||
                invitation.ExpiresAt < DateTime.UtcNow ||
                invitation.Status != InvitationStatus.Pending)
            {
                _logger.LogWarning("[CLERK_INVITATION] Invalid or expired invitation: {Token}", request.Token);
                return BadRequest(new RegisterClerkFromInvitationResponse
                {
                    Success = false,
                    Message = "Invalid or expired invitation"
                });
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == invitation.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(new RegisterClerkFromInvitationResponse
                {
                    Success = false,
                    Message = "An account with this email already exists. Try logging in."
                });
            }

            // Create the clerk user
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = invitation.Email,
                Name = request.Name,
                PreferredName = request.PreferredName,
                Phone = request.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Clerk,
                OrganizationId = invitation.OrganizationId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);

            // Mark invitation as accepted
            invitation.Status = InvitationStatus.Accepted;
            invitation.UsedAt = DateTime.UtcNow;
            invitation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Generate JWT token for auto-login
            var token = GenerateJwtToken(newUser, invitation.Organization!);

            _logger.LogInformation("[CLERK_INVITATION] Clerk registered successfully from invitation: {Token}, Email: {Email}",
                request.Token, request.Name + " " + request);

            return Ok(new RegisterClerkFromInvitationResponse
            {
                Success = true,
                Message = "Account created successfully",
                Token = token,
                User = new UserDto
                {
                    Id = newUser.Id,
                    Email = newUser.Email,
                    Name = newUser.Name,
                    PreferredName = newUser.PreferredName,
                    Role = newUser.Role.ToString(),
                    OrganizationId = newUser.OrganizationId,
                    OrganizationName = invitation.Organization.Name
                },
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CLERK_INVITATION] Error during clerk registration from invitation: {Token}",
                request.Token);

            return StatusCode(500, new RegisterClerkFromInvitationResponse
            {
                Success = false,
                Message = "Registration failed. Please try again."
            });
        }
    }

    private string GenerateJwtToken(User user, Organization organization)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"] ?? "your-secret-key");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.Name} {user}".Trim()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("OrganizationId", user.OrganizationId.ToString()),
            new("OrganizationName", organization.Name)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}