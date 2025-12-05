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
    public class ConsignorAutoApprovalTests
    {
        private readonly RegistrationController _controller;
        private readonly Mock<IRegistrationService> _mockRegistrationService;

        public ConsignorAutoApprovalTests()
        {
            _mockRegistrationService = new Mock<IRegistrationService>();
            var mockLogger = new Mock<ILogger<RegistrationController>>();
            _controller = new RegistrationController(_mockRegistrationService.Object, mockLogger.Object);
        }

        [Fact]
        public async Task RegisterConsignor_WithAutoApproveEnabled_CallsServiceAndReturnsSuccess()
        {
            // Arrange
            var request = new RegisterConsignorRequest
            {
                StoreCode = "TEST",
                FullName = "John Doe",
                Email = "john.doe@test.com",
                Phone = "555-123-4567",
                Password = "password123",
                PaymentDetails = "Check"
            };

            var expectedResult = new RegistrationResultDto
            {
                Success = true,
                Message = "Welcome to Test Shop! Your account is active."
            };

            _mockRegistrationService
                .Setup(x => x.RegisterConsignorAsync(It.IsAny<RegisterConsignorRequest>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RegisterConsignor(request);

            // Assert - Controller behavior only
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(response.Success);
            Assert.Contains("Welcome to Test Shop", response.Message);

            // ✅ Verify service was called with correct data
            _mockRegistrationService.Verify(x => x.RegisterConsignorAsync(
                It.Is<RegisterConsignorRequest>(r =>
                    r.Email == "john.doe@test.com" &&
                    r.StoreCode == "TEST"
                )
            ), Times.Once);
        }

        [Fact]
        public async Task RegisterConsignor_WithAutoApproveDisabled_CallsServiceAndReturnsPending()
        {
            // Arrange
            var request = new RegisterConsignorRequest
            {
                StoreCode = "TEST",
                FullName = "Jane Smith",
                Email = "jane.smith@test.com",
                Phone = "555-987-6543",
                Password = "password123",
                PaymentDetails = "PayPal"
            };

            var expectedResult = new RegistrationResultDto
            {
                Success = true,
                Message = "Thank you for registering! Your account is pending approval."
            };

            _mockRegistrationService
                .Setup(x => x.RegisterConsignorAsync(It.IsAny<RegisterConsignorRequest>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RegisterConsignor(request);

            // Assert - Controller behavior only
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(response.Success);
            Assert.Contains("pending approval", response.Message);

            // ✅ Verify service was called with correct data
            _mockRegistrationService.Verify(x => x.RegisterConsignorAsync(
                It.Is<RegisterConsignorRequest>(r =>
                    r.Email == "jane.smith@test.com" &&
                    r.StoreCode == "TEST"
                )
            ), Times.Once);
        }
    }
}