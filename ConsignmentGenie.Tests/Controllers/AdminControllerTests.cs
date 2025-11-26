using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly AdminController _controller;
        private readonly Mock<ILogger<AdminController>> _loggerMock;
        private readonly IRegistrationService _registrationService;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");
        private readonly Guid _adminUserId = new("22222222-2222-2222-2222-222222222222");

        public AdminControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<AdminController>>();

            // Create mocks for RegistrationService dependencies
            var emailServiceMock = new Mock<IEmailService>();
            var storeCodeServiceMock = new Mock<IStoreCodeService>();

            // Create real RegistrationService with mocked dependencies
            _registrationService = new RegistrationService(_context, emailServiceMock.Object, storeCodeServiceMock.Object);

            var ownerInvitationServiceMock = new Mock<IOwnerInvitationService>();
            _controller = new AdminController(_context, _loggerMock.Object, _registrationService, ownerInvitationServiceMock.Object);

            // Setup admin user claims
            var claims = new List<Claim>
            {
                new("organizationId", _organizationId.ToString()),
                new("userId", _adminUserId.ToString()),
                new(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Add test organization
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Consignment Shop",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add admin user
            var adminUser = new User
            {
                Id = _adminUserId,
                Email = "admin@test.com",
                PasswordHash = "hashed_password",
                Role = UserRole.Owner, // Admin functionality uses Owner role currently
                OrganizationId = _organizationId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(adminUser);

            // Add pending owners for testing
            var pendingOrg1 = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Pending Shop 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var pendingOrg2 = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Pending Shop 2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.AddRange(pendingOrg1, pendingOrg2);

            var pendingUser1 = new User
            {
                Id = Guid.NewGuid(),
                Email = "pending1@test.com",
                PasswordHash = "hashed_password",
                FullName = "John Pending",
                Phone = "555-123-4567",
                Role = UserRole.Owner,
                OrganizationId = pendingOrg1.Id,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };

            var pendingUser2 = new User
            {
                Id = Guid.NewGuid(),
                Email = "pending2@test.com",
                PasswordHash = "hashed_password",
                FullName = "Jane Pending",
                Phone = "555-987-6543",
                Role = UserRole.Owner,
                OrganizationId = pendingOrg2.Id,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.AddRange(pendingUser1, pendingUser2);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetPendingOwners_ReturnsAllPendingOwners()
        {
            // Arrange
            var expectedPendingOwners = new List<PendingOwnerDto>
            {
                new() {
                    UserId = _context.Users.First(u => u.FullName == "John Pending").Id,
                    FullName = "John Pending",
                    Email = "pending1@test.com",
                    Phone = "555-123-4567",
                    ShopName = "Pending Shop 1",
                    RequestedAt = DateTime.UtcNow.AddDays(-2)
                },
                new() {
                    UserId = _context.Users.First(u => u.FullName == "Jane Pending").Id,
                    FullName = "Jane Pending",
                    Email = "pending2@test.com",
                    Phone = "555-987-6543",
                    ShopName = "Pending Shop 2",
                    RequestedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            // Real service will query database directly

            // Act
            var result = await _controller.GetPendingOwners();

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<List<PendingOwnerDto>>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<List<PendingOwnerDto>>>(okResult.Value);
            Assert.True(response.Success);
            var pendingOwners = response.Data;

            Assert.Equal(2, pendingOwners.Count);
            Assert.Contains(pendingOwners, p => p.FullName == "John Pending");
            Assert.Contains(pendingOwners, p => p.FullName == "Jane Pending");
            Assert.All(pendingOwners, p => Assert.NotNull(p.ShopName));
        }

        [Fact]
        public async Task GetPendingOwners_OrdersByRequestedDate()
        {
            // Arrange
            var expectedPendingOwners = new List<PendingOwnerDto>
            {
                new() {
                    UserId = _context.Users.First(u => u.FullName == "John Pending").Id,
                    FullName = "John Pending",
                    Email = "pending1@test.com",
                    Phone = "555-123-4567",
                    ShopName = "Pending Shop 1",
                    RequestedAt = DateTime.UtcNow.AddDays(-2)
                },
                new() {
                    UserId = _context.Users.First(u => u.FullName == "Jane Pending").Id,
                    FullName = "Jane Pending",
                    Email = "pending2@test.com",
                    Phone = "555-987-6543",
                    ShopName = "Pending Shop 2",
                    RequestedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            // Real service will query database directly

            // Act
            var result = await _controller.GetPendingOwners();

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<List<PendingOwnerDto>>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<List<PendingOwnerDto>>>(okResult.Value);
            Assert.True(response.Success);
            var pendingOwners = response.Data;

            // Should be ordered by RequestedAt ascending (oldest first)
            Assert.Equal("John Pending", pendingOwners[0].FullName); // Created 2 days ago
            Assert.Equal("Jane Pending", pendingOwners[1].FullName); // Created 1 day ago
        }

        [Fact]
        public async Task ApproveOwner_WithValidUser_ApprovesSuccessfully()
        {
            // Arrange
            var pendingUser = _context.Users.First(u => u.ApprovalStatus == ApprovalStatus.Pending);

            // Act
            var result = await _controller.ApproveOwner(pendingUser.Id);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            // Clear change tracker to force fresh query
            _context.ChangeTracker.Clear();

            // Verify user was approved
            var approvedUser = await _context.Users.FindAsync(pendingUser.Id);
            Assert.Equal(ApprovalStatus.Approved, approvedUser.ApprovalStatus);
            Assert.Equal(_adminUserId, approvedUser.ApprovedBy);
            Assert.NotNull(approvedUser.ApprovedAt);
            Assert.True(approvedUser.ApprovedAt > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public async Task ApproveOwner_GeneratesStoreCodeForOrganization()
        {
            // Arrange
            var pendingUser = _context.Users.First(u => u.ApprovalStatus == ApprovalStatus.Pending);
            var organization = await _context.Organizations.FindAsync(pendingUser.OrganizationId);
            Assert.True(string.IsNullOrEmpty(organization.StoreCode)); // Verify it starts without store code

            // Act
            var result = await _controller.ApproveOwner(pendingUser.Id);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ApprovalResponseDto>>(okResult.Value);
            Assert.True(response.Success);

            // Verify store code was generated
            var updatedOrganization = await _context.Organizations.FindAsync(pendingUser.OrganizationId);
            Assert.NotNull(updatedOrganization.StoreCode);
            Assert.Matches(@"^[A-Z0-9]{6}$", updatedOrganization.StoreCode); // Should be 6-character alphanumeric
        }

        [Fact]
        public async Task ApproveOwner_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();

            // Act
            var result = await _controller.ApproveOwner(nonExistentUserId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ApprovalResponseDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Contains("User not found", response.Errors);
        }

        [Fact]
        public async Task ApproveOwner_WithNonOwnerUser_ReturnsBadRequest()
        {
            // Arrange - Create a non-owner user
            var providerUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "provider@test.com",
                PasswordHash = "hashed_password",
                Role = UserRole.Provider, // Not an owner
                OrganizationId = _organizationId,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(providerUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ApproveOwner(providerUser.Id);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ApprovalResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("User is not an owner", response.Errors);
        }

        [Fact]
        public async Task ApproveOwner_WithAlreadyApprovedUser_ReturnsBadRequest()
        {
            // Arrange
            var pendingUser = _context.Users.First(u => u.ApprovalStatus == ApprovalStatus.Pending);
            pendingUser.ApprovalStatus = ApprovalStatus.Approved;
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ApproveOwner(pendingUser.Id);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ApprovalResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("User is not pending approval", response.Errors);
        }

        [Fact]
        public async Task RejectOwner_WithValidData_RejectsSuccessfully()
        {
            // Arrange
            var pendingUser = _context.Users.First(u => u.ApprovalStatus == ApprovalStatus.Pending);
            var rejectRequest = new RejectUserRequest { Reason = "Insufficient information provided" };

            // Act
            var result = await _controller.RejectOwner(pendingUser.Id, rejectRequest);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ApprovalResponseDto>>(okResult.Value);
            Assert.True(response.Success);

            // Verify user was rejected
            var rejectedUser = await _context.Users.FindAsync(pendingUser.Id);
            Assert.Equal(ApprovalStatus.Rejected, rejectedUser.ApprovalStatus);
            Assert.Equal("Insufficient information provided", rejectedUser.RejectedReason);
        }

        [Fact]
        public async Task RejectOwner_WithNullReason_RejectsWithoutReason()
        {
            // Arrange
            var pendingUser = _context.Users.First(u => u.ApprovalStatus == ApprovalStatus.Pending);
            var rejectRequest = new RejectUserRequest { Reason = null };

            // Act
            var result = await _controller.RejectOwner(pendingUser.Id, rejectRequest);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ApprovalResponseDto>>(okResult.Value);
            Assert.True(response.Success);

            // Verify user was rejected
            var rejectedUser = await _context.Users.FindAsync(pendingUser.Id);
            Assert.Equal(ApprovalStatus.Rejected, rejectedUser.ApprovalStatus);
            Assert.Null(rejectedUser.RejectedReason);
        }

        [Fact]
        public async Task RejectOwner_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();
            var rejectRequest = new RejectUserRequest { Reason = "Test reason" };

            // Act
            var result = await _controller.RejectOwner(nonExistentUserId, rejectRequest);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ApprovalResponseDto>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var response = Assert.IsType<ApiResponse<ApprovalResponseDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Contains("User not found", response.Errors);
        }

        [Fact]
        public async Task GenerateUniqueStoreCode_CreatesUniqueCodes()
        {
            // Arrange
            var mockRegistrationService = new Mock<IRegistrationService>();
            var mockOwnerInvitationService = new Mock<IOwnerInvitationService>();
            var controller = new AdminController(_context, _loggerMock.Object, mockRegistrationService.Object, mockOwnerInvitationService.Object);

            // Use reflection to access the private method
            var method = typeof(AdminController).GetMethod("GenerateUniqueStoreCode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task1 = (Task<string>)method.Invoke(controller, null);
            var code1 = await task1;

            var task2 = (Task<string>)method.Invoke(controller, null);
            var code2 = await task2;

            // Assert
            Assert.Matches(@"^\d{4}$", code1); // 4-digit number
            Assert.Matches(@"^\d{4}$", code2); // 4-digit number
            Assert.NotEqual(code1, code2); // Should be different
        }

        [Fact]
        public async Task Health_ReturnsHealthyStatus()
        {
            // Act
            var result = _controller.Health();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic response = okResult.Value;

            // Access nested properties - using correct case for property names
            var data = response.GetType().GetProperty("Data").GetValue(response, null);
            var status = data.GetType().GetProperty("status").GetValue(data, null);
            Assert.Equal("healthy", status);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}