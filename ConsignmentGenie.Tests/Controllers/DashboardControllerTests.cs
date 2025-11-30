using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.DTOs.Onboarding;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class DashboardControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly DashboardController _controller;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public DashboardControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            var mockLogger = new Mock<ILogger<DashboardController>>();
            _controller = new DashboardController(_context, mockLogger.Object);

            // Mock JWT claims
            var claims = new[]
            {
                new Claim("organizationId", _organizationId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Shop",
                StoreCode = "TEST",
                StoreCodeEnabled = true,
                AutoApproveProviders = true,
                OnboardingDismissed = false,
                WelcomeGuideCompleted = false,
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Pro,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetOrganizationSettings_ReturnsCorrectSettings()
        {
            // Act
            var result = await _controller.GetOrganizationSettings();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            // Serialize and deserialize to work around anonymous type issues
            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());

            var data = response.GetProperty("data");
            Assert.True(data.GetProperty("autoApproveProviders").GetBoolean());
            Assert.True(data.GetProperty("storeCodeEnabled").GetBoolean());
            Assert.Equal("TEST", data.GetProperty("storeCode").GetString());
        }

        [Fact]
        public async Task UpdateAutoApproveProviders_EnablesAutoApprove()
        {
            // Arrange
            var request = new UpdateAutoApproveRequest { AutoApproveProviders = true };

            // Act
            var result = await _controller.UpdateAutoApproveProviders(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            // Serialize and deserialize to work around anonymous type issues
            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());
            Assert.Contains("auto-approval enabled", response.GetProperty("message").GetString());

            var data = response.GetProperty("data");
            Assert.True(data.GetProperty("autoApproveProviders").GetBoolean());

            // Verify in database
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.True(organization.AutoApproveProviders);
        }

        [Fact]
        public async Task UpdateAutoApproveProviders_DisablesAutoApprove()
        {
            // Arrange
            var request = new UpdateAutoApproveRequest { AutoApproveProviders = false };

            // Act
            var result = await _controller.UpdateAutoApproveProviders(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            // Serialize and deserialize to work around anonymous type issues
            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());
            Assert.Contains("auto-approval disabled", response.GetProperty("message").GetString());

            var data = response.GetProperty("data");
            Assert.False(data.GetProperty("autoApproveProviders").GetBoolean());

            // Verify in database
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.False(organization.AutoApproveProviders);
        }

        [Fact]
        public async Task GetOnboardingStatus_ReturnsCorrectStatus_WithNewFields()
        {
            // Act
            var result = await _controller.GetOnboardingStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());

            var data = response.GetProperty("data");
            Assert.False(data.GetProperty("dismissed").GetBoolean());
            Assert.False(data.GetProperty("welcomeGuideCompleted").GetBoolean());
            Assert.True(data.GetProperty("showModal").GetBoolean()); // Should show modal since steps are incomplete

            var steps = data.GetProperty("steps");
            Assert.False(steps.GetProperty("hasProviders").GetBoolean());
            Assert.False(steps.GetProperty("storefrontConfigured").GetBoolean());
            Assert.False(steps.GetProperty("hasInventory").GetBoolean());
            Assert.False(steps.GetProperty("quickBooksConnected").GetBoolean());
        }

        [Fact]
        public async Task GetOnboardingStatus_ReturnsShowModalFalse_WhenWelcomeGuideCompleted()
        {
            // Arrange - Complete welcome guide
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.WelcomeGuideCompleted = true;
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetOnboardingStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            var data = response.GetProperty("data");
            Assert.True(data.GetProperty("welcomeGuideCompleted").GetBoolean());
            Assert.False(data.GetProperty("showModal").GetBoolean()); // Should not show modal since welcome guide completed
        }

        [Fact]
        public async Task DismissOnboarding_UpdatesOnboardingDismissed()
        {
            // Arrange
            var request = new DismissOnboardingRequestDto { Dismissed = true };

            // Act
            var result = await _controller.DismissOnboarding(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());
            Assert.Equal("Onboarding status updated successfully", response.GetProperty("message").GetString());

            // Verify the database was updated
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.True(organization!.OnboardingDismissed);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}