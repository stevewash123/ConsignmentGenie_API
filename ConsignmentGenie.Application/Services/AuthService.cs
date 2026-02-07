using BCrypt.Net;
using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConsignmentGenie.Application.Services;

public class AuthService : IAuthService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly ISlugService _slugService;

    public AuthService(ConsignmentGenieContext context, IConfiguration configuration, ILogger<AuthService> logger, ISlugService slugService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _slugService = slugService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToStringValue(), user.OrganizationId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization.Name,
            ExpiresAt = expiresAt
        };
    }

    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return null;
        }

        // 🏗️ AGGREGATE ROOT PATTERN: Create organization aggregate root
        var organization = new Organization
        {
            Name = request.OrganizationName,
            VerticalType = request.VerticalType,
            CachedSubscriptionStatus = "trialing",
            Slug = await _slugService.GenerateUniqueOrganizationSlugAsync(request.OrganizationName)
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // 🏗️ AGGREGATE ROOT PATTERN: Create user entity associated with organization
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Owner,
            OrganizationId = organization.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate token and return response
        var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToStringValue(), user.OrganizationId);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role,
            OrganizationId = user.OrganizationId,
            OrganizationName = organization.Name,
            ExpiresAt = expiresAt
        };
    }

    public string GenerateJwtToken(Guid userId, string email, string role, Guid organizationId)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? "ConsignmentGenie_Super_Secret_Key_2024_32_Characters_Long!";
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role), // Current role format (could be "owner" or "Owner")
            new("organizationId", organizationId.ToString())
        };

        // COMPATIBILITY: Add both uppercase and lowercase role claims for migration
        // This ensures both old controllers expecting "Owner" and new ones expecting "owner" work
        if (role.Equals("owner", StringComparison.OrdinalIgnoreCase))
        {
            if (role == "owner")
                claims.Add(new(ClaimTypes.Role, "Owner")); // Add uppercase version
            else if (role == "Owner")
                claims.Add(new(ClaimTypes.Role, "owner")); // Add lowercase version
        }
        else if (role.Equals("consignor", StringComparison.OrdinalIgnoreCase))
        {
            if (role == "consignor")
                claims.Add(new(ClaimTypes.Role, "Consignor"));
            else if (role == "Consignor")
                claims.Add(new(ClaimTypes.Role, "consignor"));
        }
        // Add similar logic for other roles as needed

        // Add ConsignorId and approval status claims if the user is a Consignor
        if (role.Equals("consignor", StringComparison.OrdinalIgnoreCase))
        {
            var consignor = _context.Consignors.FirstOrDefault(c => c.UserId == userId);
            if (consignor != null)
            {
                claims.Add(new("consignorId", consignor.Id.ToString()));
                bool isApproved = consignor.ApprovalStatus == "Approved";
                claims.Add(new("isApproved", isApproved.ToString().ToLower()));
                _logger.LogInformation("Added consignor claims: ConsignorId={ConsignorId}, IsApproved={IsApproved}, ApprovalStatus={ApprovalStatus}",
                    consignor.Id, isApproved, consignor.ApprovalStatus);
            }
        }

        // Add founder status claims for owners
        if (role.Equals("owner", StringComparison.OrdinalIgnoreCase))
        {
            var organization = _context.Organizations.FirstOrDefault(o => o.Id == organizationId);
            if (organization != null)
            {
                claims.Add(new("isFounder", organization.IsFounderPricing.ToString().ToLower()));
                if (organization.IsFounderPricing && organization.FounderTier.HasValue)
                {
                    claims.Add(new("founderTier", organization.FounderTier.Value.ToString()));
                }
                _logger.LogInformation("Added founder claims: IsFounder={IsFounder}, FounderTier={FounderTier}",
                    organization.IsFounderPricing, organization.FounderTier);
            }
        }

        _logger.LogInformation("Generating JWT token with claims: UserId={UserId}, Email={Email}, Role={Role}, OrganizationId={OrganizationId}",
            userId, email, role, organizationId);

        foreach (var claim in claims)
        {
            _logger.LogInformation("JWT Claim: Type={Type}, Value={Value}", claim.Type, claim.Value);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = jwtSettings["Issuer"] ?? "ConsignmentGenieAPI",
            Audience = jwtSettings["Audience"] ?? "ConsignmentGenieClient",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}