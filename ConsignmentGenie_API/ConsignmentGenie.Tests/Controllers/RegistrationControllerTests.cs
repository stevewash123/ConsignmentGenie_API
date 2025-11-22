using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class RegistrationControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly RegistrationController _controller;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public RegistrationControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _controller = new RegistrationController(_context);

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Add test organization with store code
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Consignment Shop",
                StoreCode = "1234",
                StoreCodeEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add disabled store code organization
            var disabledOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Disabled Shop",
                StoreCode = "9999",
                StoreCodeEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(disabledOrg);

            // Add existing user for duplicate email testing
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "existing@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Owner,
                OrganizationId = _organizationId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task ValidateStoreCode_WithValidCode_ReturnsValid()
        {
            // Act
            var result = await _controller.ValidateStoreCode("1234");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var validationResult = Assert.IsType<StoreCodeValidationDto>(okResult.Value);

            Assert.True(validationResult.IsValid);
            Assert.Equal("Test Consignment Shop", validationResult.ShopName);
            Assert.Null(validationResult.ErrorMessage);
        }

        [Fact]
        public async Task ValidateStoreCode_WithInvalidCode_ReturnsInvalid()
        {
            // Act
            var result = await _controller.ValidateStoreCode("INVALID");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var validationResult = Assert.IsType<StoreCodeValidationDto>(okResult.Value);

            Assert.False(validationResult.IsValid);
            Assert.Equal("Invalid store code", validationResult.ErrorMessage);
            Assert.Null(validationResult.ShopName);
        }

        [Fact]
        public async Task ValidateStoreCode_WithDisabledCode_ReturnsInvalid()
        {
            // Act
            var result = await _controller.ValidateStoreCode("9999");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var validationResult = Assert.IsType<StoreCodeValidationDto>(okResult.Value);

            Assert.False(validationResult.IsValid);
            Assert.Equal("Invalid store code", validationResult.ErrorMessage);
            Assert.Null(validationResult.ShopName);
        }

        [Fact]
        public async Task RegisterOwner_WithValidData_CreatesOwnerSuccessfully()
        {
            // Arrange
            var registerRequest = new RegisterOwnerRequest
            {
                FullName = "New Owner",
                Email = "newowner@test.com",
                Password = "SecurePassword123!",
                ShopName = "New Test Shop",
                Phone = "555-123-4567"
            };

            // Act
            var result = await _controller.RegisterOwner(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var registrationResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(registrationResult.Success);
            Assert.Contains("Registration successful", registrationResult.Message);

            // Verify user was created in database
            var createdUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "newowner@test.com");

            Assert.NotNull(createdUser);
            Assert.Equal("New Owner", createdUser.FullName);
            Assert.Equal(UserRole.Owner, createdUser.Role);
            Assert.Equal(ApprovalStatus.Pending, createdUser.ApprovalStatus);
            Assert.True(BCrypt.Net.BCrypt.Verify("SecurePassword123!", createdUser.PasswordHash));

            // Verify organization was created
            var createdOrganization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == createdUser.OrganizationId);

            Assert.NotNull(createdOrganization);
            Assert.Equal("New Test Shop", createdOrganization.Name);
            Assert.NotNull(createdOrganization.StoreCode);
            Assert.Matches(@"^\d{4}$", createdOrganization.StoreCode); // 4-digit store code
        }

        [Fact]
        public async Task RegisterOwner_WithDuplicateEmail_ReturnsError()
        {
            // Arrange
            var registerRequest = new RegisterOwnerRequest
            {
                FullName = "Duplicate User",
                Email = "existing@test.com", // This email already exists
                Password = "SecurePassword123!",
                ShopName = "Duplicate Shop"
            };

            // Act
            var result = await _controller.RegisterOwner(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var registrationResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.False(registrationResult.Success);
            Assert.Equal("Email already registered", registrationResult.Message);
            Assert.Contains("An account with this email already exists", registrationResult.Errors);

            // Verify no new user was created
            var userCount = await _context.Users.CountAsync(u => u.Email == "existing@test.com");
            Assert.Equal(1, userCount); // Should still be just the one we seeded
        }

        [Fact]
        public async Task RegisterProvider_WithValidData_CreatesProviderSuccessfully()
        {
            // Arrange
            var registerRequest = new RegisterProviderRequest
            {
                StoreCode = "1234",
                FullName = "New Provider",
                Email = "newprovider@test.com",
                Phone = "555-987-6543",
                PaymentDetails = "@newprovider"
            };

            // Act
            var result = await _controller.RegisterProvider(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var registrationResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(registrationResult.Success);
            Assert.Contains("Registration successful", registrationResult.Message);

            // Verify user was created
            var createdUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "newprovider@test.com");

            Assert.NotNull(createdUser);
            Assert.Equal("New Provider", createdUser.FullName);
            Assert.Equal(UserRole.Provider, createdUser.Role);
            Assert.Equal(ApprovalStatus.Pending, createdUser.ApprovalStatus);
            Assert.Equal(_organizationId, createdUser.OrganizationId);

            // Verify provider record was created
            var createdProvider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == createdUser.Id);

            Assert.NotNull(createdProvider);
            Assert.Equal("New Provider", createdProvider.DisplayName);
            Assert.Equal("newprovider@test.com", createdProvider.Email);
            Assert.Equal("@newprovider", createdProvider.PaymentDetails);
            Assert.Equal(ProviderStatus.Pending, createdProvider.Status);
        }

        [Fact]
        public async Task RegisterProvider_WithInvalidStoreCode_ReturnsError()
        {
            // Arrange
            var registerRequest = new RegisterProviderRequest
            {
                StoreCode = "INVALID",
                FullName = "Provider Name",
                Email = "provider@test.com"
            };

            // Act
            var result = await _controller.RegisterProvider(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var registrationResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.False(registrationResult.Success);
            Assert.Equal("Invalid store code", registrationResult.Message);
            Assert.Contains("Store code not found or registration is disabled", registrationResult.Errors);

            // Verify no user was created
            var userExists = await _context.Users.AnyAsync(u => u.Email == "provider@test.com");
            Assert.False(userExists);
        }

        [Fact]
        public async Task RegisterProvider_WithDisabledStoreCode_ReturnsError()
        {
            // Arrange
            var registerRequest = new RegisterProviderRequest
            {
                StoreCode = "9999", // This store code is disabled
                FullName = "Provider Name",
                Email = "provider@test.com"
            };

            // Act
            var result = await _controller.RegisterProvider(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var registrationResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.False(registrationResult.Success);
            Assert.Equal("Invalid store code", registrationResult.Message);
        }

        [Fact]
        public async Task RegisterProvider_WithDuplicateEmail_ReturnsError()
        {
            // Arrange
            var registerRequest = new RegisterProviderRequest
            {
                StoreCode = "1234",
                FullName = "Duplicate Provider",
                Email = "existing@test.com" // This email already exists
            };

            // Act
            var result = await _controller.RegisterProvider(registerRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var registrationResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.False(registrationResult.Success);
            Assert.Equal("Email already registered", registrationResult.Message);
            Assert.Contains("An account with this email already exists", registrationResult.Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("12")]
        [InlineData("12345")]
        public async Task ValidateStoreCode_WithVariousInvalidFormats_ReturnsInvalid(string invalidCode)
        {
            // Act
            var result = await _controller.ValidateStoreCode(invalidCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var validationResult = Assert.IsType<StoreCodeValidationDto>(okResult.Value);

            Assert.False(validationResult.IsValid);
            Assert.Equal("Invalid store code", validationResult.ErrorMessage);
        }

        [Fact]
        public void Controller_ShouldHaveCorrectRouteAndAttributes()
        {
            // Arrange & Act
            var controllerType = typeof(RegistrationController);
            var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);
            var apiControllerAttributes = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);

            // Assert
            Assert.NotEmpty(routeAttributes);
            Assert.NotEmpty(apiControllerAttributes);

            var routeAttribute = (RouteAttribute)routeAttributes[0];
            Assert.Equal("api/auth", routeAttribute.Template);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}