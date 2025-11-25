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
    public class ShopControllerTests
    {
        private readonly Mock<IStoreCodeService> _mockStoreCodeService;
        private readonly ShopController _controller;

        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public ShopControllerTests()
        {
            _mockStoreCodeService = new Mock<IStoreCodeService>();
            _controller = new ShopController(_mockStoreCodeService.Object);

            // Mock the user context
            var claims = new List<Claim>
            {
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
        public async Task GetStoreCode_ReturnsStoreCodeDto()
        {
            // Arrange
            var expectedStoreCode = new StoreCodeDto
            {
                StoreCode = "1234",
                IsEnabled = true,
                LastRegenerated = DateTime.UtcNow.AddDays(-1)
            };

            _mockStoreCodeService
                .Setup(s => s.GetStoreCodeAsync(_organizationId))
                .ReturnsAsync(expectedStoreCode);

            // Act
            var result = await _controller.GetStoreCode();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResult = Assert.IsType<StoreCodeDto>(okResult.Value);

            Assert.Equal(expectedStoreCode.StoreCode, actualResult.StoreCode);
            Assert.Equal(expectedStoreCode.IsEnabled, actualResult.IsEnabled);
            Assert.Equal(expectedStoreCode.LastRegenerated, actualResult.LastRegenerated);

            _mockStoreCodeService.Verify(s => s.GetStoreCodeAsync(_organizationId), Times.Once);
        }

        [Fact]
        public async Task GetStoreCode_WithNonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            _mockStoreCodeService
                .Setup(s => s.GetStoreCodeAsync(_organizationId))
                .ThrowsAsync(new ArgumentException("Organization not found"));

            // Act
            var result = await _controller.GetStoreCode();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = notFoundResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Organization not found", messageProperty.GetValue(response));

            _mockStoreCodeService.Verify(s => s.GetStoreCodeAsync(_organizationId), Times.Once);
        }

        [Fact]
        public async Task RegenerateStoreCode_ReturnsNewStoreCode()
        {
            // Arrange
            var newStoreCode = new StoreCodeDto
            {
                StoreCode = "5678",
                IsEnabled = true,
                LastRegenerated = DateTime.UtcNow
            };

            _mockStoreCodeService
                .Setup(s => s.RegenerateStoreCodeAsync(_organizationId))
                .ReturnsAsync(newStoreCode);

            // Act
            var result = await _controller.RegenerateStoreCode();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResult = Assert.IsType<StoreCodeDto>(okResult.Value);

            Assert.Equal(newStoreCode.StoreCode, actualResult.StoreCode);
            Assert.Equal(newStoreCode.IsEnabled, actualResult.IsEnabled);
            Assert.Equal(newStoreCode.LastRegenerated, actualResult.LastRegenerated);

            _mockStoreCodeService.Verify(s => s.RegenerateStoreCodeAsync(_organizationId), Times.Once);
        }

        [Fact]
        public async Task RegenerateStoreCode_WithNonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            _mockStoreCodeService
                .Setup(s => s.RegenerateStoreCodeAsync(_organizationId))
                .ThrowsAsync(new ArgumentException("Organization not found"));

            // Act
            var result = await _controller.RegenerateStoreCode();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = notFoundResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Organization not found", messageProperty.GetValue(response));

            _mockStoreCodeService.Verify(s => s.RegenerateStoreCodeAsync(_organizationId), Times.Once);
        }

        [Theory]
        [InlineData(true, "enabled")]
        [InlineData(false, "disabled")]
        public async Task ToggleStoreCode_WithValidRequest_ReturnsSuccess(bool enabled, string expectedStatus)
        {
            // Arrange
            var request = new ToggleStoreCodeRequest { Enabled = enabled };

            _mockStoreCodeService
                .Setup(s => s.ToggleStoreCodeAsync(_organizationId, enabled))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ToggleStoreCode(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(response)?.ToString();
            Assert.Contains(expectedStatus, message);

            _mockStoreCodeService.Verify(s => s.ToggleStoreCodeAsync(_organizationId, enabled), Times.Once);
        }

        [Fact]
        public async Task ToggleStoreCode_WithNonExistentOrganization_ReturnsNotFound()
        {
            // Arrange
            var request = new ToggleStoreCodeRequest { Enabled = true };

            _mockStoreCodeService
                .Setup(s => s.ToggleStoreCodeAsync(_organizationId, true))
                .ThrowsAsync(new ArgumentException("Organization not found"));

            // Act
            var result = await _controller.ToggleStoreCode(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value;

            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Organization not found", messageProperty.GetValue(response));

            _mockStoreCodeService.Verify(s => s.ToggleStoreCodeAsync(_organizationId, true), Times.Once);
        }

        [Fact]
        public async Task GetStoreCode_WithMissingOrganizationId_ReturnsUnauthorized()
        {
            // Arrange - Create controller with no OrganizationId claim
            var claimsWithoutOrgId = new List<Claim>();
            var identity = new ClaimsIdentity(claimsWithoutOrgId, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            var controllerWithoutOrgId = new ShopController(_mockStoreCodeService.Object);
            controllerWithoutOrgId.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await controllerWithoutOrgId.GetStoreCode();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Organization ID not found in token", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetStoreCode_WithInternalError_ReturnsInternalServerError()
        {
            // Arrange
            _mockStoreCodeService
                .Setup(s => s.GetStoreCodeAsync(_organizationId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetStoreCode();

            // Assert
            var serverErrorResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, serverErrorResult.StatusCode);

            var response = serverErrorResult.Value;
            Assert.NotNull(response);
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("An error occurred while retrieving store code", messageProperty.GetValue(response));

            var errorProperty = response.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Database connection failed", errorProperty.GetValue(response));

            _mockStoreCodeService.Verify(s => s.GetStoreCodeAsync(_organizationId), Times.Once);
        }

        [Fact]
        public void Controller_ShouldHaveCorrectRouteAndAttributes()
        {
            // Arrange & Act
            var controllerType = typeof(ShopController);
            var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);
            var apiControllerAttributes = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);

            // Assert
            Assert.NotEmpty(routeAttributes);
            Assert.NotEmpty(apiControllerAttributes);

            var routeAttribute = (RouteAttribute)routeAttributes[0];
            Assert.Equal("api/shop", routeAttribute.Template);
        }
    }
}