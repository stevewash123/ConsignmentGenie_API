using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConsignmentGenie.Tests.Services
{
    public class StoreCodeServiceTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly StoreCodeService _storeCodeService;

        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public StoreCodeServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _storeCodeService = new StoreCodeService(_context);

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Shop",
                StoreCode = "1234",
                StoreCodeEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add organization with existing store codes to test uniqueness
            var org2 = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Other Shop",
                StoreCode = "5678",
                StoreCodeEnabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(org2);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetStoreCodeAsync_WithValidOrganization_ReturnsStoreCode()
        {
            // Act
            var result = await _storeCodeService.GetStoreCodeAsync(_organizationId);

            // Assert
            Assert.Equal("1234", result.StoreCode);
            Assert.True(result.IsEnabled);
            Assert.NotNull(result.LastRegenerated);
        }

        [Fact]
        public async Task GetStoreCodeAsync_WithInvalidOrganization_ThrowsException()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _storeCodeService.GetStoreCodeAsync(nonExistentOrgId));
        }

        [Fact]
        public async Task RegenerateStoreCodeAsync_GeneratesNewCode()
        {
            // Act
            var result = await _storeCodeService.RegenerateStoreCodeAsync(_organizationId);

            // Assert
            Assert.NotEqual("1234", result.StoreCode); // Should be different from original
            Assert.Matches(@"^\d{4,5}$", result.StoreCode); // Should be 4 or 5 digits
            Assert.True(result.IsEnabled);
            Assert.NotNull(result.LastRegenerated);

            // Verify in database
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.NotNull(organization);
            Assert.Equal(result.StoreCode, organization.StoreCode);
        }

        [Fact]
        public async Task ToggleStoreCodeAsync_TogglesEnabledStatus()
        {
            // Act - Disable
            await _storeCodeService.ToggleStoreCodeAsync(_organizationId, false);

            // Assert - Should be disabled
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.NotNull(organization);
            Assert.False(organization.StoreCodeEnabled);

            // Act - Enable
            await _storeCodeService.ToggleStoreCodeAsync(_organizationId, true);

            // Assert - Should be enabled
            organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.NotNull(organization);
            Assert.True(organization.StoreCodeEnabled);
        }

        [Fact]
        public async Task ToggleStoreCodeAsync_WithInvalidOrganization_ThrowsException()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _storeCodeService.ToggleStoreCodeAsync(nonExistentOrgId, false));
        }

        [Fact]
        public void GenerateStoreCode_GeneratesValidCode()
        {
            // Act
            var storeCode = _storeCodeService.GenerateStoreCode();

            // Assert
            Assert.Matches(@"^\d{4,5}$", storeCode); // Should be 4 or 5 digits
            Assert.NotNull(storeCode);
            Assert.NotEmpty(storeCode);
        }

        [Fact]
        public void GenerateStoreCode_GeneratesUniqueCode()
        {
            // Act - Generate multiple codes
            var codes = new HashSet<string>();
            for (int i = 0; i < 10; i++)
            {
                var code = _storeCodeService.GenerateStoreCode();
                codes.Add(code);
            }

            // Assert - All codes should be unique (very high probability)
            Assert.True(codes.Count >= 8, "Most generated codes should be unique");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ToggleStoreCodeAsync_WithDifferentValues_WorksCorrectly(bool enableValue)
        {
            // Act
            await _storeCodeService.ToggleStoreCodeAsync(_organizationId, enableValue);

            // Assert
            var storeCodeResult = await _storeCodeService.GetStoreCodeAsync(_organizationId);
            Assert.Equal(enableValue, storeCodeResult.IsEnabled);
        }

        [Fact]
        public async Task RegenerateStoreCodeAsync_UpdatesTimestamp()
        {
            // Arrange
            var originalTimestamp = (await _context.Organizations.FindAsync(_organizationId))!.UpdatedAt;
            await Task.Delay(10); // Small delay to ensure timestamp difference

            // Act
            var result = await _storeCodeService.RegenerateStoreCodeAsync(_organizationId);

            // Assert
            var updatedOrg = await _context.Organizations.FindAsync(_organizationId);
            Assert.NotNull(updatedOrg);
            Assert.True(updatedOrg.UpdatedAt > originalTimestamp);
            Assert.Equal(result.LastRegenerated, updatedOrg.UpdatedAt);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}