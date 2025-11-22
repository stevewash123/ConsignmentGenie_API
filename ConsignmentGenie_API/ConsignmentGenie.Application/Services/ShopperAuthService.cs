using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ConsignmentGenie.Application.Services;

public class ShopperAuthService : IShopperAuthService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;

    public ShopperAuthService(ConsignmentGenieContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResultDto> RegisterAsync(ShopperRegisterRequest request, string storeSlug)
    {
        try
        {
            // Find organization by slug
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == storeSlug);

            if (organization == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Store not found."
                };
            }

            // Check if shopper already exists for this store
            var existingShopper = await _context.Shoppers
                .AnyAsync(s => s.OrganizationId == organization.Id && s.Email.ToLower() == request.Email.ToLower());

            if (existingShopper)
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "An account with this email already exists for this store."
                };
            }

            // Check if user already exists globally
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.OrganizationId == organization.Id);

            User user;
            if (existingUser != null)
            {
                // User exists for this organization, use existing user
                user = existingUser;
            }
            else
            {
                // Create new user
                user = new User
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.Customer, // Using Customer role as per the enum
                    OrganizationId = organization.Id,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    ApprovalStatus = ApprovalStatus.Approved // Shoppers don't require approval
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Create shopper record
            var shopper = new Shopper
            {
                OrganizationId = organization.Id,
                UserId = user.Id,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                EmailNotifications = request.EmailNotifications
            };

            _context.Shoppers.Add(shopper);
            await _context.SaveChangesAsync();

            // Update last login
            shopper.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate token
            var token = GenerateShopperJwtToken(user.Id, shopper.Id, user.Email, organization.Id, storeSlug);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Create profile DTO
            var profile = new ShopperProfileDto
            {
                ShopperId = shopper.Id,
                FullName = shopper.FullName,
                Email = shopper.Email,
                Phone = shopper.Phone,
                EmailNotifications = shopper.EmailNotifications,
                MemberSince = shopper.CreatedAt,
                ShippingAddress = new AddressDto
                {
                    Address1 = shopper.ShippingAddress1,
                    Address2 = shopper.ShippingAddress2,
                    City = shopper.ShippingCity,
                    State = shopper.ShippingState,
                    Zip = shopper.ShippingZip
                }
            };

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                ExpiresAt = expiresAt,
                Profile = profile
            };
        }
        catch (Exception)
        {
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = "An error occurred during registration."
            };
        }
    }

    public async Task<AuthResultDto> LoginAsync(ShopperLoginRequest request, string storeSlug)
    {
        try
        {
            // Find organization by slug
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == storeSlug);

            if (organization == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Store not found."
                };
            }

            // Find shopper
            var shopper = await _context.Shoppers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.OrganizationId == organization.Id &&
                                          s.Email.ToLower() == request.Email.ToLower());

            if (shopper == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password."
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, shopper.User.PasswordHash))
            {
                return new AuthResultDto
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password."
                };
            }

            // Update last login
            shopper.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate token
            var token = GenerateShopperJwtToken(shopper.User.Id, shopper.Id, shopper.Email, organization.Id, storeSlug);
            var expiresAt = DateTime.UtcNow.AddHours(request.RememberMe ? 24 * 30 : 24); // 30 days if remember me

            // Create profile DTO
            var profile = new ShopperProfileDto
            {
                ShopperId = shopper.Id,
                FullName = shopper.FullName,
                Email = shopper.Email,
                Phone = shopper.Phone,
                EmailNotifications = shopper.EmailNotifications,
                MemberSince = shopper.CreatedAt,
                ShippingAddress = new AddressDto
                {
                    Address1 = shopper.ShippingAddress1,
                    Address2 = shopper.ShippingAddress2,
                    City = shopper.ShippingCity,
                    State = shopper.ShippingState,
                    Zip = shopper.ShippingZip
                }
            };

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                ExpiresAt = expiresAt,
                Profile = profile
            };
        }
        catch (Exception)
        {
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = "An error occurred during login."
            };
        }
    }

    public async Task<GuestSessionDto> CreateGuestSessionAsync(GuestSessionRequest request, string storeSlug)
    {
        // Find organization by slug
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Slug == storeSlug);

        if (organization == null)
        {
            throw new ArgumentException("Store not found.", nameof(storeSlug));
        }

        // Generate session token
        var sessionToken = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Create guest checkout record
        var guestCheckout = new GuestCheckout
        {
            OrganizationId = organization.Id,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            SessionToken = sessionToken,
            ExpiresAt = expiresAt
        };

        _context.GuestCheckouts.Add(guestCheckout);
        await _context.SaveChangesAsync();

        return new GuestSessionDto
        {
            SessionToken = sessionToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task<ShopperProfileDto?> GetShopperProfileAsync(Guid userId, Guid organizationId)
    {
        var shopper = await _context.Shoppers
            .FirstOrDefaultAsync(s => s.UserId == userId && s.OrganizationId == organizationId);

        if (shopper == null)
            return null;

        return new ShopperProfileDto
        {
            ShopperId = shopper.Id,
            FullName = shopper.FullName,
            Email = shopper.Email,
            Phone = shopper.Phone,
            EmailNotifications = shopper.EmailNotifications,
            MemberSince = shopper.CreatedAt,
            ShippingAddress = new AddressDto
            {
                Address1 = shopper.ShippingAddress1,
                Address2 = shopper.ShippingAddress2,
                City = shopper.ShippingCity,
                State = shopper.ShippingState,
                Zip = shopper.ShippingZip
            }
        };
    }

    public async Task<ShopperProfileDto?> UpdateShopperProfileAsync(Guid userId, Guid organizationId, UpdateShopperProfileRequest request)
    {
        var shopper = await _context.Shoppers
            .FirstOrDefaultAsync(s => s.UserId == userId && s.OrganizationId == organizationId);

        if (shopper == null)
            return null;

        // Update shopper fields
        shopper.FullName = request.FullName;
        shopper.Phone = request.Phone;
        shopper.EmailNotifications = request.EmailNotifications;

        if (request.ShippingAddress != null)
        {
            shopper.ShippingAddress1 = request.ShippingAddress.Address1;
            shopper.ShippingAddress2 = request.ShippingAddress.Address2;
            shopper.ShippingCity = request.ShippingAddress.City;
            shopper.ShippingState = request.ShippingAddress.State;
            shopper.ShippingZip = request.ShippingAddress.Zip;
        }

        await _context.SaveChangesAsync();

        return new ShopperProfileDto
        {
            ShopperId = shopper.Id,
            FullName = shopper.FullName,
            Email = shopper.Email,
            Phone = shopper.Phone,
            EmailNotifications = shopper.EmailNotifications,
            MemberSince = shopper.CreatedAt,
            ShippingAddress = new AddressDto
            {
                Address1 = shopper.ShippingAddress1,
                Address2 = shopper.ShippingAddress2,
                City = shopper.ShippingCity,
                State = shopper.ShippingState,
                Zip = shopper.ShippingZip
            }
        };
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return false;

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return true;
    }

    public string GenerateShopperJwtToken(Guid userId, Guid shopperId, string email, Guid organizationId, string storeSlug)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? "ConsignmentGenie_Super_Secret_Key_2024_32_Characters_Long!";
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, UserRole.Customer.ToString()), // Using Customer role for shoppers
            new("ShopperId", shopperId.ToString()),
            new("OrganizationId", organizationId.ToString()),
            new("StoreSlug", storeSlug)
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

    private static string GenerateSecureToken()
    {
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        randomNumberGenerator.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
    }
}