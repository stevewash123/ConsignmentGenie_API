using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.SetupWizard;
using ConsignmentGenie.Core.Interfaces;

namespace ConsignmentGenie.Tests.Controllers
{
    public class SetupWizardControllerTests
    {
        private readonly Mock<ISetupWizardService> _mockSetupWizardService;
        private readonly Mock<ILogger<SetupWizardController>> _mockLogger;
        private readonly SetupWizardController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();

        public SetupWizardControllerTests()
        {
            _mockSetupWizardService = new Mock<ISetupWizardService>();
            _mockLogger = new Mock<ILogger<SetupWizardController>>();
            _controller = new SetupWizardController(_mockSetupWizardService.Object, _mockLogger.Object);

            // Setup user claims
            var claims = new List<Claim>
            {
                new("organizationId", _organizationId.ToString()),
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
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesSuccessfully()
        {
            // Arrange & Act
            var controller = new SetupWizardController(_mockSetupWizardService.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task GetWizardProgress_WithValidService_ReturnsOkResult()
        {
            // Arrange
            var expectedProgress = new SetupWizardProgressDto
            {
                CurrentStep = 1,
                TotalSteps = 4,
                IsCompleted = false,
                ProgressPercentage = 25.0,
                Steps = new List<SetupWizardStepDto>
                {
                    new SetupWizardStepDto
                    {
                        StepNumber = 1,
                        StepTitle = "Shop Profile",
                        IsCompleted = true
                    }
                }
            };

            _mockSetupWizardService.Setup(s => s.GetWizardProgressAsync(_organizationId))
                .ReturnsAsync(expectedProgress);

            // Act
            var result = await _controller.GetWizardProgress();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<SetupWizardProgressDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(expectedProgress.CurrentStep, apiResponse.Data.CurrentStep);
            Assert.Equal(expectedProgress.TotalSteps, apiResponse.Data.TotalSteps);
        }

        [Fact]
        public async Task GetWizardProgress_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            _mockSetupWizardService.Setup(s => s.GetWizardProgressAsync(_organizationId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetWizardProgress();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var apiResponse = Assert.IsType<ApiResponse<SetupWizardProgressDto>>(statusCodeResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Failed to get wizard progress", apiResponse.Errors);
        }

        [Fact]
        public async Task GetWizardStep_WithValidStepNumber_ReturnsOkResult()
        {
            // Arrange
            var stepNumber = 1;
            var expectedStep = new SetupWizardStepDto
            {
                StepNumber = stepNumber,
                StepTitle = "Shop Profile",
                IsCompleted = true,
                IsCurrentStep = true
            };

            _mockSetupWizardService.Setup(s => s.GetWizardStepAsync(_organizationId, stepNumber))
                .ReturnsAsync(expectedStep);

            // Act
            var result = await _controller.GetWizardStep(stepNumber);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<SetupWizardStepDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(stepNumber, apiResponse.Data.StepNumber);
            Assert.Equal("Shop Profile", apiResponse.Data.StepTitle);
        }

        [Fact]
        public async Task GetWizardStep_WithInvalidStepNumber_ReturnsBadRequest()
        {
            // Arrange
            var invalidStepNumber = 999;
            _mockSetupWizardService.Setup(s => s.GetWizardStepAsync(_organizationId, invalidStepNumber))
                .ThrowsAsync(new ArgumentException("Invalid step number"));

            // Act
            var result = await _controller.GetWizardStep(invalidStepNumber);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<SetupWizardStepDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Invalid step number", apiResponse.Errors);
        }

        [Fact]
        public async Task GetWizardStep_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var stepNumber = 1;
            _mockSetupWizardService.Setup(s => s.GetWizardStepAsync(_organizationId, stepNumber))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetWizardStep(stepNumber);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var apiResponse = Assert.IsType<ApiResponse<SetupWizardStepDto>>(statusCodeResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Failed to get wizard step", apiResponse.Errors);
        }

    }
}