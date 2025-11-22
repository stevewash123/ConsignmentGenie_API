using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.DTOs.Admin;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ConsignmentGenieContext context,
        ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.SuccessResult(new {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }, "API is running"));
    }

    [HttpPost("reseed")]
    public async Task<ActionResult<ApiResponse<object>>> ReseedDatabase()
    {
        try
        {
            _logger.LogInformation("Starting database reseed...");

            // Verify database connection
            if (!await _context.Database.CanConnectAsync())
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult("Cannot connect to database"));
            }

            // Clear existing data in dependency order
            _logger.LogInformation("Clearing existing data...");

            _context.Transactions.RemoveRange(_context.Transactions);
            _context.Payouts.RemoveRange(_context.Payouts);
            _context.Items.RemoveRange(_context.Items);
            _context.Providers.RemoveRange(_context.Providers);
            _context.Users.RemoveRange(_context.Users);
            _context.Organizations.RemoveRange(_context.Organizations);

            await _context.SaveChangesAsync();

            // Manually recreate the seeded data (since HasData only runs on migrations)
            _logger.LogInformation("Creating demo data...");

            await CreateDemoDataAsync();

            _logger.LogInformation("Database reseed completed successfully");

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                message = "Database reseeded successfully with demo data",
                timestamp = DateTime.UtcNow,
                testAccounts = new[]
                {
                    new { email = "admin@demoshop.com", role = "Owner", password = "password123" },
                    new { email = "owner@demoshop.com", role = "Owner", password = "password123" },
                    new { email = "provider@demoshop.com", role = "Provider", password = "password123" },
                    new { email = "customer@demoshop.com", role = "Customer", password = "password123" }
                }
            }, "Reseed completed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reseed database: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Failed to reseed database: {ex.Message}"));
        }
    }

    private async Task CreateDemoDataAsync()
    {
        // Use the same data from OnModelCreating in ConsignmentGenieContext
        var orgId = new Guid("11111111-1111-1111-1111-111111111111");
        var adminUserId = new Guid("22222222-2222-2222-2222-222222222222");
        var ownerUserId = new Guid("33333333-3333-3333-3333-333333333333");
        var providerUserId = new Guid("44444444-4444-4444-4444-444444444444");
        var customerUserId = new Guid("55555555-5555-5555-5555-555555555555");
        var providerId = new Guid("66666666-6666-6666-6666-666666666666");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");

        // Create Organization
        var organization = new ConsignmentGenie.Core.Entities.Organization
        {
            Id = orgId,
            Name = "Demo Consignment Shop",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(organization);

        // Create Users
        var users = new[]
        {
            new ConsignmentGenie.Core.Entities.User
            {
                Id = adminUserId,
                Email = "admin@demoshop.com",
                PasswordHash = hashedPassword,
                Role = ConsignmentGenie.Core.Enums.UserRole.Owner,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConsignmentGenie.Core.Entities.User
            {
                Id = ownerUserId,
                Email = "owner@demoshop.com",
                PasswordHash = hashedPassword,
                Role = ConsignmentGenie.Core.Enums.UserRole.Owner,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConsignmentGenie.Core.Entities.User
            {
                Id = providerUserId,
                Email = "provider@demoshop.com",
                PasswordHash = hashedPassword,
                Role = ConsignmentGenie.Core.Enums.UserRole.Provider,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConsignmentGenie.Core.Entities.User
            {
                Id = customerUserId,
                Email = "customer@demoshop.com",
                PasswordHash = hashedPassword,
                Role = ConsignmentGenie.Core.Enums.UserRole.Customer,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Users.AddRange(users);

        // Create Provider entity for the provider user
        var provider = new ConsignmentGenie.Core.Entities.Provider
        {
            Id = providerId,
            UserId = providerUserId,
            OrganizationId = orgId,
            DisplayName = "Demo Provider",
            Email = "provider@demoshop.com",
            Phone = "555-987-6543",
            Address = "456 Provider Ave, Demo City, DC 12345",
            CommissionRate = 60.0m,
            PaymentMethod = "Check",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Providers.Add(provider);

        await _context.SaveChangesAsync();
    }

    // Owner Approval Endpoints
    [HttpGet("pending-owners")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PendingOwnerDto>>> GetPendingOwners()
    {
        var pendingOwners = await _context.Users
            .Where(u => u.Role == UserRole.Owner && u.ApprovalStatus == ApprovalStatus.Pending)
            .Include(u => u.Organization)
            .Select(u => new PendingOwnerDto
            {
                UserId = u.Id,
                FullName = u.FullName ?? string.Empty,
                Email = u.Email,
                Phone = u.Phone,
                ShopName = u.Organization.Name,
                RequestedAt = u.CreatedAt
            })
            .OrderBy(p => p.RequestedAt)
            .ToListAsync();

        return Ok(pendingOwners);
    }

    [HttpPost("{userId}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ApproveOwner(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        if (user.Role != UserRole.Owner)
        {
            return BadRequest("User is not an owner");
        }

        if (user.ApprovalStatus != ApprovalStatus.Pending)
        {
            return BadRequest("User is not pending approval");
        }

        // Approve the user
        user.ApprovalStatus = ApprovalStatus.Approved;
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedBy = GetCurrentUserId();
        user.UpdatedAt = DateTime.UtcNow;

        // Generate store code for the organization if it doesn't have one
        if (string.IsNullOrEmpty(user.Organization.StoreCode))
        {
            user.Organization.StoreCode = await GenerateUniqueStoreCode();
            user.Organization.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{userId}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RejectOwner(Guid userId, [FromBody] RejectUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        if (user.Role != UserRole.Owner)
        {
            return BadRequest("User is not an owner");
        }

        if (user.ApprovalStatus != ApprovalStatus.Pending)
        {
            return BadRequest("User is not pending approval");
        }

        // Reject the user
        user.ApprovalStatus = ApprovalStatus.Rejected;
        user.RejectedReason = request.Reason;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok();
    }

    private Guid GetCurrentUserId()
    {
        // Get the current user's ID from the JWT token
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        // Fallback to admin user ID if not found
        return new Guid("22222222-2222-2222-2222-222222222222");
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