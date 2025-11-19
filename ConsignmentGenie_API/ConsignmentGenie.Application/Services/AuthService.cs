using BCrypt.Net;
using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConsignmentGenie.Application.Services;

public class AuthService : IAuthService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ConsignmentGenieContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToString(), user.OrganizationId);
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
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return null;
        }

        // Create organization
        var organization = new Organization
        {
            Name = request.OrganizationName,
            VerticalType = request.VerticalType,
            SubscriptionStatus = SubscriptionStatus.Trial,
            SubscriptionTier = SubscriptionTier.Basic
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Create user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.ShopOwner,
            OrganizationId = organization.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate token and return response
        var token = GenerateJwtToken(user.Id, user.Email, user.Role.ToString(), user.OrganizationId);
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
            new(ClaimTypes.Role, role),
            new("OrganizationId", organizationId.ToString())
        };

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