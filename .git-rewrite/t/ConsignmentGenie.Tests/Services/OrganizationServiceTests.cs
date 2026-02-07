using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConsignmentGenie.Tests.Services
{
    public class OrganizationServiceTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;

        public OrganizationServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
        }

        [Fact]
        public async Task Organization_SlugPropertyWorks()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Organization",
                Slug = "test-org-slug",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Assert
            var savedOrg = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == organization.Id);
            Assert.NotNull(savedOrg);
            Assert.Equal("test-org-slug", savedOrg.Slug);
        }

        [Fact]
        public async Task Organization_SlugIndex_ExistsInModel()
        {
            // Arrange & Act
            var organizationEntityType = _context.Model.FindEntityType(typeof(Organization));
            var indexes = organizationEntityType.GetIndexes();

            // Assert - Should have Slug index configured (unique constraint will be enforced in real database)
            var slugIndex = indexes.FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == "Slug"));

            Assert.NotNull(slugIndex);
            Assert.True(slugIndex.IsUnique); // Index should be marked as unique
        }

        [Fact]
        public async Task Organization_CanBeQueriedBySlug()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Searchable Organization",
                Slug = "searchable-org",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Act
            var foundOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == "searchable-org");

            // Assert
            Assert.NotNull(foundOrg);
            Assert.Equal(organization.Id, foundOrg.Id);
            Assert.Equal("Searchable Organization", foundOrg.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Organization_CanHaveNullOrEmptySlug(string? slugValue)
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Organization With Empty Slug",
                Slug = slugValue,
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act & Assert - Should not throw
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var savedOrg = await _context.Organizations.FindAsync(organization.Id);
            Assert.NotNull(savedOrg);
            Assert.Equal(slugValue, savedOrg.Slug);
        }

        [Fact]
        public async Task Organization_SlugCanBeUpdated()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Updatable Organization",
                Slug = "original-slug",
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Basic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Act
            organization.Slug = "updated-slug";
            organization.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Assert
            var updatedOrg = await _context.Organizations.FindAsync(organization.Id);
            Assert.NotNull(updatedOrg);
            Assert.Equal("updated-slug", updatedOrg.Slug);
        }

        [Fact]
        public void Organization_HasCorrectProperties()
        {
            // Act
            var orgType = typeof(Organization);
            var properties = orgType.GetProperties();

            // Assert
            var slugProperty = properties.FirstOrDefault(p => p.Name == "Slug");
            var storeSlugProperty = properties.FirstOrDefault(p => p.Name == "StoreSlug");

            Assert.NotNull(slugProperty);
            Assert.Null(storeSlugProperty); // Should no longer exist

            // Verify Slug property type
            Assert.Equal(typeof(string), slugProperty.PropertyType);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}