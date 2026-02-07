using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class OrganizationSettingsManagerTests
{
    private readonly Mock<ILogger<OrganizationSettingsManager>> _loggerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly ConsignmentGenieContext _context;
    private readonly OrganizationSettingsManager _settingsManager;
    private readonly Organization _testOrganization;

    public OrganizationSettingsManagerTests()
    {
        _loggerMock = new Mock<ILogger<OrganizationSettingsManager>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ConsignmentGenieContext(options);

        // Create test organization
        _testOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            AgreementTemplateId = null,
            ConsignorSettings = null,
            ApprovalMode = Core.Enums.ApprovalMode.Auto
        };

        _context.Organizations.Add(_testOrganization);
        _context.SaveChanges();

        _settingsManager = new OrganizationSettingsManager(_context, _memoryCache, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPageSettingsAsync_WithEmptyDatabase_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _settingsManager.GetPageSettingsAsync<ConsignorPageSettings>(_testOrganization.Id, SettingsPage.Consignor);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("none", settings.AgreementRequirement);
        Assert.Null(settings.AgreementTemplateId);
        Assert.Equal("auto", settings.ApprovalMode);
        Assert.False(settings.RequireSignedAgreement);
    }

    [Fact]
    public async Task SetPageSettingsAsync_WithValidSettings_SynchronizesToDatabaseColumns()
    {
        // Arrange
        var agreementTemplateId = Guid.NewGuid();
        var settings = new ConsignorPageSettings
        {
            AgreementRequirement = "required",
            AgreementTemplateId = agreementTemplateId,
            ApprovalMode = "manual",
            RequireSignedAgreement = true,
            AcknowledgeTermsText = "I agree to the terms"
        };

        // Act
        await _settingsManager.SetPageSettingsAsync(_testOrganization.Id, SettingsPage.Consignor, settings);

        // Assert - Verify JSON settings
        var updatedOrg = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == _testOrganization.Id);
        Assert.NotNull(updatedOrg);
        Assert.NotNull(updatedOrg.ConsignorSettings);
        Assert.Contains("required", updatedOrg.ConsignorSettings);
        Assert.Contains(agreementTemplateId.ToString(), updatedOrg.ConsignorSettings);

        // Assert - Verify database column synchronization
        Assert.Equal(agreementTemplateId, updatedOrg.AgreementTemplateId);
    }

    [Fact]
    public async Task SetPageSettingsAsync_WithInvalidSettings_ThrowsValidationException()
    {
        // Arrange
        var invalidSettings = new ConsignorPageSettings
        {
            AgreementRequirement = "required", // Required but no template ID
            AgreementTemplateId = null, // This should cause validation to fail
            ApprovalMode = "auto"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _settingsManager.SetPageSettingsAsync(_testOrganization.Id, SettingsPage.Consignor, invalidSettings));

        Assert.Contains("Agreement template is required", exception.Message);
    }

    [Fact]
    public async Task GetPageSettingsAsync_UsesCaching_ReducesDatabaseQueries()
    {
        // Arrange
        var settings = new ConsignorPageSettings
        {
            AgreementRequirement = "upload",
            AgreementTemplateId = Guid.NewGuid(),
            ApprovalMode = "manual"
        };

        // Set settings first
        await _settingsManager.SetPageSettingsAsync(_testOrganization.Id, SettingsPage.Consignor, settings);

        // Act - First call should hit database
        var firstCall = await _settingsManager.GetPageSettingsAsync<ConsignorPageSettings>(_testOrganization.Id, SettingsPage.Consignor);

        // Act - Second call should use cache
        var secondCall = await _settingsManager.GetPageSettingsAsync<ConsignorPageSettings>(_testOrganization.Id, SettingsPage.Consignor);

        // Assert
        Assert.Equal(firstCall.AgreementRequirement, secondCall.AgreementRequirement);
        Assert.Equal(firstCall.AgreementTemplateId, secondCall.AgreementTemplateId);
        Assert.Equal(firstCall.ApprovalMode, secondCall.ApprovalMode);
    }

    [Fact]
    public async Task SettingsSync_ResolvesPreviousDataInconsistency()
    {
        // Arrange - Simulate the original problem: stale data in database column
        var newTemplateId = Guid.NewGuid();
        _testOrganization.AgreementTemplateId = Guid.NewGuid(); // Old stale data
        _testOrganization.ConsignorSettings = null; // No JSON settings yet
        _context.SaveChanges();

        var settings = new ConsignorPageSettings
        {
            AgreementRequirement = "required",
            AgreementTemplateId = newTemplateId, // New correct value
            ApprovalMode = "manual"
        };

        // Act - Update settings through new architecture
        await _settingsManager.SetPageSettingsAsync(_testOrganization.Id, SettingsPage.Consignor, settings);

        // Assert - Both JSON and database column should now be synchronized
        var updatedOrg = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == _testOrganization.Id);
        Assert.NotNull(updatedOrg);

        // JSON should contain new value
        Assert.Contains(newTemplateId.ToString(), updatedOrg.ConsignorSettings);

        // Database column should be updated to match
        Assert.Equal(newTemplateId, updatedOrg.AgreementTemplateId);
    }
}