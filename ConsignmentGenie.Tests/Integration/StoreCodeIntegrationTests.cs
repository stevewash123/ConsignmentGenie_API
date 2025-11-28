using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Integration
{
    public class StoreCodeIntegrationTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly StoreCodeService _storeCodeService;
        private readonly RegistrationService _registrationService;

        public StoreCodeIntegrationTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockEmailService = new Mock<IEmailService>();
            _mockAuthService = new Mock<IAuthService>();

            _storeCodeService = new StoreCodeService(_context);
            _registrationService = new RegistrationService(
                _context,
                _mockEmailService.Object,
                _storeCodeService,
                _mockAuthService.Object);

            _mockAuthService
                .Setup(s => s.GenerateJwtToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .Returns("test-token");
        }

        [Fact]
        public async Task EndToEndRegistration_WithStoreCodeGeneration_CreatesValidStoreCode()
        {
            // Arrange
            var request = new RegisterOwnerRequest
            {
                FullName = "Integration Test Owner",
                Email = "integration@example.com",
                Password = "SecurePassword123!",
                ShopName = "Integration Test Shop",
                Subdomain = "integration-test-shop"
            };

            // Act
            var result = await _registrationService.RegisterOwnerAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Token);

            // Verify organization was created with valid store code
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Subdomain == request.Subdomain);

            Assert.NotNull(organization);
            Assert.True(_storeCodeService.IsValidStoreCode(organization.StoreCode));
            Assert.Matches(@"^\d{3}[ABCDEFGHJKMNPQRTUVWXY]{2}\d$", organization.StoreCode);
            Assert.True(organization.StoreCodeEnabled);

            // Verify user was created and linked
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            Assert.NotNull(user);
            Assert.Equal(organization.Id, user.OrganizationId);
            Assert.Equal(UserRole.Owner, user.Role);
        }

        [Fact]
        public async Task MultipleRegistrations_GenerateUniqueStoreCodes()
        {
            // Arrange
            var registrations = new[]
            {
                new RegisterOwnerRequest
                {
                    FullName = "Owner 1",
                    Email = "owner1@example.com",
                    Password = "SecurePassword123!",
                    ShopName = "Shop 1",
                    Subdomain = "shop-1"
                },
                new RegisterOwnerRequest
                {
                    FullName = "Owner 2",
                    Email = "owner2@example.com",
                    Password = "SecurePassword123!",
                    ShopName = "Shop 2",
                    Subdomain = "shop-2"
                },
                new RegisterOwnerRequest
                {
                    FullName = "Owner 3",
                    Email = "owner3@example.com",
                    Password = "SecurePassword123!",
                    ShopName = "Shop 3",
                    Subdomain = "shop-3"
                }
            };

            var generatedCodes = new HashSet<string>();

            // Act
            foreach (var request in registrations)
            {
                var result = await _registrationService.RegisterOwnerAsync(request);
                Assert.True(result.Success);

                var org = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Subdomain == request.Subdomain);

                Assert.NotNull(org);
                generatedCodes.Add(org.StoreCode);
            }

            // Assert
            Assert.Equal(3, generatedCodes.Count); // All codes should be unique

            foreach (var code in generatedCodes)
            {
                Assert.True(_storeCodeService.IsValidStoreCode(code));
                Assert.Matches(@"^\d{3}[ABCDEFGHJKMNPQRTUVWXY]{2}\d$", code);
            }
        }

        [Fact]
        public async Task StoreCodeRegeneration_UpdatesCodeAndTimestamp()
        {
            // Arrange - Create organization first
            var registerRequest = new RegisterOwnerRequest
            {
                FullName = "Regeneration Test",
                Email = "regen@example.com",
                Password = "SecurePassword123!",
                ShopName = "Regeneration Shop",
                Subdomain = "regeneration-shop"
            };

            var registerResult = await _registrationService.RegisterOwnerAsync(registerRequest);
            Assert.True(registerResult.Success);

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Subdomain == registerRequest.Subdomain);
            Assert.NotNull(organization);

            var originalCode = organization.StoreCode;
            var originalTimestamp = organization.UpdatedAt;

            await Task.Delay(100); // Ensure sufficient timestamp difference

            // Act
            var result = await _storeCodeService.RegenerateStoreCodeAsync(organization.Id);

            // Assert
            Assert.NotEqual(originalCode, result.StoreCode);
            Assert.True(_storeCodeService.IsValidStoreCode(result.StoreCode));
            Assert.True(result.IsEnabled);
            Assert.NotNull(result.LastRegenerated);

            // Verify timestamp is after original (should be updated)
            Assert.True(result.LastRegenerated.Value >= originalTimestamp,
                $"Timestamp should be updated. Original: {originalTimestamp}, New: {result.LastRegenerated}");

            // Verify in database
            var updatedOrg = await _context.Organizations.FindAsync(organization.Id);
            Assert.Equal(result.StoreCode, updatedOrg.StoreCode);
            Assert.Equal(result.LastRegenerated, updatedOrg.UpdatedAt);
        }

        [Fact]
        public async Task StoreCodeValidation_WithProviderRegistration_WorksCorrectly()
        {
            // Arrange - Create shop owner first
            var ownerRequest = new RegisterOwnerRequest
            {
                FullName = "Shop Owner",
                Email = "owner@example.com",
                Password = "SecurePassword123!",
                ShopName = "Validation Test Shop",
                Subdomain = "validation-shop"
            };

            var ownerResult = await _registrationService.RegisterOwnerAsync(ownerRequest);
            Assert.True(ownerResult.Success);

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Subdomain == ownerRequest.Subdomain);
            Assert.NotNull(organization);

            // Act - Validate store code
            var validationResult = await _registrationService.ValidateStoreCodeAsync(organization.StoreCode);

            // Assert
            Assert.True(validationResult.IsValid);
            Assert.Equal(organization.ShopName, validationResult.ShopName);

            // Test provider registration with the valid store code
            var providerRequest = new RegisterProviderRequest
            {
                StoreCode = organization.StoreCode,
                FullName = "Test Provider",
                Email = "provider@example.com",
                Password = "SecurePassword123!"
            };

            var providerResult = await _registrationService.RegisterProviderAsync(providerRequest);
            Assert.True(providerResult.Success);
        }

        [Fact]
        public async Task StoreCodeValidation_WithInvalidCode_ReturnsError()
        {
            // Arrange
            var invalidCodes = new[]
            {
                "INVALID", "1234", "12345", "123OI4", "ABC123", "", null
            };

            // Act & Assert
            foreach (var invalidCode in invalidCodes)
            {
                var result = await _registrationService.ValidateStoreCodeAsync(invalidCode);
                Assert.False(result.IsValid, $"Code '{invalidCode}' should be invalid");
            }
        }

        [Fact]
        public async Task StoreCodeToggling_UpdatesEnabledStatus()
        {
            // Arrange - Create organization
            var request = new RegisterOwnerRequest
            {
                FullName = "Toggle Test",
                Email = "toggle@example.com",
                Password = "SecurePassword123!",
                ShopName = "Toggle Shop",
                Subdomain = "toggle-shop"
            };

            var result = await _registrationService.RegisterOwnerAsync(request);
            Assert.True(result.Success);

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Subdomain == request.Subdomain);
            Assert.NotNull(organization);

            // Store code should be enabled by default
            Assert.True(organization.StoreCodeEnabled);

            // Act - Disable store code
            await _storeCodeService.ToggleStoreCodeAsync(organization.Id, false);

            // Assert - Should be disabled
            var disabledValidation = await _registrationService.ValidateStoreCodeAsync(organization.StoreCode);
            Assert.False(disabledValidation.IsValid);

            var storeCodeResult = await _storeCodeService.GetStoreCodeAsync(organization.Id);
            Assert.False(storeCodeResult.IsEnabled);

            // Act - Re-enable store code
            await _storeCodeService.ToggleStoreCodeAsync(organization.Id, true);

            // Assert - Should be enabled again
            var enabledValidation = await _registrationService.ValidateStoreCodeAsync(organization.StoreCode);
            Assert.True(enabledValidation.IsValid);

            var reenabledResult = await _storeCodeService.GetStoreCodeAsync(organization.Id);
            Assert.True(reenabledResult.IsEnabled);
        }

        [Fact]
        public async Task LargeScaleStoreCodeGeneration_MaintainsUniqueness()
        {
            // Arrange & Act - Generate many store codes
            const int codeCount = 500;
            var generatedCodes = new HashSet<string>();

            for (int i = 0; i < codeCount; i++)
            {
                var code = _storeCodeService.GenerateStoreCode();
                generatedCodes.Add(code);

                // Verify each code follows the pattern
                Assert.True(_storeCodeService.IsValidStoreCode(code));
                Assert.Matches(@"^\d{3}[ABCDEFGHJKMNPQRTUVWXY]{2}\d$", code);

                // Verify no excluded letters
                Assert.DoesNotContain('O', code);
                Assert.DoesNotContain('I', code);
                Assert.DoesNotContain('L', code);
                Assert.DoesNotContain('S', code);
                Assert.DoesNotContain('Z', code);
            }

            // Assert - High probability all codes are unique (collisions extremely unlikely)
            Assert.True(generatedCodes.Count >= codeCount * 0.99,
                $"Expected at least 99% unique codes, got {generatedCodes.Count}/{codeCount}");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}