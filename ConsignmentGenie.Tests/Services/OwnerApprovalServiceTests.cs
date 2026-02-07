using BCrypt.Net;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class OwnerApprovalServiceTests : IDisposable
{
    private readonly Mock<ILogger<OwnerApprovalService>> _mockLogger;
    private readonly OwnerApprovalService _approvalService;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Organization _testOrganization;
    private readonly User _testUser;

    public OwnerApprovalServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<OwnerApprovalService>>();

        _approvalService = new OwnerApprovalService(_context, _mockLogger.Object);

        // Set up test data
        _testOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            RequirePinForTaxExempt = true,
            RequirePinForReturnsOver = 100.00M,
            RequirePinForVoid = true,
            RequirePinForDrawerOpen = false,
            OwnerPin = BCrypt.Net.BCrypt.HashPassword("1234")
        };

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            OrganizationId = _testOrganization.Id,
            Role = UserRole.Owner
        };

        _context.Organizations.Add(_testOrganization);
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RequiresApprovalAsync_TaxExempt_ReturnsTrueWhenEnabled()
    {
        // Act
        var result = await _approvalService.RequiresApprovalAsync(_testOrganization.Id, ApprovalAction.TaxExempt);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RequiresApprovalAsync_TaxExempt_ReturnsFalseWhenDisabled()
    {
        // Arrange
        _testOrganization.RequirePinForTaxExempt = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _approvalService.RequiresApprovalAsync(_testOrganization.Id, ApprovalAction.TaxExempt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RequiresApprovalAsync_LargeReturn_ReturnsTrueWhenAmountExceedsThreshold()
    {
        // Act
        var result = await _approvalService.RequiresApprovalAsync(_testOrganization.Id, ApprovalAction.LargeReturn, 150.00M);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RequiresApprovalAsync_LargeReturn_ReturnsFalseWhenAmountBelowThreshold()
    {
        // Act
        var result = await _approvalService.RequiresApprovalAsync(_testOrganization.Id, ApprovalAction.LargeReturn, 50.00M);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RequiresApprovalAsync_Void_ReturnsTrueWhenEnabled()
    {
        // Act
        var result = await _approvalService.RequiresApprovalAsync(_testOrganization.Id, ApprovalAction.Void);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RequiresApprovalAsync_DrawerOpen_ReturnsFalseWhenDisabled()
    {
        // Act
        var result = await _approvalService.RequiresApprovalAsync(_testOrganization.Id, ApprovalAction.DrawerOpen);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidatePinAsync_ValidPin_ReturnsApproved()
    {
        // Act
        var result = await _approvalService.ValidatePinAsync(_testOrganization.Id, "1234", ApprovalAction.TaxExempt);

        // Assert
        Assert.True(result.Approved);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task ValidatePinAsync_InvalidPin_ReturnsRejected()
    {
        // Act
        var result = await _approvalService.ValidatePinAsync(_testOrganization.Id, "9999", ApprovalAction.TaxExempt);

        // Assert
        Assert.False(result.Approved);
        Assert.Equal("Invalid PIN", result.Reason);
    }

    [Fact]
    public async Task ValidatePinAsync_EmptyPin_ReturnsRejected()
    {
        // Act
        var result = await _approvalService.ValidatePinAsync(_testOrganization.Id, "", ApprovalAction.TaxExempt);

        // Assert
        Assert.False(result.Approved);
        Assert.Equal("PIN is required", result.Reason);
    }

    [Fact]
    public async Task ValidatePinAsync_InvalidPinFormat_ReturnsRejected()
    {
        // Act
        var result = await _approvalService.ValidatePinAsync(_testOrganization.Id, "abc", ApprovalAction.TaxExempt);

        // Assert
        Assert.False(result.Approved);
        Assert.Equal("Invalid PIN format", result.Reason);
    }

    [Fact]
    public async Task SetPinAsync_ValidPinAndPassword_ReturnsTrue()
    {
        // Act
        var result = await _approvalService.SetPinAsync(_testOrganization.Id, "5678", "password123", _testUser.Id);

        // Assert
        Assert.True(result);

        // Verify PIN was updated
        var org = await _context.Organizations.FindAsync(_testOrganization.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("5678", org!.OwnerPin));
    }

    [Fact]
    public async Task SetPinAsync_InvalidPassword_ReturnsFalse()
    {
        // Act
        var result = await _approvalService.SetPinAsync(_testOrganization.Id, "5678", "wrongpassword", _testUser.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetPinAsync_InvalidPinFormat_ReturnsFalse()
    {
        // Act
        var result = await _approvalService.SetPinAsync(_testOrganization.Id, "12", "password123", _testUser.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetSecuritySettingsAsync_ReturnsCorrectSettings()
    {
        // Act
        var result = await _approvalService.GetSecuritySettingsAsync(_testOrganization.Id);

        // Assert
        Assert.True(result.HasPin);
        Assert.True(result.PinRequirements.TaxExempt);
        Assert.Equal(100.00M, result.PinRequirements.ReturnsOver);
        Assert.True(result.PinRequirements.Void);
        Assert.False(result.PinRequirements.DrawerOpen);
    }

    [Fact]
    public async Task UpdatePinRequirementsAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new UpdatePinRequirementsRequest
        {
            TaxExempt = false,
            ReturnsOver = 200.00M,
            Void = false,
            DrawerOpen = true
        };

        // Act
        var result = await _approvalService.UpdatePinRequirementsAsync(_testOrganization.Id, request);

        // Assert
        Assert.True(result);

        // Verify settings were updated
        var org = await _context.Organizations.FindAsync(_testOrganization.Id);
        Assert.False(org!.RequirePinForTaxExempt);
        Assert.Equal(200.00M, org.RequirePinForReturnsOver);
        Assert.False(org.RequirePinForVoid);
        Assert.True(org.RequirePinForDrawerOpen);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}