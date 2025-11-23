using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.DTOs.SetupWizard;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace ConsignmentGenie.Tests.Services
{
    public class SlugMigrationTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<SetupWizardService>> _mockLogger;
        private readonly SetupWizardService _setupWizardService;
        private readonly ShopperAuthService _shopperAuthService;

        public SlugMigrationTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();

            // Mock configuration for JWT
            _mockConfiguration = new Mock<IConfiguration>();
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Key"]).Returns("ConsignmentGenie_Test_Secret_Key_32_Characters_Long!");
            jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
            jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
            _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

            _mockLogger = new Mock<ILogger<SetupWizardService>>();

            _setupWizardService = new SetupWizardService(_context, _mockLogger.Object);
            _shopperAuthService = new ShopperAuthService(_context, _mockConfiguration.Object);
        }

        [Fact]
        public async Task StorefrontSettings_UsesSlugInsteadOfStoreSlug()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Shop",
                Slug = "test-shop-slug",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var storefrontSettings = new StorefrontSettingsDto
            {
                Slug = "updated-slug"
            };

            // Act - Use the available method
            var result = await _setupWizardService.UpdateStorefrontSettingsAsync(organization.Id, storefrontSettings);

            // Assert
            Assert.NotNull(result);

            // Verify in database that slug was updated
            var updatedOrg = await _context.Organizations.FindAsync(organization.Id);
            Assert.Equal("updated-slug", updatedOrg.Slug);
        }

        [Fact]
        public async Task UpdateStorefrontSettings_UpdatesSlugCorrectly()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Shop",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var storefrontDto = new StorefrontSettingsDto
            {
                Slug = "new-slug-value"
            };

            // Act
            var result = await _setupWizardService.UpdateStorefrontSettingsAsync(organization.Id, storefrontDto);

            // Assert
            Assert.NotNull(result);

            // Verify in database
            var updatedOrg = await _context.Organizations.FindAsync(organization.Id);
            Assert.Equal("new-slug-value", updatedOrg.Slug);
        }

        [Fact]
        public void ShopperJwtToken_ContainsSlugClaim()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var shopperId = Guid.NewGuid();
            var email = "test@example.com";
            var organizationId = Guid.NewGuid();
            var storeSlug = "test-store";

            // Act
            var token = _shopperAuthService.GenerateShopperJwtToken(userId, shopperId, email, organizationId, storeSlug);

            // Assert
            Assert.NotNull(token);
            Assert.True(token.Length > 0);

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            var slugClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "Slug");
            Assert.NotNull(slugClaim);
            Assert.Equal(storeSlug, slugClaim.Value);
        }

        [Fact]
        public async Task CompleteSetup_ReturnsSlugNotStoreSlug()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Complete Test Shop",
                ShopName = "Complete Shop",
                Slug = "complete-shop-slug",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Act
            var result = await _setupWizardService.CompleteSetupAsync(organization.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("complete-shop-slug", result.Slug);
            Assert.Equal("Complete Test Shop", result.OrganizationName);
            Assert.Equal("Complete Shop", result.ShopName);
        }

        [Fact]
        public void DatabaseContext_NoLongerContainsStoreSlugIndex()
        {
            // Arrange & Act
            var organizationEntityType = _context.Model.FindEntityType(typeof(Organization));
            var indexes = organizationEntityType.GetIndexes();

            // Assert - Should have Slug index but not StoreSlug index
            var slugIndex = indexes.FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == "Slug"));
            var storeSlugIndex = indexes.FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == "StoreSlug"));

            Assert.NotNull(slugIndex);
            Assert.Null(storeSlugIndex);
        }

        [Fact]
        public async Task OrganizationEntity_DoesNotHaveStoreSlugProperty()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Property Test Shop",
                Slug = "property-test-slug",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Act & Assert
            var orgType = typeof(Organization);
            var storeSlugProperty = orgType.GetProperty("StoreSlug");
            var slugProperty = orgType.GetProperty("Slug");

            Assert.Null(storeSlugProperty); // Should not exist
            Assert.NotNull(slugProperty); // Should exist
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}