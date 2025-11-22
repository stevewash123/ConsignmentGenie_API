using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/auth")]
public class RegistrationController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;

    public RegistrationController(ConsignmentGenieContext context)
    {
        _context = context;
    }

    [HttpGet("validate-store-code/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<StoreCodeValidationDto>> ValidateStoreCode(string code)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.StoreCode == code && o.StoreCodeEnabled);

        if (organization == null)
        {
            return Ok(new StoreCodeValidationDto
            {
                IsValid = false,
                ErrorMessage = "Invalid store code"
            });
        }

        return Ok(new StoreCodeValidationDto
        {
            IsValid = true,
            ShopName = organization.Name
        });
    }

    [HttpPost("register/owner")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResultDto>> RegisterOwner([FromBody] RegisterOwnerRequest request)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Ok(new RegistrationResultDto
            {
                Success = false,
                Message = "Email already registered",
                Errors = new List<string> { "An account with this email already exists" }
            });
        }

        try
        {
            // Generate store code
            var storeCode = await GenerateUniqueStoreCode();

            // Create organization
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = request.ShopName,
                StoreCode = storeCode,
                StoreCodeEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);

            // Create user (pending approval)
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = BC.HashPassword(request.Password),
                Role = UserRole.Owner,
                OrganizationId = organization.Id,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new RegistrationResultDto
            {
                Success = true,
                Message = "Registration successful! Your account is pending admin approval."
            });
        }
        catch (Exception ex)
        {
            return Ok(new RegistrationResultDto
            {
                Success = false,
                Message = "Registration failed",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("register/provider")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResultDto>> RegisterProvider([FromBody] RegisterProviderRequest request)
    {
        // Validate store code
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.StoreCode == request.StoreCode && o.StoreCodeEnabled);

        if (organization == null)
        {
            return Ok(new RegistrationResultDto
            {
                Success = false,
                Message = "Invalid store code",
                Errors = new List<string> { "Store code not found or registration is disabled" }
            });
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Ok(new RegistrationResultDto
            {
                Success = false,
                Message = "Email already registered",
                Errors = new List<string> { "An account with this email already exists" }
            });
        }

        try
        {
            // Create user (pending approval)
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = BC.HashPassword(request.Password),
                Role = UserRole.Provider,
                OrganizationId = organization.Id,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Create provider record
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = user.Id,
                DisplayName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                PaymentDetails = request.PaymentDetails,
                Status = ProviderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();

            return Ok(new RegistrationResultDto
            {
                Success = true,
                Message = $"Registration successful! Your request to join {organization.Name} is pending approval."
            });
        }
        catch (Exception ex)
        {
            return Ok(new RegistrationResultDto
            {
                Success = false,
                Message = "Registration failed",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private async Task<string> GenerateUniqueStoreCode()
    {
        var random = new Random();
        string code;

        do
        {
            code = random.Next(1000, 9999).ToString();
        }
        while (await _context.Organizations.AnyAsync(o => o.StoreCode == code));

        return code;
    }
}