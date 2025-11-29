using ConsignmentGenie.Application.Models.Notifications;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly Mock<INotificationTemplateService> _mockTemplateService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly ConsignmentGenieContext _context;

    public NotificationServiceTests()
    {
        _mockTemplateService = new Mock<INotificationTemplateService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ConsignmentGenieContext(options);
    }

    [Fact]
    public void NotificationService_Constructor_InitializesCorrectly()
    {
        // Act
        var service = new NotificationService(
            _context,
            _mockTemplateService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Theory]
    [InlineData("https://consignmentgenie-ui.onrender.com", "https://consignmentgenie-ui.onrender.com")]
    [InlineData("http://localhost:4200", "http://localhost:4200")]
    [InlineData("", "http://localhost:4200")] // Should default to localhost when not configured
    public async Task SendAsync_UsesCorrectClientUrl_ForEmailTemplateData(string configuredUrl, string expectedUrl)
    {
        // Arrange
        _mockConfiguration.Setup(x => x["ClientUrl"]).Returns(string.IsNullOrEmpty(configuredUrl) ? null : configuredUrl);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Organization = organization
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var emailMessage = new EmailMessage
        {
            To = user.Email,
            Subject = "Test",
            Body = "Test Body",
            IsHtml = true
        };

        _mockTemplateService.Setup(x => x.RenderTemplate(
            It.IsAny<NotificationType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string>()))
            .Returns(emailMessage);

        _mockEmailService.Setup(x => x.SendSimpleEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()))
            .ReturnsAsync(true);

        var service = new NotificationService(
            _context,
            _mockTemplateService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        var request = new NotificationRequest
        {
            Type = NotificationType.NewProviderRequest,
            UserId = user.Id,
            Data = new Dictionary<string, string>()
        };

        // Act
        await service.SendAsync(request);

        // Assert
        _mockTemplateService.Verify(x => x.RenderTemplate(
            It.IsAny<NotificationType>(),
            It.Is<Dictionary<string, string>>(data =>
                data["LoginUrl"] == $"{expectedUrl}/login" &&
                data["PortalUrl"] == $"{expectedUrl}/owner/dashboard" &&
                data["ReviewUrl"] == $"{expectedUrl}/owner/providers"),
            It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_AddsDefaultDataToEmailTemplate()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["ClientUrl"]).Returns("https://test.com");

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "testuser@example.com",
            Organization = organization
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var emailMessage = new EmailMessage
        {
            To = user.Email,
            Subject = "Test",
            Body = "Test Body",
            IsHtml = true
        };

        _mockTemplateService.Setup(x => x.RenderTemplate(
            It.IsAny<NotificationType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<string>()))
            .Returns(emailMessage);

        _mockEmailService.Setup(x => x.SendSimpleEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()))
            .ReturnsAsync(true);

        var service = new NotificationService(
            _context,
            _mockTemplateService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        var request = new NotificationRequest
        {
            Type = NotificationType.NewProviderRequest,
            UserId = user.Id,
            Data = new Dictionary<string, string>()
        };

        // Act
        await service.SendAsync(request);

        // Assert
        _mockTemplateService.Verify(x => x.RenderTemplate(
            It.IsAny<NotificationType>(),
            It.Is<Dictionary<string, string>>(data =>
                data["UserName"] == "testuser" &&
                data["UserEmail"] == "testuser@example.com" &&
                data["OrganizationName"] == "Test Shop" &&
                data["ShopName"] == "Test Shop" &&
                data.ContainsKey("SubmittedAt")),
            It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_WhenUserNotFound()
    {
        // Arrange
        var service = new NotificationService(
            _context,
            _mockTemplateService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        var request = new NotificationRequest
        {
            Type = NotificationType.NewProviderRequest,
            UserId = Guid.NewGuid(), // Non-existent user
            Data = new Dictionary<string, string>()
        };

        // Act
        var result = await service.SendAsync(request);

        // Assert
        Assert.False(result);
        _mockEmailService.Verify(x => x.SendSimpleEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_RespectsUserEmailPreferences_WhenDisabled()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com"
        };

        var userPreference = new UserNotificationPreference
        {
            UserId = user.Id,
            NotificationType = NotificationType.NewProviderRequest,
            EmailEnabled = false
        };

        _context.Users.Add(user);
        _context.UserNotificationPreferences.Add(userPreference);
        await _context.SaveChangesAsync();

        var service = new NotificationService(
            _context,
            _mockTemplateService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        var request = new NotificationRequest
        {
            Type = NotificationType.NewProviderRequest,
            UserId = user.Id,
            Data = new Dictionary<string, string>()
        };

        // Act
        var result = await service.SendAsync(request);

        // Assert
        Assert.False(result); // Returns false when email disabled and no other channels succeed
        _mockEmailService.Verify(x => x.SendSimpleEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()),
            Times.Never);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}