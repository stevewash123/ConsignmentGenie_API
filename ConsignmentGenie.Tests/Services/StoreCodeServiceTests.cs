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
                StoreCode = "888TG4",
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
                StoreCode = "999AK7",
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
            Assert.Equal("888TG4", result.StoreCode);
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
            Assert.NotEqual("888TG4", result.StoreCode); // Should be different from original
            Assert.Matches(@"^\d{3}[ABCDEFGHJKMNPQRTUVWXY]{2}\d$", result.StoreCode); // Should match NNNLLN pattern
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
            Assert.Matches(@"^\d{3}[ABCDEFGHJKMNPQRTUVWXY]{2}\d$", storeCode); // Should match NNNLLN pattern
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

        [Theory]
        [InlineData("123AB4", true)]   // Valid NNNLLN format
        [InlineData("999TG7", true)]   // Valid NNNLLN format
        [InlineData("000ZZ0", false)]  // Invalid - Z is not allowed
        [InlineData("1234", false)]    // Invalid - old 4-digit format
        [InlineData("12345", false)]   // Invalid - old 5-digit format
        [InlineData("12AB34", false)]  // Invalid - too long
        [InlineData("12AB", false)]    // Invalid - too short
        [InlineData("ABC123", false)]  // Invalid - wrong pattern
        [InlineData("", false)]        // Invalid - empty
        [InlineData(null, false)]      // Invalid - null
        public void IsValidStoreCode_WithVariousFormats_ReturnsCorrectValidation(string code, bool expected)
        {
            // Act
            var result = _storeCodeService.IsValidStoreCode(code);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsValidStoreCode_WithExcludedLetters_ReturnsFalse()
        {
            // Arrange - Test excluded letters: O, I, L, S, Z
            var codesWithExcludedLetters = new[]
            {
                "123OA4", // O excluded
                "123IA4", // I excluded
                "123LA4", // L excluded
                "123SA4", // S excluded
                "123ZA4", // Z excluded
                "123AO4", // O excluded in second position
                "123AI4", // I excluded in second position
                "123AL4", // L excluded in second position
                "123AS4", // S excluded in second position
                "123AZ4"  // Z excluded in second position
            };

            // Act & Assert
            foreach (var code in codesWithExcludedLetters)
            {
                var result = _storeCodeService.IsValidStoreCode(code);
                Assert.False(result, $"Code {code} should be invalid due to excluded letters");
            }
        }

        [Fact]
        public void GenerateStoreCode_NeverGeneratesExcludedLetters()
        {
            // Arrange
            var excludedLetters = new[] { 'O', 'I', 'L', 'S', 'Z' };

            // Act - Generate many codes to test
            for (int i = 0; i < 100; i++)
            {
                var code = _storeCodeService.GenerateStoreCode();

                // Assert - Check that no excluded letters appear in positions 3-4
                Assert.DoesNotContain(excludedLetters, letter => code[3] == letter);
                Assert.DoesNotContain(excludedLetters, letter => code[4] == letter);
            }
        }

        [Fact]
        public void GenerateStoreCode_AlwaysFollowsNNNLLNPattern()
        {
            // Act - Generate many codes to test pattern consistency
            for (int i = 0; i < 100; i++)
            {
                var code = _storeCodeService.GenerateStoreCode();

                // Assert
                Assert.Equal(6, code.Length);
                Assert.True(char.IsDigit(code[0]), $"Position 0 should be digit in code {code}");
                Assert.True(char.IsDigit(code[1]), $"Position 1 should be digit in code {code}");
                Assert.True(char.IsDigit(code[2]), $"Position 2 should be digit in code {code}");
                Assert.True(char.IsLetter(code[3]), $"Position 3 should be letter in code {code}");
                Assert.True(char.IsLetter(code[4]), $"Position 4 should be letter in code {code}");
                Assert.True(char.IsDigit(code[5]), $"Position 5 should be digit in code {code}");
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}