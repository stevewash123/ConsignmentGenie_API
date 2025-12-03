using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IRegistrationService> _mockRegistrationService;
        private readonly UsersController _controller;

        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");
        private readonly Guid _userId = new("22222222-2222-2222-2222-222222222222");

        public UsersControllerTests()
        {
            _mockRegistrationService = new Mock<IRegistrationService>();
            _controller = new UsersController(_mockRegistrationService.Object);

            // Mock the user context
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _userId.ToString()),
                new("OrganizationId", _organizationId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetPendingApprovals_ReturnsListOfPendingApprovals()
        {
            // Arrange
            var pendingApprovals = new List<PendingApprovalDto>
            {
                new PendingApprovalDto
                {
                    UserId = Guid.NewGuid(),
                    FullName = "John Consignor",
                    Email = "john@provider.com",
                    Phone = "555-123-4567",
                    PreferredPaymentMethod = "Venmo",
                    PaymentDetails = "@johnprovider",
                    RequestedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _mockRegistrationService
                .Setup(s => s.GetPendingProvidersAsync(_organizationId))
                .ReturnsAsync(pendingApprovals);

            // Act
            var result = await _controller.GetPendingApprovals();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResult = Assert.IsAssignableFrom<List<PendingApprovalDto>>(okResult.Value);

            Assert.Single(actualResult);
            Assert.Equal("John Consignor", actualResult[0].FullName);
            Assert.Equal("john@provider.com", actualResult[0].Email);

            _mockRegistrationService.Verify(s => s.GetPendingProvidersAsync(_organizationId), Times.Once);
        }

        [Fact]
        public async Task GetPendingApprovalCount_ReturnsCount()
        {
            // Arrange
            _mockRegistrationService
                .Setup(s => s.GetPendingApprovalCountAsync(_organizationId))
                .ReturnsAsync(3);

            // Act
            var result = await _controller.GetPendingApprovalCount();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var count = Assert.IsType<int>(okResult.Value);

            Assert.Equal(3, count);

            _mockRegistrationService.Verify(s => s.GetPendingApprovalCountAsync(_organizationId), Times.Once);
        }

        [Fact]
        public async Task ApproveUser_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var userIdToApprove = Guid.NewGuid();

            _mockRegistrationService
                .Setup(s => s.ApproveUserAsync(userIdToApprove, _userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ApproveUser(userIdToApprove);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("User approved successfully", messageProperty.GetValue(response));

            _mockRegistrationService.Verify(s => s.ApproveUserAsync(userIdToApprove, _userId), Times.Once);
        }

        [Fact]
        public async Task ApproveUser_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var userIdToApprove = Guid.NewGuid();

            _mockRegistrationService
                .Setup(s => s.ApproveUserAsync(userIdToApprove, _userId))
                .ThrowsAsync(new ArgumentException("User not found"));

            // Act
            var result = await _controller.ApproveUser(userIdToApprove);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("User not found", messageProperty.GetValue(response));

            _mockRegistrationService.Verify(s => s.ApproveUserAsync(userIdToApprove, _userId), Times.Once);
        }

        [Fact]
        public async Task ApproveUser_WithAlreadyApprovedUser_ReturnsBadRequest()
        {
            // Arrange
            var userIdToApprove = Guid.NewGuid();

            _mockRegistrationService
                .Setup(s => s.ApproveUserAsync(userIdToApprove, _userId))
                .ThrowsAsync(new InvalidOperationException("User is not pending approval"));

            // Act
            var result = await _controller.ApproveUser(userIdToApprove);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("User is not pending approval", messageProperty.GetValue(response));

            _mockRegistrationService.Verify(s => s.ApproveUserAsync(userIdToApprove, _userId), Times.Once);
        }

        [Fact]
        public async Task RejectUser_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var userIdToReject = Guid.NewGuid();
            var rejectRequest = new RejectUserRequest
            {
                Reason = "Incomplete application"
            };

            _mockRegistrationService
                .Setup(s => s.RejectUserAsync(userIdToReject, _userId, rejectRequest.Reason))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RejectUser(userIdToReject, rejectRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("User rejected successfully", messageProperty.GetValue(response));

            _mockRegistrationService.Verify(s => s.RejectUserAsync(userIdToReject, _userId, rejectRequest.Reason), Times.Once);
        }

        [Fact]
        public async Task RejectUser_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var userIdToReject = Guid.NewGuid();
            var rejectRequest = new RejectUserRequest
            {
                Reason = "Test reason"
            };

            _mockRegistrationService
                .Setup(s => s.RejectUserAsync(userIdToReject, _userId, rejectRequest.Reason))
                .ThrowsAsync(new ArgumentException("User not found"));

            // Act
            var result = await _controller.RejectUser(userIdToReject, rejectRequest);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("User not found", messageProperty.GetValue(response));

            _mockRegistrationService.Verify(s => s.RejectUserAsync(userIdToReject, _userId, rejectRequest.Reason), Times.Once);
        }

        [Fact]
        public async Task GetPendingApprovals_WithMissingOrganizationId_ReturnsUnauthorized()
        {
            // Arrange - Create controller with no OrganizationId claim
            var claimsWithoutOrgId = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _userId.ToString())
            };
            var identity = new ClaimsIdentity(claimsWithoutOrgId, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            var controllerWithoutOrgId = new UsersController(_mockRegistrationService.Object);
            controllerWithoutOrgId.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await controllerWithoutOrgId.GetPendingApprovals();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Organization ID not found in token", unauthorizedResult.Value);
        }

        [Fact]
        public void Controller_ShouldHaveCorrectRouteAndAttributes()
        {
            // Arrange & Act
            var controllerType = typeof(UsersController);
            var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);
            var apiControllerAttributes = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);

            // Assert
            Assert.NotEmpty(routeAttributes);
            Assert.NotEmpty(apiControllerAttributes);

            var routeAttribute = (RouteAttribute)routeAttributes[0];
            Assert.Equal("api/users", routeAttribute.Template);
        }
    }
}