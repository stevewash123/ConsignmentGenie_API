using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            _controller = new DashboardController(_context);

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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}