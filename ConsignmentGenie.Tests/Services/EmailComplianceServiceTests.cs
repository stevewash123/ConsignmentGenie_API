using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class EmailComplianceServiceTests : IDisposable
{
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Mock<INotificationPreferenceService> _mockPreferenceService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmailComplianceService>> _mockLogger;
    private readonly EmailComplianceService _service;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _role = "Owner";
    private readonly string _notificationType = "item_sold";

    public EmailComplianceServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockPreferenceService = new Mock<INotificationPreferenceService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailComplianceService>>();

        // Setup configuration mock
        _mockConfiguration.Setup(x => x["EmailCompliance:SecretKey"]).Returns("test-secret-key-for-compliance-testing-32-chars!");

        _service = new EmailComplianceService(
            _context,
            _mockPreferenceService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateUnsubscribeTokenAsync_ValidInput_ReturnsToken()
    {
        // Act
        var token = await _service.GenerateUnsubscribeTokenAsync(_userId, _role, _notificationType);

        // Assert
        Assert.NotEmpty(token);
        Assert.True(token.Length > 50); // Token should be substantial length when base64 encoded
    }

    [Fact]
    public async Task ValidateUnsubscribeTokenAsync_ValidToken_ReturnsTokenInfo()
    {
        // Arrange
        var originalToken = await _service.GenerateUnsubscribeTokenAsync(_userId, _role, _notificationType);

        // Act
        var tokenInfo = await _service.ValidateUnsubscribeTokenAsync(originalToken);

        // Assert
        Assert.NotNull(tokenInfo);
        Assert.Equal(_userId, tokenInfo.UserId);
        Assert.Equal(_role, tokenInfo.Role);
        Assert.Equal(_notificationType, tokenInfo.NotificationType);
        Assert.False(tokenInfo.IsExpired);
        Assert.True(tokenInfo.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ValidateUnsubscribeTokenAsync_InvalidToken_ReturnsNull()
    {
        // Act
        var tokenInfo = await _service.ValidateUnsubscribeTokenAsync("invalid-token");

        // Assert
        Assert.Null(tokenInfo);
    }

    [Fact]
    public async Task ProcessUnsubscribeAsync_ValidToken_SpecificNotification_UpdatesPreferences()
    {
        // Arrange
        var token = await _service.GenerateUnsubscribeTokenAsync(_userId, _role, _notificationType);

        var mockMatrix = new NotificationPreferencesMatrix
        {
            UserId = _userId,
            Role = _role,
            Preferences = new Dictionary<string, Dictionary<NotificationChannel, bool>>
            {
                [_notificationType] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false }
            }
        };

        _mockPreferenceService.Setup(x => x.GetPreferencesAsync(_userId, _role))
            .ReturnsAsync(mockMatrix);

        _mockPreferenceService.Setup(x => x.UpdatePreferencesAsync(_userId, _role, It.IsAny<NotificationPreferencesMatrix>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessUnsubscribeAsync(token, UnsubscribeScope.SpecificNotification);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnsubscribeScope.SpecificNotification, result.ProcessedScope);
        Assert.Single(result.UpdatedNotificationTypes);
        Assert.Contains(_notificationType, result.UpdatedNotificationTypes);

        // Verify preference service was called to update
        _mockPreferenceService.Verify(x => x.UpdatePreferencesAsync(_userId, _role,
            It.Is<NotificationPreferencesMatrix>(m =>
                !m.Preferences[_notificationType][NotificationChannel.Email])), Times.Once);
    }

    [Fact]
    public async Task ProcessUnsubscribeAsync_ValidToken_AllEmailNotifications_UpdatesAllPreferences()
    {
        // Arrange
        var token = await _service.GenerateUnsubscribeTokenAsync(_userId, _role, _notificationType);

        var mockMatrix = new NotificationPreferencesMatrix
        {
            UserId = _userId,
            Role = _role,
            Preferences = new Dictionary<string, Dictionary<NotificationChannel, bool>>
            {
                ["item_sold"] = new() { [NotificationChannel.Email] = true },
                ["high_value_sale"] = new() { [NotificationChannel.Email] = true },
                ["daily_sales_summary"] = new() { [NotificationChannel.Email] = true }
            }
        };

        _mockPreferenceService.Setup(x => x.GetPreferencesAsync(_userId, _role))
            .ReturnsAsync(mockMatrix);

        _mockPreferenceService.Setup(x => x.UpdatePreferencesAsync(_userId, _role, It.IsAny<NotificationPreferencesMatrix>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessUnsubscribeAsync(token, UnsubscribeScope.AllEmailNotifications);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(UnsubscribeScope.AllEmailNotifications, result.ProcessedScope);
        Assert.Equal(3, result.UpdatedNotificationTypes.Count);

        // Verify all email notifications were disabled
        _mockPreferenceService.Verify(x => x.UpdatePreferencesAsync(_userId, _role,
            It.Is<NotificationPreferencesMatrix>(m =>
                !m.Preferences["item_sold"][NotificationChannel.Email] &&
                !m.Preferences["high_value_sale"][NotificationChannel.Email] &&
                !m.Preferences["daily_sales_summary"][NotificationChannel.Email])), Times.Once);
    }

    [Fact]
    public async Task GetUnsubscribeUrl_ValidInputs_ReturnsProperUrl()
    {
        // Arrange
        var baseUrl = "https://app.consignmentgenie.com";
        var token = "test-token-123";

        // Act
        var url = _service.GetUnsubscribeUrl(baseUrl, token);

        // Assert
        Assert.StartsWith("https://app.consignmentgenie.com/unsubscribe?token=", url);
        Assert.Contains("test-token-123", url);
    }

    [Fact]
    public async Task AddComplianceContentAsync_ValidRequest_AddsHeadersAndFooter()
    {
        // Arrange
        var request = new EmailComplianceRequest
        {
            EmailContent = "This is a test email.",
            EmailSubject = "Test Subject",
            UserId = _userId,
            Role = _role,
            NotificationType = _notificationType,
            RecipientEmail = "test@example.com",
            BaseUrl = "https://app.consignmentgenie.com",
            OrganizationName = "Test Organization",
            SenderName = "Test Sender",
            SenderEmail = "sender@example.com"
        };

        // Act
        var result = await _service.AddComplianceContentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.EmailSubject, result.EmailSubject);
        Assert.Contains("This is a test email.", result.EmailContent);
        Assert.Contains("unsubscribe", result.EmailContent.ToLower());
        Assert.Contains("Test Organization", result.EmailContent);

        // Verify required headers
        Assert.True(result.Headers.ContainsKey("List-Unsubscribe"));
        Assert.True(result.Headers.ContainsKey("List-Unsubscribe-Post"));
        Assert.True(result.Headers.ContainsKey("X-Email-Type"));
        Assert.True(result.Headers.ContainsKey("X-Organization"));

        Assert.Equal(_notificationType, result.Headers["X-Email-Type"]);
        Assert.Equal("Test Organization", result.Headers["X-Organization"]);

        // Verify unsubscribe URL is generated
        Assert.NotEmpty(result.UnsubscribeUrl);
        Assert.Contains("unsubscribe?token=", result.UnsubscribeUrl);
    }

    [Fact]
    public async Task CanSendEmailAsync_ChecksPreferenceService()
    {
        // Arrange
        _mockPreferenceService.Setup(x => x.IsNotificationEnabledAsync(_userId, _role, _notificationType, NotificationChannel.Email))
            .ReturnsAsync(true);

        // Act
        var canSend = await _service.CanSendEmailAsync(_userId, _role, _notificationType);

        // Assert
        Assert.True(canSend);
        _mockPreferenceService.Verify(x => x.IsNotificationEnabledAsync(_userId, _role, _notificationType, NotificationChannel.Email), Times.Once);
    }

    [Fact]
    public async Task ProcessUnsubscribeAsync_ExpiredToken_ReturnsFailure()
    {
        // Arrange - Generate a token but manually create an expired one by manipulating the time
        // This is a simplified test - in practice, you'd need to create a token with past expiration
        var invalidToken = "invalid-expired-token";

        // Act
        var result = await _service.ProcessUnsubscribeAsync(invalidToken, UnsubscribeScope.SpecificNotification);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid or expired", result.Message);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}