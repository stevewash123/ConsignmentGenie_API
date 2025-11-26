using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IRegistrationService _registrationService;
    private readonly IOwnerInvitationService _ownerInvitationService;

    public AdminController(
        ConsignmentGenieContext context,
        ILogger<AdminController> logger,
        IRegistrationService registrationService,
        IOwnerInvitationService ownerInvitationService)
    {
        _context = context;
        _logger = logger;
        _registrationService = registrationService;
        _ownerInvitationService = ownerInvitationService;
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
    public async Task<ActionResult<ApiResponse<ReseedResponseDto>>> ReseedDatabase()
    {
        try
        {
            _logger.LogInformation("Starting database reseed...");

            // Verify database connection
            if (!await _context.Database.CanConnectAsync())
            {
                return StatusCode(500, ApiResponse<ReseedResponseDto>.ErrorResult("Cannot connect to database"));
            }

            // Clear existing data in dependency order
            _logger.LogInformation("Clearing existing data...");

            // Clear dependent data first
            _context.OrderItems.RemoveRange(_context.OrderItems);
            _context.Orders.RemoveRange(_context.Orders);
            _context.CartItems.RemoveRange(_context.CartItems);
            _context.ShoppingCarts.RemoveRange(_context.ShoppingCarts);
            _context.GuestCheckouts.RemoveRange(_context.GuestCheckouts);
            _context.Shoppers.RemoveRange(_context.Shoppers);
            _context.Customers.RemoveRange(_context.Customers);
            _context.Statements.RemoveRange(_context.Statements);
            _context.Notifications.RemoveRange(_context.Notifications);
            _context.UserNotificationPreferences.RemoveRange(_context.UserNotificationPreferences);
            _context.NotificationPreferences.RemoveRange(_context.NotificationPreferences);
            _context.Suggestions.RemoveRange(_context.Suggestions);
            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            _context.ItemTagAssignments.RemoveRange(_context.ItemTagAssignments);
            _context.ItemTags.RemoveRange(_context.ItemTags);
            _context.ItemCategories.RemoveRange(_context.ItemCategories);
            _context.PaymentGatewayConnections.RemoveRange(_context.PaymentGatewayConnections);
            _context.SquareSyncLogs.RemoveRange(_context.SquareSyncLogs);
            _context.SquareConnections.RemoveRange(_context.SquareConnections);
            _context.SubscriptionEvents.RemoveRange(_context.SubscriptionEvents);
            _context.ItemImages.RemoveRange(_context.ItemImages);
            _context.Categories.RemoveRange(_context.Categories);
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

            var response = new ReseedResponseDto
            {
                Message = "Database reseeded successfully with demo data and Cypress test data",
                Timestamp = DateTime.UtcNow,
                TestAccounts = new[]
                {
                    new TestAccountDto { Email = "admin@demoshop.com", Role = "Owner", Password = "password123", Store = "demo-shop" },
                    new TestAccountDto { Email = "owner@demoshop.com", Role = "Owner", Password = "password123", Store = "demo-shop" },
                    new TestAccountDto { Email = "provider@demoshop.com", Role = "Provider", Password = "password123", Store = "demo-shop" },
                    new TestAccountDto { Email = "customer@demoshop.com", Role = "Customer", Password = "password123", Store = "demo-shop" }
                },
                CypressTestData = new
                {
                    store = new { name = "Cypress Test Store", slug = "test-store", taxRate = 0.085m },
                    testAccounts = new[]
                    {
                        new { email = "cypress.shopper@example.com", role = "Customer", password = "password123" },
                        new { email = "cypress.guest@example.com", role = "Customer", password = "password123" }
                    },
                    testItems = new[]
                    {
                        new { id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", title = "Cypress Test Electronics Item", price = 25.99m, category = "Electronics" },
                        new { id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", title = "Cypress Test Clothing Item", price = 45.50m, category = "Clothing" }
                    }
                }
            };
            return Ok(ApiResponse<ReseedResponseDto>.SuccessResult(response, "Reseed completed with Cypress test data"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reseed database: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<ReseedResponseDto>.ErrorResult($"Failed to reseed database: {ex.Message}"));
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

        // Cypress test data IDs
        var testOrgId = new Guid("77777777-7777-7777-7777-777777777777");
        var testUserId1 = new Guid("88888888-8888-8888-8888-888888888888");
        var testUserId2 = new Guid("99999999-9999-9999-9999-999999999999");
        var testItemId1 = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var testItemId2 = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");

        // Create Demo Organization
        var organization = new ConsignmentGenie.Core.Entities.Organization
        {
            Id = orgId,
            Name = "Demo Consignment Shop",
            Slug = "demo-shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create Cypress Test Organization
        var testOrganization = new ConsignmentGenie.Core.Entities.Organization
        {
            Id = testOrgId,
            Name = "Cypress Test Store",
            Slug = "test-store",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.AddRange(organization, testOrganization);

        // Create Demo Users
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

        // Create Cypress Test Users
        var testUsers = new[]
        {
            new ConsignmentGenie.Core.Entities.User
            {
                Id = testUserId1,
                Email = "cypress.shopper@example.com",
                PasswordHash = hashedPassword,
                Role = ConsignmentGenie.Core.Enums.UserRole.Customer,
                OrganizationId = testOrgId,
                FullName = "Cypress Test Shopper",
                Phone = "555-123-4567",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConsignmentGenie.Core.Entities.User
            {
                Id = testUserId2,
                Email = "cypress.guest@example.com",
                PasswordHash = hashedPassword,
                Role = ConsignmentGenie.Core.Enums.UserRole.Customer,
                OrganizationId = testOrgId,
                FullName = "Cypress Guest User",
                Phone = "555-987-6543",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Users.AddRange(users);
        _context.Users.AddRange(testUsers);

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

        // Create Test Provider for Cypress test items
        var testProviderId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var testProvider = new ConsignmentGenie.Core.Entities.Provider
        {
            Id = testProviderId,
            UserId = testUserId1, // Link to first test user
            OrganizationId = testOrgId,
            DisplayName = "Cypress Test Provider",
            Email = "cypress.shopper@example.com",
            Phone = "555-123-4567",
            Address = "123 Test St, Cypress City, CC 12345",
            CommissionRate = 50.0m,
            PaymentMethod = "Check",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Providers.AddRange(provider, testProvider);

        // Create Test Items for Cypress tests
        var testItems = new[]
        {
            new ConsignmentGenie.Core.Entities.Item
            {
                Id = testItemId1,
                OrganizationId = testOrgId,
                ProviderId = testProviderId, // Use the test provider
                Sku = "CY-ELEC-001",
                Title = "Cypress Test Electronics Item",
                Description = "A test electronic item for Cypress tests",
                Price = 25.99m,
                Status = ItemStatus.Available,
                Category = "Electronics",
                Brand = "TestBrand",
                Condition = ItemCondition.Good,
                Size = "Medium",
                Color = "Black",
                ReceivedDate = DateOnly.FromDateTime(DateTime.Now),
                ListedDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConsignmentGenie.Core.Entities.Item
            {
                Id = testItemId2,
                OrganizationId = testOrgId,
                ProviderId = testProviderId, // Use the test provider
                Sku = "CY-CLOTH-001",
                Title = "Cypress Test Clothing Item",
                Description = "A test clothing item for Cypress tests",
                Price = 45.50m,
                Status = ItemStatus.Available,
                Category = "Clothing",
                Brand = "TestFashion",
                Condition = ItemCondition.LikeNew,
                Size = "Large",
                Color = "Blue",
                ReceivedDate = DateOnly.FromDateTime(DateTime.Now),
                ListedDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Items.AddRange(testItems);

        await _context.SaveChangesAsync();
    }

    // Owner Approval Endpoints
    [HttpGet("pending-owners")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<PendingOwnerDto>>>> GetPendingOwners()
    {
        try
        {
            var pendingOwners = await _registrationService.GetPendingOwnersAsync();
            return Ok(ApiResponse<List<PendingOwnerDto>>.SuccessResult(pendingOwners));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<List<PendingOwnerDto>>.ErrorResult($"An error occurred while retrieving pending owners: {ex.Message}"));
        }
    }

    [HttpPost("{userId}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ApprovalResponseDto>>> ApproveOwner(Guid userId)
    {
        try
        {
            var approvedByUserId = GetCurrentUserId();
            await _registrationService.ApproveOwnerAsync(userId, approvedByUserId);
            var response = new ApprovalResponseDto { Message = "Owner approved successfully" };
            return Ok(ApiResponse<ApprovalResponseDto>.SuccessResult(response, "Owner approved successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<ApprovalResponseDto>.ErrorResult(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ApprovalResponseDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ApprovalResponseDto>.ErrorResult($"An error occurred while approving owner: {ex.Message}"));
        }
    }

    [HttpPost("{userId}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ApprovalResponseDto>>> RejectOwner(Guid userId, [FromBody] RejectUserRequest request)
    {
        try
        {
            var rejectedByUserId = GetCurrentUserId();
            await _registrationService.RejectOwnerAsync(userId, rejectedByUserId, request.Reason);
            var response = new ApprovalResponseDto { Message = "Owner rejected successfully" };
            return Ok(ApiResponse<ApprovalResponseDto>.SuccessResult(response, "Owner rejected successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<ApprovalResponseDto>.ErrorResult(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ApprovalResponseDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ApprovalResponseDto>.ErrorResult($"An error occurred while rejecting owner: {ex.Message}"));
        }
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

    #region Owner Invitations

    /// <summary>
    /// Invite a new shop owner
    /// </summary>
    [HttpPost("invitations/owner")]
    public async Task<ActionResult<ApiResponse<OwnerInvitationDetailDto>>> InviteOwner(
        [FromBody] CreateOwnerInvitationRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _ownerInvitationService.CreateInvitationAsync(request, currentUserId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<OwnerInvitationDetailDto>.ErrorResult(result.Message));
            }

            return Ok(ApiResponse<OwnerInvitationDetailDto>.SuccessResult(
                result.Data,
                "Owner invitation sent successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner invitation for {Email}", request.Email);
            return StatusCode(500, ApiResponse<OwnerInvitationDetailDto>.ErrorResult("An error occurred while creating the invitation"));
        }
    }

    /// <summary>
    /// Get all owner invitations with pagination and filtering
    /// </summary>
    [HttpGet("invitations/owner")]
    public async Task<ActionResult<ApiResponse<ConsignmentGenie.Core.DTOs.PagedResult<OwnerInvitationListDto>>>> GetOwnerInvitations(
        [FromQuery] OwnerInvitationQueryParams queryParams)
    {
        try
        {
            var result = await _ownerInvitationService.GetInvitationsAsync(queryParams);

            return Ok(ApiResponse<ConsignmentGenie.Core.DTOs.PagedResult<OwnerInvitationListDto>>.SuccessResult(
                result,
                $"Retrieved {result.Data.Count} owner invitations"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving owner invitations");
            return StatusCode(500, ApiResponse<ConsignmentGenie.Core.DTOs.PagedResult<OwnerInvitationListDto>>.ErrorResult("An error occurred while retrieving invitations"));
        }
    }

    /// <summary>
    /// Get owner invitation metrics and statistics
    /// </summary>
    [HttpGet("invitations/owner/metrics")]
    public async Task<ActionResult<ApiResponse<OwnerInvitationMetricsDto>>> GetOwnerInvitationMetrics()
    {
        try
        {
            var metrics = await _ownerInvitationService.GetMetricsAsync();

            return Ok(ApiResponse<OwnerInvitationMetricsDto>.SuccessResult(
                metrics,
                "Owner invitation metrics retrieved successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving owner invitation metrics");
            return StatusCode(500, ApiResponse<OwnerInvitationMetricsDto>.ErrorResult("An error occurred while retrieving metrics"));
        }
    }

    /// <summary>
    /// Get owner invitation details by ID
    /// </summary>
    [HttpGet("invitations/owner/{invitationId}")]
    public async Task<ActionResult<ApiResponse<OwnerInvitationDetailDto>>> GetOwnerInvitation(Guid invitationId)
    {
        try
        {
            var invitation = await _ownerInvitationService.GetInvitationByIdAsync(invitationId);

            if (invitation == null)
            {
                return NotFound(ApiResponse<OwnerInvitationDetailDto>.ErrorResult("Invitation not found"));
            }

            return Ok(ApiResponse<OwnerInvitationDetailDto>.SuccessResult(
                invitation,
                "Owner invitation retrieved successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving owner invitation {InvitationId}", invitationId);
            return StatusCode(500, ApiResponse<OwnerInvitationDetailDto>.ErrorResult("An error occurred while retrieving the invitation"));
        }
    }

    /// <summary>
    /// Cancel an owner invitation
    /// </summary>
    [HttpPost("invitations/owner/{invitationId}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelOwnerInvitation(Guid invitationId)
    {
        try
        {
            var result = await _ownerInvitationService.CancelInvitationAsync(invitationId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult(result.Message));
            }

            return Ok(ApiResponse<bool>.SuccessResult(
                true,
                "Owner invitation cancelled successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling owner invitation {InvitationId}", invitationId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while cancelling the invitation"));
        }
    }

    /// <summary>
    /// Resend an owner invitation
    /// </summary>
    [HttpPost("invitations/owner/{invitationId}/resend")]
    public async Task<ActionResult<ApiResponse<bool>>> ResendOwnerInvitation(Guid invitationId)
    {
        try
        {
            var result = await _ownerInvitationService.ResendInvitationAsync(invitationId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult(result.Message));
            }

            return Ok(ApiResponse<bool>.SuccessResult(
                true,
                "Owner invitation resent successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending owner invitation {InvitationId}", invitationId);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while resending the invitation"));
        }
    }

    #endregion

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