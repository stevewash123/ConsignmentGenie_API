using ConsignmentGenie.Application.DTOs.Provider;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class ProviderInvitationServiceTests : IDisposable
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ProviderInvitationService _service;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ProviderInvitationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(x => x["App:BaseUrl"]).Returns("http://localhost:4200");
        _service = new ProviderInvitationService(_context, _mockEmailService.Object, _mockConfiguration.Object);
        SeedTestData();
    }

    private void SeedTestData()
    {
        var organization = new Organization
        {
            Id = _organizationId,
            Name = "Test Shop"
        };

        var user = new User
        {
            Id = _userId,
            Email = "owner@example.com",
            OrganizationId = _organizationId,
            Role = Core.Enums.UserRole.Owner,
            PasswordHash = "dummy"
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateInvitationAsync_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = new CreateProviderInvitationDto
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        _mockEmailService.Setup(x => x.SendProviderInvitationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateInvitationAsync(request, _organizationId, _userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Invitation sent successfully.", result.Message);
        Assert.NotNull(result.Invitation);
        Assert.Equal(request.Email, result.Invitation.Email);
    }

    public void Dispose() => _context.Dispose();
}