using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class RegistrationControllerTests
    {
        private readonly Mock<IRegistrationService> _mockRegistrationService;
        private readonly RegistrationController _controller;

        public RegistrationControllerTests()
        {
            _mockRegistrationService = new Mock<IRegistrationService>();
            var mockLogger = new Mock<ILogger<RegistrationController>>();
            _controller = new RegistrationController(_mockRegistrationService.Object, mockLogger.Object);
        }

        [Fact]
        public async Task ValidateStoreCode_WithValidCode_ReturnsValidationResult()
        {
            // Arrange
            var storeCode = "1234";
            var expectedResult = new StoreCodeValidationDto
            {
                IsValid = true,
                ShopName = "Test Shop"
            };

            _mockRegistrationService
                .Setup(s => s.ValidateStoreCodeAsync(storeCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ValidateStoreCode(storeCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResult = Assert.IsType<StoreCodeValidationDto>(okResult.Value);

            Assert.Equal(expectedResult.IsValid, actualResult.IsValid);
            Assert.Equal(expectedResult.ShopName, actualResult.ShopName);
            Assert.Equal(expectedResult.ErrorMessage, actualResult.ErrorMessage);

            _mockRegistrationService.Verify(s => s.ValidateStoreCodeAsync(storeCode), Times.Once);
        }

        [Fact]
        public async Task ValidateStoreCode_WithInvalidCode_ReturnsInvalidResult()
        {
            // Arrange
            var storeCode = "INVALID";
            var expectedResult = new StoreCodeValidationDto
            {
                IsValid = false,
                ErrorMessage = "Invalid or disabled store code"
            };

            _mockRegistrationService
                .Setup(s => s.ValidateStoreCodeAsync(storeCode))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ValidateStoreCode(storeCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResult = Assert.IsType<StoreCodeValidationDto>(okResult.Value);

            Assert.False(actualResult.IsValid);
            Assert.Equal(expectedResult.ErrorMessage, actualResult.ErrorMessage);

            _mockRegistrationService.Verify(s => s.ValidateStoreCodeAsync(storeCode), Times.Once);
        }

        [Fact]
        public async Task RegisterOwner_WithValidRequest_ReturnsSuccessResult()
        {
            // Arrange
            var request = new RegisterOwnerRequest
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Password = "SecurePassword123!",
                ShopName = "John's Shop",
                Phone = "555-123-4567"
            };

            var expectedResult = new RegistrationResultDto
            {
                Success = true,
                Message = "Account created successfully. You'll receive an email when approved."
            };

            _mockRegistrationService
                .Setup(s => s.RegisterOwnerAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RegisterOwner(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(actualResult.Success);
            Assert.Equal(expectedResult.Message, actualResult.Message);

            _mockRegistrationService.Verify(s => s.RegisterOwnerAsync(request), Times.Once);
        }

        [Fact]
        public async Task RegisterOwner_WithDuplicateEmail_ReturnsErrorResult()
        {
            // Arrange
            var request = new RegisterOwnerRequest
            {
                FullName = "Jane Doe",
                Email = "existing@example.com",
                Password = "SecurePassword123!",
                ShopName = "Jane's Shop"
            };

            var expectedResult = new RegistrationResultDto
            {
                Success = false,
                Message = "An account with this email already exists.",
                Errors = new List<string> { "Email already in use" }
            };

            _mockRegistrationService
                .Setup(s => s.RegisterOwnerAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RegisterOwner(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var actualResult = Assert.IsType<RegistrationResultDto>(badRequestResult.Value);

            Assert.False(actualResult.Success);
            Assert.Equal(expectedResult.Message, actualResult.Message);
            Assert.Equal(expectedResult.Errors, actualResult.Errors);

            _mockRegistrationService.Verify(s => s.RegisterOwnerAsync(request), Times.Once);
        }

        [Fact]
        public async Task RegisterConsignor_WithValidRequest_ReturnsSuccessResult()
        {
            // Arrange
            var request = new RegisterConsignorRequest
            {
                StoreCode = "1234",
                FullName = "Consignor Name",
                Email = "consignor@example.com",
                Password = "SecurePassword123!",
                Phone = "555-987-6543",
                PreferredPaymentMethod = "Venmo",
                PaymentDetails = "@consignor"
            };

            var expectedResult = new RegistrationResultDto
            {
                Success = true,
                Message = "Account created successfully. You'll receive an email when approved."
            };

            _mockRegistrationService
                .Setup(s => s.RegisterConsignorAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RegisterConsignor(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResult = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(actualResult.Success);
            Assert.Equal(expectedResult.Message, actualResult.Message);

            _mockRegistrationService.Verify(s => s.RegisterConsignorAsync(request), Times.Once);
        }

        [Fact]
        public async Task RegisterConsignor_WithInvalidStoreCode_ReturnsErrorResult()
        {
            // Arrange
            var request = new RegisterConsignorRequest
            {
                StoreCode = "INVALID",
                FullName = "Consignor Name",
                Email = "consignor@example.com",
                Password = "SecurePassword123!"
            };

            var expectedResult = new RegistrationResultDto
            {
                Success = false,
                Message = "Invalid store code",
                Errors = new List<string> { "Invalid store code" }
            };

            _mockRegistrationService
                .Setup(s => s.RegisterConsignorAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RegisterConsignor(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var actualResult = Assert.IsType<RegistrationResultDto>(badRequestResult.Value);

            Assert.False(actualResult.Success);
            Assert.Equal(expectedResult.Message, actualResult.Message);
            Assert.Equal(expectedResult.Errors, actualResult.Errors);

            _mockRegistrationService.Verify(s => s.RegisterConsignorAsync(request), Times.Once);
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
    }
}