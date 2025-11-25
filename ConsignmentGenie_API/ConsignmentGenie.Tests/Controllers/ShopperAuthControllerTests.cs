using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Application.Services.Interfaces;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ShopperAuthControllerTests
    {
        private readonly Mock<IShopperAuthService> _mockShopperAuthService;
        private readonly Mock<ILogger<ShopperAuthController>> _mockLogger;
        private readonly ShopperAuthController _controller;

        public ShopperAuthControllerTests()
        {
            _mockShopperAuthService = new Mock<IShopperAuthService>();
            _mockLogger = new Mock<ILogger<ShopperAuthController>>();

            _controller = new ShopperAuthController(
                _mockShopperAuthService.Object,
                _mockLogger.Object
            );
        }

        #region Register Tests

        [Fact]
        public async Task Register_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new ShopperRegisterRequest
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Password = "Password123!",
                Phone = "555-123-4567"
            };

            var expectedAuthResult = new AuthResultDto
            {
                Success = true,
                Token = "jwt-token",
                Profile = new ShopperProfileDto
                {
                    ShopperId = Guid.NewGuid(),
                    FullName = "John Doe",
                    Email = "john@example.com"
                }
            };

            _mockShopperAuthService.Setup(s => s.RegisterAsync(request, storeSlug))
                .ReturnsAsync(expectedAuthResult);

            // Act
            var result = await _controller.Register(storeSlug, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResultDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("jwt-token", apiResponse.Data.Token);
        }

        [Fact]
        public async Task Register_ServiceReturnsFailed_ReturnsBadRequest()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new ShopperRegisterRequest
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Password = "Password123!"
            };

            var failedAuthResult = new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Email already exists"
            };

            _mockShopperAuthService.Setup(s => s.RegisterAsync(request, storeSlug))
                .ReturnsAsync(failedAuthResult);

            // Act
            var result = await _controller.Register(storeSlug, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResultDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Email already exists", apiResponse.Errors);
        }

        [Fact]
        public async Task Register_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new ShopperRegisterRequest
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Password = "Password123!"
            };

            _mockShopperAuthService.Setup(s => s.RegisterAsync(request, storeSlug))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Register(storeSlug, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var apiResponse = Assert.IsType<ApiResponse<AuthResultDto>>(statusCodeResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("An error occurred during registration", apiResponse.Errors);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new ShopperLoginRequest
            {
                Email = "john@example.com",
                Password = "Password123!"
            };

            var expectedAuthResult = new AuthResultDto
            {
                Success = true,
                Token = "jwt-token",
                Profile = new ShopperProfileDto
                {
                    ShopperId = Guid.NewGuid(),
                    FullName = "John Doe",
                    Email = "john@example.com"
                }
            };

            _mockShopperAuthService.Setup(s => s.LoginAsync(request, storeSlug))
                .ReturnsAsync(expectedAuthResult);

            // Act
            var result = await _controller.Login(storeSlug, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResultDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("jwt-token", apiResponse.Data.Token);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsBadRequest()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new ShopperLoginRequest
            {
                Email = "john@example.com",
                Password = "WrongPassword"
            };

            var failedAuthResult = new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            };

            _mockShopperAuthService.Setup(s => s.LoginAsync(request, storeSlug))
                .ReturnsAsync(failedAuthResult);

            // Act
            var result = await _controller.Login(storeSlug, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResultDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Invalid email or password", apiResponse.Errors);
        }

        #endregion

        #region Guest Session Tests

        [Fact]
        public async Task CreateGuestSession_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new GuestSessionRequest
            {
                Email = "guest@example.com"
            };

            var expectedGuestSession = new GuestSessionDto
            {
                SessionToken = "guest-token",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _mockShopperAuthService.Setup(s => s.CreateGuestSessionAsync(request, storeSlug))
                .ReturnsAsync(expectedGuestSession);

            // Act
            var result = await _controller.CreateGuestSession(storeSlug, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<GuestSessionDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("guest-token", apiResponse.Data.SessionToken);
        }

        [Fact]
        public async Task CreateGuestSession_InvalidStore_ReturnsBadRequest()
        {
            // Arrange
            var storeSlug = "invalid-store";
            var request = new GuestSessionRequest
            {
                Email = "guest@example.com"
            };

            _mockShopperAuthService.Setup(s => s.CreateGuestSessionAsync(request, storeSlug))
                .ThrowsAsync(new ArgumentException("Store not found"));

            // Act
            var result = await _controller.CreateGuestSession(storeSlug, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<GuestSessionDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Store not found", apiResponse.Errors);
        }

        #endregion

        #region Password Reset Tests

        [Fact]
        public async Task ForgotPassword_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new ForgotPasswordRequest
            {
                Email = "john@example.com"
            };

            // Act
            var result = await _controller.ForgotPassword(storeSlug, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PasswordResetResponseDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Password reset email sent if account exists", apiResponse.Data.Message);
        }

        [Fact]
        public async Task ResetPassword_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";
            var request = new ResetPasswordRequest
            {
                Email = "john@example.com",
                NewPassword = "NewPassword123!"
            };

            // Act
            var result = await _controller.ResetPassword(storeSlug, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PasswordResetResponseDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Password reset successfully", apiResponse.Data.Message);
        }

        #endregion
    }
}