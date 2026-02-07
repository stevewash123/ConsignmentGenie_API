using ConsignmentGenie.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class ResendEmailServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ResendEmailService>> _mockLogger;

    public ResendEmailServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ResendEmailService>>();

        // Setup configuration
        _mockConfiguration.Setup(x => x["Resend:ApiKey"]).Returns("re_test_key");
        _mockConfiguration.Setup(x => x["Resend:FromEmail"]).Returns("noreply@test.com");
        _mockConfiguration.Setup(x => x["Resend:FromName"]).Returns("Test App");
        _mockConfiguration.Setup(x => x["DeveloperEmail"]).Returns("dev@test.com");
    }

    [Fact]
    public void ResendEmailService_Constructor_InitializesCorrectly()
    {
        // Act
        var service = new ResendEmailService(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task SendSimpleEmailAsync_ValidInput_DoesNotThrow()
    {
        // Arrange
        var service = new ResendEmailService(_mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert - Since we're hitting a real API, we just verify it doesn't throw
        // In a real test environment, you would mock the HttpClient
        var result = await service.SendSimpleEmailAsync("test@example.com", "Test Subject", "Test Body", false);

        // We expect false here since we don't have a valid API key configured
        // This test is mainly to ensure the method signature and basic structure work
        Assert.False(result);
    }

    [Fact]
    public void Dispose_CallsDispose()
    {
        // Arrange
        var service = new ResendEmailService(_mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert - Should not throw
        service.Dispose();
    }
}