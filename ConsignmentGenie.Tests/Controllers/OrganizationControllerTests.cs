using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.DTOs.Onboarding;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class OrganizationControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly OrganizationController _controller;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public OrganizationControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            var mockLogger = new Mock<ILogger<OrganizationController>>();
            _controller = new OrganizationController(_context, mockLogger.Object);

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
                AutoApproveConsignors = true,
                OnboardingDismissed = false,
                WelcomeGuideCompleted = false,
                StoreEnabled = false,
                StripeConnected = false,
                QuickBooksConnected = false,
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
        public async Task GetSetupStatus_ReturnsCorrectSetupStatus_WhenAllStepsIncomplete()
        {
            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            // Serialize and deserialize to work around anonymous type issues
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
        public async Task GetSetupStatus_ReturnsCorrectSetupStatus_WhenSomeStepsComplete()
        {
            // Arrange - Add a provider and enable store
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.StoreEnabled = true;

            var provider = new Consignor
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                FirstName = "Test",
                LastName = "Consignor",
                Email = "test@provider.com",
                Status = ConsignorStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };
            _context.Consignors.Add(provider);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());

            var data = response.GetProperty("data");
            Assert.True(data.GetProperty("showModal").GetBoolean()); // Still should show since not all steps complete

            var steps = data.GetProperty("steps");
            Assert.True(steps.GetProperty("hasProviders").GetBoolean());
            Assert.True(steps.GetProperty("storefrontConfigured").GetBoolean());
            Assert.False(steps.GetProperty("hasInventory").GetBoolean());
            Assert.False(steps.GetProperty("quickBooksConnected").GetBoolean());
        }

        [Fact]
        public async Task GetSetupStatus_ReturnsShowModalFalse_WhenAllStepsComplete()
        {
            // Arrange - Complete all steps
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.StoreEnabled = true;
            organization.QuickBooksConnected = true;

            var provider = new Consignor
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                FirstName = "Test",
                LastName = "Consignor",
                Email = "test@provider.com",
                Status = ConsignorStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };
            _context.Consignors.Add(provider);

            var item = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = provider.Id,
                Title = "Test Item",
                Price = 100.00m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Items.Add(item);

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            var data = response.GetProperty("data");
            Assert.False(data.GetProperty("showModal").GetBoolean()); // Should not show modal since all steps complete

            var steps = data.GetProperty("steps");
            Assert.True(steps.GetProperty("hasProviders").GetBoolean());
            Assert.True(steps.GetProperty("storefrontConfigured").GetBoolean());
            Assert.True(steps.GetProperty("hasInventory").GetBoolean());
            Assert.True(steps.GetProperty("quickBooksConnected").GetBoolean());
        }

        [Fact]
        public async Task GetSetupStatus_ReturnsShowModalFalse_WhenWelcomeGuideCompleted()
        {
            // Arrange - Complete welcome guide but leave some steps incomplete
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.WelcomeGuideCompleted = true;
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

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
        public async Task GetSetupStatus_ReturnsNotFound_WhenOrganizationNotExists()
        {
            // Arrange - Remove the organization
            var organization = await _context.Organizations.FindAsync(_organizationId);
            _context.Organizations.Remove(organization!);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task DismissWelcomeGuide_UpdatesWelcomeGuideCompleted()
        {
            // Act
            var result = await _controller.DismissWelcomeGuide();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());
            Assert.True(response.GetProperty("welcomeGuideCompleted").GetBoolean());
            Assert.Equal("Welcome guide dismissed successfully", response.GetProperty("message").GetString());

            // Verify the database was updated
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.True(organization!.WelcomeGuideCompleted);
        }

        [Fact]
        public async Task DismissWelcomeGuide_ReturnsNotFound_WhenOrganizationNotExists()
        {
            // Arrange - Remove the organization
            var organization = await _context.Organizations.FindAsync(_organizationId);
            _context.Organizations.Remove(organization!);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DismissWelcomeGuide();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task DismissWelcomeGuide_UpdatesTimestamp()
        {
            // Arrange - Get initial timestamp
            var organization = await _context.Organizations.FindAsync(_organizationId);
            var initialTimestamp = organization!.UpdatedAt;

            // Wait a small amount to ensure timestamp difference
            await Task.Delay(10);

            // Act
            await _controller.DismissWelcomeGuide();

            // Assert
            var updatedOrganization = await _context.Organizations.FindAsync(_organizationId);
            Assert.True(updatedOrganization!.UpdatedAt > initialTimestamp);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}