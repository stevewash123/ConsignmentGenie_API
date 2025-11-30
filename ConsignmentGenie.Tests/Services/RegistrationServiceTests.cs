using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services
{
    public class RegistrationServiceTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IStoreCodeService> _mockStoreCodeService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly RegistrationService _registrationService;

        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");
        private readonly Guid _userId = new("22222222-2222-2222-2222-222222222222");

        public RegistrationServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockEmailService = new Mock<IEmailService>();
            _mockStoreCodeService = new Mock<IStoreCodeService>();
            _mockAuthService = new Mock<IAuthService>();

            var mockLogger = new Mock<ILogger<RegistrationService>>();
            _registrationService = new RegistrationService(
                _context,
                _mockEmailService.Object,
                _mockStoreCodeService.Object,
                _mockAuthService.Object,
                mockLogger.Object);

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Add test organization
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Shop",
                ShopName = "Test Shop",
                StoreCode = "1234",
                StoreCodeEnabled = true,
                AutoApproveProviders = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add auto-approve organization
            var autoApproveOrg = new Organization
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Name = "Auto Approve Shop",
                ShopName = "Auto Approve Shop",
                StoreCode = "5678",
                StoreCodeEnabled = true,
                AutoApproveProviders = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(autoApproveOrg);

            // Add disabled organization
            var disabledOrg = new Organization
            {
                Id = new Guid("44444444-4444-4444-4444-444444444444"),
                Name = "Disabled Shop",
                StoreCode = "9999",
                StoreCodeEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(disabledOrg);

            // Add existing user
            var existingUser = new User
            {
                Id = _userId,
                Email = "existing@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Owner,
                OrganizationId = _organizationId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);

            // Add pending provider user
            var pendingProviderUser = new User
            {
                Id = new Guid("55555555-5555-5555-5555-555555555555"),
                Email = "pending.provider@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FullName = "Pending Provider",
                Role = UserRole.Provider,
                OrganizationId = _organizationId,
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(pendingProviderUser);

            // Add pending owner user
            var pendingOwnerUser = new User
            {
                Id = new Guid("66666666-6666-6666-6666-666666666666"),
                Email = "pending.owner@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FullName = "Pending Owner",
                Role = UserRole.Owner,
                OrganizationId = new Guid("77777777-7777-7777-7777-777777777777"),
                ApprovalStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var pendingOwnerOrg = new Organization
            {
                Id = new Guid("77777777-7777-7777-7777-777777777777"),
                Name = "Pending Shop",
                ShopName = "Pending Shop",
                StoreCode = "7777",
                StoreCodeEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(pendingOwnerOrg);
            _context.Users.Add(pendingOwnerUser);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task ValidateStoreCodeAsync_WithValidCode_ReturnsValid()
        {
            // Act
            var result = await _registrationService.ValidateStoreCodeAsync("1234");

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("Test Shop", result.ShopName);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateStoreCodeAsync_WithInvalidCode_ReturnsInvalid()
        {
            // Act
            var result = await _registrationService.ValidateStoreCodeAsync("INVALID");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Invalid or disabled store code", result.ErrorMessage);
            Assert.Null(result.ShopName);
        }

        [Fact]
        public async Task ValidateStoreCodeAsync_WithDisabledCode_ReturnsInvalid()
        {
            // Act
            var result = await _registrationService.ValidateStoreCodeAsync("9999");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Invalid or disabled store code", result.ErrorMessage);
            Assert.Null(result.ShopName);
        }

        [Fact]
        public async Task RegisterOwnerAsync_WithValidRequest_CreatesOwnerSuccessfully()
        {
            // Arrange
            _mockStoreCodeService.Setup(s => s.GenerateStoreCode()).Returns("4321");
            _mockEmailService.Setup(e => e.SendSimpleEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var request = new RegisterOwnerRequest
            {
                FullName = "New Owner",
                Email = "newowner@test.com",
                Password = "SecurePassword123!",
                ShopName = "New Shop",
                Phone = "555-123-4567"
            };

            // Act
            var result = await _registrationService.RegisterOwnerAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Account created successfully", result.Message);

            // Verify user was created
            var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newowner@test.com");
            Assert.NotNull(createdUser);
            Assert.Equal("New Owner", createdUser.FullName);
            Assert.Equal(UserRole.Owner, createdUser.Role);
            Assert.Equal(ApprovalStatus.Approved, createdUser.ApprovalStatus);
            Assert.NotNull(createdUser.ApprovedAt);

            // Verify organization was created
            var createdOrg = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == createdUser.OrganizationId);
            Assert.NotNull(createdOrg);
            Assert.Equal("New Shop", createdOrg.ShopName);
            Assert.Equal("4321", createdOrg.StoreCode);
            Assert.Equal("active", createdOrg.Status);

            // Verify email was sent
            _mockEmailService.Verify(e => e.SendWelcomeEmailAsync(
                "newowner@test.com",
                "New Shop",
                "New Owner",
                "4321"), Times.Once);
        }

        [Fact]
        public async Task RegisterOwnerAsync_WithDuplicateEmail_ReturnsError()
        {
            // Arrange
            var request = new RegisterOwnerRequest
            {
                FullName = "Duplicate User",
                Email = "existing@test.com",
                Password = "SecurePassword123!",
                ShopName = "Duplicate Shop"
            };

            // Act
            var result = await _registrationService.RegisterOwnerAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("email already exists", result.Message);
            Assert.Contains("Email already in use", result.Errors);
        }

        [Fact]
        public async Task RegisterProviderAsync_WithValidRequest_CreatesProviderSuccessfully()
        {
            // Arrange
            _mockEmailService.Setup(e => e.SendSimpleEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var request = new RegisterProviderRequest
            {
                StoreCode = "1234",
                FullName = "New Provider",
                Email = "newprovider@test.com",
                Password = "SecurePassword123!",
                Phone = "555-987-6543",
                PreferredPaymentMethod = "Venmo",
                PaymentDetails = "@newprovider"
            };

            // Act
            var result = await _registrationService.RegisterProviderAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Account created successfully", result.Message);

            // Verify user was created
            var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newprovider@test.com");
            Assert.NotNull(createdUser);
            Assert.Equal("New Provider", createdUser.FullName);
            Assert.Equal(UserRole.Provider, createdUser.Role);
            Assert.Equal(ApprovalStatus.Pending, createdUser.ApprovalStatus);
            Assert.Equal(_organizationId, createdUser.OrganizationId);

            // Verify email notifications were sent
            _mockEmailService.Verify(e => e.SendSimpleEmailAsync(
                "newprovider@test.com",
                "Welcome to ConsignmentGenie - Account Pending",
                It.IsAny<string>(),
                true), Times.Once);

            _mockEmailService.Verify(e => e.SendSimpleEmailAsync(
                "existing@test.com",
                "New Provider Request - New Provider",
                It.IsAny<string>(),
                true), Times.Once);
        }

        [Fact]
        public async Task RegisterProviderAsync_WithAutoApproveOrg_CreatesProviderAndApproves()
        {
            // Arrange
            _mockEmailService.Setup(e => e.SendSimpleEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            var request = new RegisterProviderRequest
            {
                StoreCode = "5678", // Auto-approve organization
                FullName = "Auto Provider",
                Email = "autoprovider@test.com",
                Password = "SecurePassword123!",
                PreferredPaymentMethod = "Check"
            };

            // Act
            var result = await _registrationService.RegisterProviderAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Account created and approved", result.Message);

            // Verify user was created and approved
            var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "autoprovider@test.com");
            Assert.NotNull(createdUser);
            Assert.Equal(ApprovalStatus.Approved, createdUser.ApprovalStatus);

            // Verify provider record was created
            var createdProvider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == createdUser.Id);
            Assert.NotNull(createdProvider);
            Assert.Equal(ProviderStatus.Active, createdProvider.Status);
            Assert.Equal("Approved", createdProvider.ApprovalStatus);
        }

        [Fact]
        public async Task RegisterProviderAsync_WithInvalidStoreCode_ReturnsError()
        {
            // Arrange
            var request = new RegisterProviderRequest
            {
                StoreCode = "INVALID",
                FullName = "Provider Name",
                Email = "provider@test.com",
                Password = "SecurePassword123!"
            };

            // Act
            var result = await _registrationService.RegisterProviderAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid or disabled store code", result.Message);
        }

        [Fact]
        public async Task GetPendingProvidersAsync_ReturnsPendingProviders()
        {
            // Act
            var result = await _registrationService.GetPendingProvidersAsync(_organizationId);

            // Assert
            Assert.Single(result);
            Assert.Equal("Pending Provider", result[0].FullName);
            Assert.Equal("pending.provider@test.com", result[0].Email);
        }

        [Fact]
        public async Task GetPendingApprovalCountAsync_ReturnsCorrectCount()
        {
            // Act
            var result = await _registrationService.GetPendingApprovalCountAsync(_organizationId);

            // Assert
            Assert.Equal(1, result); // One pending provider
        }

        [Fact]
        public async Task ApproveUserAsync_ApprovesUserAndCreatesProvider()
        {
            // Arrange
            var pendingUserId = new Guid("55555555-5555-5555-5555-555555555555");
            var approverUserId = _userId;

            _mockEmailService.Setup(e => e.SendSimpleEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            // Act
            await _registrationService.ApproveUserAsync(pendingUserId, approverUserId);

            // Assert
            var approvedUser = await _context.Users.FindAsync(pendingUserId);
            Assert.NotNull(approvedUser);
            Assert.Equal(ApprovalStatus.Approved, approvedUser.ApprovalStatus);
            Assert.Equal(approverUserId, approvedUser.ApprovedBy);
            Assert.NotNull(approvedUser.ApprovedAt);

            // Verify provider record was created
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == pendingUserId);
            Assert.NotNull(provider);
            Assert.Equal(ProviderStatus.Active, provider.Status);
            Assert.Equal("Approved", provider.ApprovalStatus);

            // Verify email was sent
            _mockEmailService.Verify(e => e.SendSimpleEmailAsync(
                "pending.provider@test.com",
                "Account Approved - You're In! 🎉",
                It.IsAny<string>(),
                true), Times.Once);
        }

        [Fact]
        public async Task RejectUserAsync_RejectsUserWithReason()
        {
            // Arrange
            var pendingUserId = new Guid("55555555-5555-5555-5555-555555555555");
            var rejectorUserId = _userId;
            var reason = "Incomplete application";

            _mockEmailService.Setup(e => e.SendSimpleEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            // Act
            await _registrationService.RejectUserAsync(pendingUserId, rejectorUserId, reason);

            // Assert
            var rejectedUser = await _context.Users.FindAsync(pendingUserId);
            Assert.NotNull(rejectedUser);
            Assert.Equal(ApprovalStatus.Rejected, rejectedUser.ApprovalStatus);
            Assert.Equal(reason, rejectedUser.RejectedReason);

            // Verify email was sent
            _mockEmailService.Verify(e => e.SendSimpleEmailAsync(
                "pending.provider@test.com",
                "Account Request Update",
                It.IsAny<string>(),
                true), Times.Once);
        }

        [Fact]
        public async Task GetPendingOwnersAsync_ReturnsPendingOwners()
        {
            // Act
            var result = await _registrationService.GetPendingOwnersAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("Pending Owner", result[0].FullName);
            Assert.Equal("pending.owner@test.com", result[0].Email);
            Assert.Equal("Pending Shop", result[0].ShopName);
        }

        [Fact]
        public async Task ApproveUserAsync_WithNonExistentUser_ThrowsException()
        {
            // Arrange
            var nonExistentUserId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _registrationService.ApproveUserAsync(nonExistentUserId, _userId));
        }

        [Fact]
        public async Task ApproveUserAsync_WithAlreadyApprovedUser_ThrowsException()
        {
            // Arrange - existing user is already approved

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _registrationService.ApproveUserAsync(_userId, _userId));
        }

        [Fact]
        public async Task RegisterOwnerAsync_WithValidRequest_GeneratesValidStoreCode()
        {
            // Arrange
            var request = new RegisterOwnerRequest
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Password = "SecurePassword123!",
                ShopName = "John's Shop",
                Subdomain = "johns-shop"
            };

            var testCode = "123AB4";

            _mockStoreCodeService
                .Setup(s => s.GenerateStoreCode())
                .Returns(testCode);

            _mockStoreCodeService
                .Setup(s => s.IsValidStoreCode(testCode))
                .Returns(true);

            _mockAuthService
                .Setup(s => s.GenerateJwtToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .Returns("test-token");

            // Act
            var result = await _registrationService.RegisterOwnerAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Token);

            var organization = await _context.Organizations
                .Where(o => o.Subdomain == request.Subdomain)
                .FirstAsync();

            Assert.Equal(testCode, organization.StoreCode);
            Assert.True(organization.StoreCodeEnabled);

            // Verify store code service was called
            _mockStoreCodeService.Verify(s => s.GenerateStoreCode(), Times.Once);
        }

        [Fact]
        public async Task RegisterOwnerAsync_WithStoreCodeGeneration_CreatesOrganizationAndUser()
        {
            // Arrange
            var request = new RegisterOwnerRequest
            {
                FullName = "Jane Smith",
                Email = "jane@example.com",
                Password = "SecurePassword123!",
                ShopName = "Jane's Shop",
                Subdomain = "janes-shop"
            };

            var testCode = "789XY4";

            _mockStoreCodeService
                .Setup(s => s.GenerateStoreCode())
                .Returns(testCode);

            _mockAuthService
                .Setup(s => s.GenerateJwtToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .Returns("test-token");

            // Act
            var result = await _registrationService.RegisterOwnerAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Token);

            // Verify organization was created correctly
            var organization = await _context.Organizations
                .Where(o => o.Subdomain == request.Subdomain)
                .FirstAsync();

            Assert.Equal(testCode, organization.StoreCode);
            Assert.True(organization.StoreCodeEnabled);
            Assert.Equal(request.ShopName, organization.Name);

            // Verify user was created and linked correctly
            var user = await _context.Users
                .Where(u => u.Email == request.Email)
                .FirstAsync();

            Assert.Equal(organization.Id, user.OrganizationId);
            Assert.Equal(request.FullName, user.FullName);
            Assert.Equal(UserRole.Owner, user.Role);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}