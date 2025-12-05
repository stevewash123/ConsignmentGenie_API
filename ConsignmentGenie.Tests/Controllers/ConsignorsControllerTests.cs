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
using ConsignmentGenie.Application.Services.Interfaces;

namespace ConsignmentGenie.Tests.Controllers
{

    public class ConsignorsControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<ILogger<ConsignorsController>> _mockLogger;
        private readonly Mock<IConsignorInvitationService> _mockInvitationService;
        private readonly ConsignorsController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _consignorId = Guid.NewGuid();

        public ConsignorsControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<ConsignorsController>>();
            _mockInvitationService = new Mock<IConsignorInvitationService>();
            _controller = new ConsignorsController(_context, _mockLogger.Object, _mockInvitationService.Object);

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

            // Add consignors
            var consignor1 = new Consignor
            {
                Id = _consignorId,
                OrganizationId = _organizationId,
                ConsignorNumber = "PROV001",
                DisplayName = "John Doe",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "555-123-4567",
                CommissionRate = 0.60m,
                Status = ConsignorStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            var consignor2 = new Consignor
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorNumber = "PROV002",
                DisplayName = "Jane Smith",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                CommissionRate = 0.70m,
                Status = ConsignorStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            _context.Consignors.AddRange(consignor1, consignor2);

            // Add some items for consignor metrics
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _consignorId,
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
                ConsignorId = _consignorId,
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
            var controller = new ConsignorsController(_context, _mockLogger.Object, _mockInvitationService.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task GetConsignors_ReturnsPagedResults()
        {
            // Arrange
            var queryParams = new ConsignorQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetConsignors(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ConsignorListDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.Count);
        }

        [Fact]
        public async Task GetConsignors_WithSearchFilter_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = new ConsignorQueryParams
            {
                Page = 1,
                PageSize = 10,
                Search = "John"
            };

            // Act
            var result = await _controller.GetConsignors(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ConsignorListDto>>(okResult.Value);
            Assert.Equal(1, pagedResult.TotalCount);
            Assert.Single(pagedResult.Items);
            Assert.Contains("John", pagedResult.Items.First().FullName);
        }

        [Fact]
        public async Task GetConsignors_WithStatusFilter_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = new ConsignorQueryParams
            {
                Page = 1,
                PageSize = 10,
                Status = "Active"
            };

            // Act
            var result = await _controller.GetConsignors(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ConsignorListDto>>(okResult.Value);
            Assert.Equal(1, pagedResult.TotalCount);
            Assert.Single(pagedResult.Items);
            Assert.Equal("Active", pagedResult.Items.First().Status);
        }

        [Fact]
        public async Task GetConsignor_WithValidId_ReturnsConsignor()
        {
            // Act
            var result = await _controller.GetConsignor(_consignorId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ConsignorDetailDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(_consignorId, apiResponse.Data.ConsignorId);
            Assert.Equal("John", apiResponse.Data.FirstName);
            Assert.Equal("Doe", apiResponse.Data.LastName);
        }

        [Fact]
        public async Task GetConsignor_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _controller.GetConsignor(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ConsignorDetailDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Consignor not found", apiResponse.Errors);
        }

        [Fact]
        public async Task CreateConsignor_WithValidData_CreatesSuccessfully()
        {
            // Arrange
            var request = new CreateConsignorRequest
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
                Notes = "Test consignor"
            };

            // Act
            var result = await _controller.CreateConsignor(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ConsignorDetailDto>>(createdAtResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Alice", apiResponse.Data.FirstName);
            Assert.Equal("Johnson", apiResponse.Data.LastName);
            Assert.Equal("alice@example.com", apiResponse.Data.Email);

            // Verify consignor was created in database
            var consignorInDb = await _context.Consignors.FindAsync(apiResponse.Data.ConsignorId);
            Assert.NotNull(consignorInDb);
            Assert.Equal("Alice", consignorInDb.FirstName);
        }

        [Fact]
        public async Task CreateConsignor_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateConsignorRequest
            {
                FirstName = "Test",
                LastName = "Consignor",
                Email = "john@example.com", // Email already exists
                CommissionRate = 0.50m
            };

            // Act
            var result = await _controller.CreateConsignor(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ConsignorDetailDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("A consignor with this email already exists", apiResponse.Errors);
        }

        [Fact]
        public async Task CreateConsignor_WithInvalidContractDates_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateConsignorRequest
            {
                FirstName = "Test",
                LastName = "Consignor",
                Email = "test@example.com",
                CommissionRate = 0.50m,
                ContractStartDate = DateTime.UtcNow,
                ContractEndDate = DateTime.UtcNow.AddDays(-1) // End date before start date
            };

            // Act
            var result = await _controller.CreateConsignor(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ConsignorDetailDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Contract end date must be after start date", apiResponse.Errors);
        }

        #region Invitation Tests

        [Fact]
        public async Task GetPendingInvitations_ReturnsSuccessfully()
        {
            // Arrange
            var invitations = new List<ConsignmentGenie.Application.DTOs.Consignor.ConsignorInvitationDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "John Invited",
                    Email = "john.invited@example.com",
                    Status = ConsignmentGenie.Core.Entities.InvitationStatus.Pending,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    InvitedByEmail = "owner@test.com"
                }
            };

            _mockInvitationService.Setup(x => x.GetPendingInvitationsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(invitations);

            // Act
            var result = await _controller.GetPendingInvitations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedInvitations = Assert.IsType<List<ConsignmentGenie.Application.DTOs.Consignor.ConsignorInvitationDto>>(okResult.Value);
            Assert.Single(returnedInvitations);
            Assert.Equal("John Invited", returnedInvitations[0].Name);
            Assert.Equal("john.invited@example.com", returnedInvitations[0].Email);
        }

        [Fact]
        public async Task CreateInvitation_WithValidData_ReturnsSuccessfully()
        {
            // Arrange
            var request = new ConsignmentGenie.Application.DTOs.Consignor.CreateConsignorInvitationDto
            {
                Name = "Jane Invited",
                Email = "jane.invited@example.com"
            };

            var resultDto = new ConsignmentGenie.Application.DTOs.Consignor.ConsignorInvitationResultDto
            {
                Success = true,
                Message = "Invitation sent successfully"
            };

            _mockInvitationService.Setup(x => x.CreateInvitationAsync(
                    It.IsAny<ConsignmentGenie.Application.DTOs.Consignor.CreateConsignorInvitationDto>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .ReturnsAsync(resultDto);

            // Act
            var result = await _controller.CreateInvitation(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<ConsignmentGenie.Application.DTOs.Consignor.ConsignorInvitationResultDto>(okResult.Value);
            Assert.True(returnedResult.Success);
            Assert.Equal("Invitation sent successfully", returnedResult.Message);
        }

        [Fact]
        public async Task CreateInvitation_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var request = new ConsignmentGenie.Application.DTOs.Consignor.CreateConsignorInvitationDto
            {
                Name = "Jane Invited",
                Email = "jane.invited@example.com"
            };

            var resultDto = new ConsignmentGenie.Application.DTOs.Consignor.ConsignorInvitationResultDto
            {
                Success = false,
                Message = "A user with this email already exists in the system."
            };

            _mockInvitationService.Setup(x => x.CreateInvitationAsync(
                    It.IsAny<ConsignmentGenie.Application.DTOs.Consignor.CreateConsignorInvitationDto>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .ReturnsAsync(resultDto);

            // Act
            var result = await _controller.CreateInvitation(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var returnedResult = Assert.IsType<ConsignmentGenie.Application.DTOs.Consignor.ConsignorInvitationResultDto>(badRequestResult.Value);
            Assert.False(returnedResult.Success);
            Assert.Contains("A user with this email already exists", returnedResult.Message);
        }

        [Fact]
        public async Task CancelInvitation_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var invitationId = Guid.NewGuid();
            _mockInvitationService.Setup(x => x.CancelInvitationAsync(invitationId, It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CancelInvitation(invitationId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task CancelInvitation_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invitationId = Guid.NewGuid();
            _mockInvitationService.Setup(x => x.CancelInvitationAsync(invitationId, It.IsAny<Guid>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CancelInvitation(invitationId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Invitation not found or cannot be cancelled.", notFoundResult.Value);
        }

        [Fact]
        public async Task ResendInvitation_WithValidId_ReturnsOk()
        {
            // Arrange
            var invitationId = Guid.NewGuid();
            _mockInvitationService.Setup(x => x.ResendInvitationAsync(invitationId, It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ResendInvitation(invitationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var message = okResult.Value;
            Assert.NotNull(message);
        }

        [Fact]
        public async Task ResendInvitation_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invitationId = Guid.NewGuid();
            _mockInvitationService.Setup(x => x.ResendInvitationAsync(invitationId, It.IsAny<Guid>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ResendInvitation(invitationId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Invitation not found or cannot be resent.", notFoundResult.Value);
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}