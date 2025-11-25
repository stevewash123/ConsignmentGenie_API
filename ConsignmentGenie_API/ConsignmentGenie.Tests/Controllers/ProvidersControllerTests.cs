using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;

using ConsignmentGenie.Application.DTOs;

namespace ConsignmentGenie.Tests.Controllers
{

    public class ProvidersControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<ILogger<ProvidersController>> _mockLogger;
        private readonly ProvidersController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _providerId = Guid.NewGuid();

        public ProvidersControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<ProvidersController>>();
            _controller = new ProvidersController(_context, _mockLogger.Object);

            // Setup user claims
            var claims = new List<Claim>
            {
                new("organizationId", _organizationId.ToString()),
                new("userId", _userId.ToString()),
                new(ClaimTypes.Role, "Owner")
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
            // Add organization
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Organization",
                Slug = "test-org",
                CreatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add test user
            var user = new User
            {
                Id = _userId,
                Email = "owner@test.com",
                OrganizationId = _organizationId,
                Role = UserRole.Owner,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);

            // Add providers
            var provider1 = new Provider
            {
                Id = _providerId,
                OrganizationId = _organizationId,
                ProviderNumber = "PROV001",
                DisplayName = "John Doe",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "555-123-4567",
                CommissionRate = 0.60m,
                Status = ProviderStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            var provider2 = new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProviderNumber = "PROV002",
                DisplayName = "Jane Smith",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                CommissionRate = 0.70m,
                Status = ProviderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            _context.Providers.AddRange(provider1, provider2);

            // Add some items for provider metrics
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProviderId = _providerId,
                Sku = "ITEM001",
                Title = "Test Item 1",
                Price = 100.00m,
                Status = ItemStatus.Available,
                Condition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            var item2 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProviderId = _providerId,
                Sku = "ITEM002",
                Title = "Test Item 2",
                Price = 80.00m,
                Status = ItemStatus.Sold,
                Condition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            _context.Items.AddRange(item1, item2);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesSuccessfully()
        {
            // Arrange & Act
            var controller = new ProvidersController(_context, _mockLogger.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task GetProviders_ReturnsPagedResults()
        {
            // Arrange
            var queryParams = new ProviderQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetProviders(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ProviderListDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.Count);
        }

        [Fact]
        public async Task GetProviders_WithSearchFilter_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = new ProviderQueryParams
            {
                Page = 1,
                PageSize = 10,
                Search = "John"
            };

            // Act
            var result = await _controller.GetProviders(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ProviderListDto>>(okResult.Value);
            Assert.Equal(1, pagedResult.TotalCount);
            Assert.Single(pagedResult.Items);
            Assert.Contains("John", pagedResult.Items.First().FullName);
        }

        [Fact]
        public async Task GetProviders_WithStatusFilter_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = new ProviderQueryParams
            {
                Page = 1,
                PageSize = 10,
                Status = "Active"
            };

            // Act
            var result = await _controller.GetProviders(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ProviderListDto>>(okResult.Value);
            Assert.Equal(1, pagedResult.TotalCount);
            Assert.Single(pagedResult.Items);
            Assert.Equal("Active", pagedResult.Items.First().Status);
        }

        [Fact]
        public async Task GetProvider_WithValidId_ReturnsProvider()
        {
            // Act
            var result = await _controller.GetProvider(_providerId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProviderDetailDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(_providerId, apiResponse.Data.ProviderId);
            Assert.Equal("John", apiResponse.Data.FirstName);
            Assert.Equal("Doe", apiResponse.Data.LastName);
        }

        [Fact]
        public async Task GetProvider_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _controller.GetProvider(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProviderDetailDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Provider not found", apiResponse.Errors);
        }

        [Fact]
        public async Task CreateProvider_WithValidData_CreatesSuccessfully()
        {
            // Arrange
            var request = new CreateProviderRequest
            {
                FirstName = "Alice",
                LastName = "Johnson",
                Email = "alice@example.com",
                Phone = "555-987-6543",
                CommissionRate = 0.65m,
                AddressLine1 = "123 Main St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                PreferredPaymentMethod = "Direct Deposit",
                Notes = "Test provider"
            };

            // Act
            var result = await _controller.CreateProvider(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProviderDetailDto>>(createdAtResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Alice", apiResponse.Data.FirstName);
            Assert.Equal("Johnson", apiResponse.Data.LastName);
            Assert.Equal("alice@example.com", apiResponse.Data.Email);

            // Verify provider was created in database
            var providerInDb = await _context.Providers.FindAsync(apiResponse.Data.ProviderId);
            Assert.NotNull(providerInDb);
            Assert.Equal("Alice", providerInDb.FirstName);
        }

        [Fact]
        public async Task CreateProvider_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateProviderRequest
            {
                FirstName = "Test",
                LastName = "Provider",
                Email = "john@example.com", // Email already exists
                CommissionRate = 0.50m
            };

            // Act
            var result = await _controller.CreateProvider(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProviderDetailDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("A provider with this email already exists", apiResponse.Errors);
        }

        [Fact]
        public async Task CreateProvider_WithInvalidContractDates_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateProviderRequest
            {
                FirstName = "Test",
                LastName = "Provider",
                Email = "test@example.com",
                CommissionRate = 0.50m,
                ContractStartDate = DateTime.UtcNow,
                ContractEndDate = DateTime.UtcNow.AddDays(-1) // End date before start date
            };

            // Act
            var result = await _controller.CreateProvider(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProviderDetailDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Contract end date must be after start date", apiResponse.Errors);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}