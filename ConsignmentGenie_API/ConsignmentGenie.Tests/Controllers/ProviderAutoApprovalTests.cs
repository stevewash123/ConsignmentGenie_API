using System;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ProviderAutoApprovalTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly RegistrationController _controller;
        private readonly Mock<ILogger<RegistrationController>> _loggerMock;

        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public ProviderAutoApprovalTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<RegistrationController>>();
            var mockRegistrationService = new Mock<IRegistrationService>();
            _controller = new RegistrationController(mockRegistrationService.Object);

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Create organization with auto-approve enabled
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Shop",
                StoreCode = "TEST",
                StoreCodeEnabled = true,
                AutoApproveProviders = true, // Auto-approve enabled
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
        public async Task RegisterProvider_WithAutoApproveEnabled_SetsStatusToActive()
        {
            // Arrange
            var request = new RegisterProviderRequest
            {
                StoreCode = "TEST",
                FullName = "John Doe",
                Email = "john.doe@test.com",
                Phone = "555-123-4567",
                Password = "password123",
                PaymentDetails = "Check"
            };

            // Act
            var result = await _controller.RegisterProvider(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(response.Success);
            Assert.Contains("Welcome to Test Shop", response.Message);

            // Verify provider is created with Active status
            var provider = await _context.Providers
                .Where(p => p.Email == request.Email)
                .FirstOrDefaultAsync();

            Assert.NotNull(provider);
            Assert.Equal(ProviderStatus.Active, provider.Status);
        }

        [Fact]
        public async Task RegisterProvider_WithAutoApproveDisabled_SetsStatusToPending()
        {
            // Arrange - disable auto-approve
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.AutoApproveProviders = false;
            await _context.SaveChangesAsync();

            var request = new RegisterProviderRequest
            {
                StoreCode = "TEST",
                FullName = "Jane Smith",
                Email = "jane.smith@test.com",
                Phone = "555-987-6543",
                Password = "password123",
                PaymentDetails = "PayPal"
            };

            // Act
            var result = await _controller.RegisterProvider(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<RegistrationResultDto>(okResult.Value);

            Assert.True(response.Success);
            Assert.Contains("pending approval", response.Message);

            // Verify provider is created with Pending status
            var provider = await _context.Providers
                .Where(p => p.Email == request.Email)
                .FirstOrDefaultAsync();

            Assert.NotNull(provider);
            Assert.Equal(ProviderStatus.Pending, provider.Status);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}