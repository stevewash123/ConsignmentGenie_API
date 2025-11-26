using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace ConsignmentGenie.Application.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Initialize Stripe with API key
        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(stripeSecretKey))
        {
            StripeConfiguration.ApiKey = stripeSecretKey;
        }
    }

    public async Task<PaymentIntentDto> CreatePaymentIntentAsync(decimal amount, string currency = "usd", string? description = null)
    {
        _logger.LogInformation("Creating payment intent for amount {Amount} {Currency}", amount, currency);

        // Validate Stripe configuration
        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(stripeSecretKey))
        {
            _logger.LogError("Stripe secret key not configured");
            throw new InvalidOperationException("Stripe payment service is not properly configured");
        }

        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency,
                Description = description ?? "ConsignmentGenie Purchase",
                PaymentMethodTypes = new List<string> { "card" },
                CaptureMethod = "automatic",
                ConfirmationMethod = "automatic"
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Stripe payment intent created successfully: {PaymentIntentId}", paymentIntent.Id);

            return new PaymentIntentDto
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Amount = amount
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe payment intent: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException($"Payment processing failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating payment intent");
            throw new InvalidOperationException("Payment processing failed due to an unexpected error", ex);
        }
    }

    public async Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId)
    {
        _logger.LogInformation("Confirming payment intent {PaymentIntentId}", paymentIntentId);

        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(stripeSecretKey))
        {
            _logger.LogError("Stripe secret key not configured");
            throw new InvalidOperationException("Stripe payment service is not properly configured");
        }

        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            _logger.LogInformation("Payment intent {PaymentIntentId} status: {Status}", paymentIntentId, paymentIntent.Status);

            return paymentIntent.Status == "succeeded";
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to confirm Stripe payment intent {PaymentIntentId}: {ErrorMessage}", paymentIntentId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error confirming payment intent {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<bool> CancelPaymentIntentAsync(string paymentIntentId)
    {
        _logger.LogInformation("Canceling payment intent {PaymentIntentId}", paymentIntentId);

        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(stripeSecretKey))
        {
            _logger.LogError("Stripe secret key not configured");
            throw new InvalidOperationException("Stripe payment service is not properly configured");
        }

        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.CancelAsync(paymentIntentId);

            _logger.LogInformation("Payment intent {PaymentIntentId} canceled successfully, status: {Status}", paymentIntentId, paymentIntent.Status);

            return paymentIntent.Status == "canceled";
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel Stripe payment intent {PaymentIntentId}: {ErrorMessage}", paymentIntentId, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error canceling payment intent {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<string> GetPaymentIntentStatusAsync(string paymentIntentId)
    {
        _logger.LogInformation("Getting payment intent status for {PaymentIntentId}", paymentIntentId);

        var stripeSecretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(stripeSecretKey))
        {
            _logger.LogError("Stripe secret key not configured");
            throw new InvalidOperationException("Stripe payment service is not properly configured");
        }

        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            _logger.LogInformation("Payment intent {PaymentIntentId} status retrieved: {Status}", paymentIntentId, paymentIntent.Status);

            return paymentIntent.Status;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe payment intent status {PaymentIntentId}: {ErrorMessage}", paymentIntentId, ex.Message);
            throw new InvalidOperationException($"Failed to retrieve payment status: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting payment intent status {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException("Failed to retrieve payment status due to an unexpected error", ex);
        }
    }
}