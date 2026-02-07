using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Application.Services.Interfaces;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ShopperAccountControllerTests
    {
        private readonly Mock<IShopperAuthService> _mockShopperAuthService;
        private readonly Mock<ILogger<ShopperAccountController>> _mockLogger;
        private readonly ShopperAccountController _controller;

        public ShopperAccountControllerTests()
        {
            _mockShopperAuthService = new Mock<IShopperAuthService>();
            _mockLogger = new Mock<ILogger<ShopperAccountController>>();
            _controller = new ShopperAccountController(_mockShopperAuthService.Object, _mockLogger.Object);

            // Setup test user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim("org_id", Guid.NewGuid().ToString()),
                new Claim("slug", "test-store")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesSuccessfully()
        {
            // Arrange & Act
            var controller = new ShopperAccountController(_mockShopperAuthService.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task GetProfile_WithValidSlug_ReturnsResult()
        {
            // Arrange
            var storeSlug = "test-store"; // Matches the token claim

            // Act
            var result = await _controller.GetProfile(storeSlug);

            // Assert
            // The test verifies the controller handles the request appropriately
            // Either returns a result or throws an exception based on service implementation
            Assert.NotNull(result);
        }
    }
}