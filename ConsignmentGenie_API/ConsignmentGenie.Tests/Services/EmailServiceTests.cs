using ConsignmentGenie.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SendGrid;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<ISendGridClient> _mockSendGridClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _mockSendGridClient = new Mock<ISendGridClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailService>>();

        // Setup configuration
        var templatesSection = new Mock<IConfigurationSection>();
        templatesSection.Setup(x => x["Welcome"]).Returns("d-welcome123");
        templatesSection.Setup(x => x["TrialExpiring"]).Returns("d-trial123");
        templatesSection.Setup(x => x["PaymentFailed"]).Returns("d-payment123");

        _mockConfiguration.Setup(x => x.GetSection("SendGrid:Templates")).Returns(templatesSection.Object);
        _mockConfiguration.Setup(x => x["SendGrid:FromEmail"]).Returns("noreply@test.com");
        _mockConfiguration.Setup(x => x["SendGrid:FromName"]).Returns("Test App");

        _emailService = new EmailService(_mockSendGridClient.Object, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_ValidInput_ReturnsTrue()
    {
        // Arrange
        var response = new Mock<Response>();
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);

        _mockSendGridClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default))
            .ReturnsAsync(response.Object);

        // Act
        var result = await _emailService.SendWelcomeEmailAsync("test@example.com", "Test Org");

        // Assert
        Assert.True(result);
        _mockSendGridClient.Verify(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task SendTrialExpiringEmailAsync_ValidInput_ReturnsTrue()
    {
        // Arrange
        var response = new Mock<Response>();
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);

        _mockSendGridClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default))
            .ReturnsAsync(response.Object);

        // Act
        var result = await _emailService.SendTrialExpiringEmailAsync("test@example.com", 3);

        // Assert
        Assert.True(result);
        _mockSendGridClient.Verify(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task SendPaymentFailedEmailAsync_ValidInput_ReturnsTrue()
    {
        // Arrange
        var response = new Mock<Response>();
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);

        _mockSendGridClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default))
            .ReturnsAsync(response.Object);

        // Act
        var result = await _emailService.SendPaymentFailedEmailAsync("test@example.com", 99.99m, DateTime.UtcNow.AddDays(3));

        // Assert
        Assert.True(result);
        _mockSendGridClient.Verify(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task SendEmail_SendGridFailure_ReturnsFalse()
    {
        // Arrange
        var response = new Mock<Response>();
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);

        var bodyContent = new System.Net.Http.StringContent("Error message");
        response.Setup(r => r.Body).Returns(bodyContent);

        _mockSendGridClient
            .Setup(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default))
            .ReturnsAsync(response.Object);

        // Act
        var result = await _emailService.SendWelcomeEmailAsync("test@example.com", "Test Org");

        // Assert
        Assert.False(result);
        _mockSendGridClient.Verify(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task SendEmail_MissingTemplate_ReturnsFalse()
    {
        // Arrange - Clear template configuration
        var templatesSection = new Mock<IConfigurationSection>();
        templatesSection.Setup(x => x["Welcome"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x.GetSection("SendGrid:Templates")).Returns(templatesSection.Object);

        var emailService = new EmailService(_mockSendGridClient.Object, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await emailService.SendWelcomeEmailAsync("test@example.com", "Test Org");

        // Assert
        Assert.False(result);
        _mockSendGridClient.Verify(x => x.SendEmailAsync(It.IsAny<SendGrid.Helpers.Mail.SendGridMessage>(), default), Times.Never);
    }
}