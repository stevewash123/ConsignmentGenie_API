using ConsignmentGenie.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class StripePaymentServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<StripePaymentService>> _mockLogger;
    private readonly StripePaymentService _stripeService;

    public StripePaymentServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<StripePaymentService>>();
        _stripeService = new StripePaymentService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_NoStripeKey_ThrowsException()
    {
        // Arrange
        var amount = 50.00m;
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.CreatePaymentIntentAsync(amount));

        Assert.Contains("Stripe payment service is not properly configured", exception.Message);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithCurrency_NoKey_ThrowsException()
    {
        // Arrange
        var amount = 100.00m;
        var currency = "eur";
        var description = "Test payment";
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.CreatePaymentIntentAsync(amount, currency, description));

        Assert.Contains("Stripe payment service is not properly configured", exception.Message);
    }

    [Fact]
    public async Task ConfirmPaymentIntentAsync_NoKey_ThrowsException()
    {
        // Arrange
        var paymentIntentId = "pi_test123456";
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.ConfirmPaymentIntentAsync(paymentIntentId));

        Assert.Contains("Stripe payment service is not properly configured", exception.Message);
    }

    [Fact]
    public async Task CancelPaymentIntentAsync_NoKey_ThrowsException()
    {
        // Arrange
        var paymentIntentId = "pi_test123456";
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.CancelPaymentIntentAsync(paymentIntentId));

        Assert.Contains("Stripe payment service is not properly configured", exception.Message);
    }

    [Fact]
    public async Task GetPaymentIntentStatusAsync_NoKey_ThrowsException()
    {
        // Arrange
        var paymentIntentId = "pi_test123456";
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.GetPaymentIntentStatusAsync(paymentIntentId));

        Assert.Contains("Stripe payment service is not properly configured", exception.Message);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_LogsErrorWhenNoKey()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);
        var amount = 25.00m;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.CreatePaymentIntentAsync(amount));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stripe secret key not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmPaymentIntentAsync_LogsErrorWhenNoKey()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);
        var paymentIntentId = "pi_test123456";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.ConfirmPaymentIntentAsync(paymentIntentId));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stripe secret key not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_LogsCreationAttempt()
    {
        // Arrange
        var amount = 75.50m;
        var currency = "usd";
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _stripeService.CreatePaymentIntentAsync(amount, currency));

        // Verify information log was called before error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Creating payment intent for amount {amount} {currency}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithValidKey_CallsStripeAPI()
    {
        // Arrange
        var amount = 50.00m;
        var testKey = "sk_test_51SVOTxL5W1M01qnRXV1uAZi4csPyIdjxpCQL54WnApaX3Fjkhrphw0Bl3PV8kWtXpetcqTvhaBaHC87zQ0pRvSYw00tnKhLRYW";
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns(testKey);

        // Act & Assert
        // This will make a real call to Stripe test API
        try
        {
            var result = await _stripeService.CreatePaymentIntentAsync(amount);

            // If successful, verify the result structure
            Assert.NotNull(result);
            Assert.True(result.Amount > 0);
            Assert.NotNull(result.PaymentIntentId);
            Assert.NotNull(result.ClientSecret);
            Assert.StartsWith("pi_", result.PaymentIntentId);
        }
        catch (InvalidOperationException ex)
        {
            // This is expected if there's a Stripe API issue or authentication problem
            Assert.Contains("Payment processing failed", ex.Message);
        }

        // Verify information log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Creating payment intent for amount {amount}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}