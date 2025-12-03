using ConsignmentGenie.Application.Models.PaymentGateway;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace ConsignmentGenie.Application.Services;

public class StripePaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<StripePaymentGatewayService> _logger;
    private readonly IConfiguration _configuration;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly PaymentMethodService _paymentMethodService;
    private readonly CustomerService _customerService;
    private readonly RefundService _refundService;

    public string ConsignorName => "Stripe";

    public StripePaymentGatewayService(IConfiguration configuration, ILogger<StripePaymentGatewayService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Configure Stripe with secret key
        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("Stripe secret key not configured");
        }

        StripeConfiguration.ApiKey = secretKey;

        // Initialize Stripe services
        _paymentIntentService = new PaymentIntentService();
        _paymentMethodService = new PaymentMethodService();
        _customerService = new CustomerService();
        _refundService = new RefundService();
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Convert to cents
                Currency = request.Currency.ToLowerInvariant(),
                PaymentMethod = request.PaymentMethodId,
                Customer = request.CustomerId,
                Description = request.Description,
                Confirm = request.CaptureImmediately,
                Metadata = request.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value.ToString() ?? "")
            };

            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var requestOptions = new RequestOptions
                {
                    IdempotencyKey = request.IdempotencyKey
                };
                var paymentIntent = await _paymentIntentService.CreateAsync(options, requestOptions);
                return MapPaymentResult(paymentIntent);
            }
            else
            {
                var paymentIntent = await _paymentIntentService.CreateAsync(options);
                return MapPaymentResult(paymentIntent);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment processing failed for amount {Amount}", request.Amount);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.StripeError?.Code,
                Status = MapStripeStatus(ex.StripeError?.Type),
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<PaymentResult> CapturePaymentAsync(string paymentIntentId, decimal? amount = null)
    {
        try
        {
            var options = new PaymentIntentCaptureOptions();

            if (amount.HasValue)
            {
                options.AmountToCapture = (long)(amount.Value * 100);
            }

            var paymentIntent = await _paymentIntentService.CaptureAsync(paymentIntentId, options);
            return MapPaymentResult(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment capture failed for payment intent {PaymentIntentId}", paymentIntentId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.StripeError?.Code,
                Status = PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(RefundRequest request)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.TransactionId,
                Reason = request.Reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    "requested_by_customer" => "requested_by_customer",
                    _ => "requested_by_customer"
                },
                Metadata = request.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value.ToString() ?? "")
            };

            if (request.Amount.HasValue)
            {
                options.Amount = (long)(request.Amount.Value * 100);
            }

            Refund refund;
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var requestOptions = new RequestOptions
                {
                    IdempotencyKey = request.IdempotencyKey
                };
                refund = await _refundService.CreateAsync(options, requestOptions);
            }
            else
            {
                refund = await _refundService.CreateAsync(options);
            }

            return new PaymentResult
            {
                Success = true,
                TransactionId = refund.PaymentIntentId,
                GatewayTransactionId = refund.Id,
                AmountProcessed = refund.Amount / 100m,
                Status = refund.Status switch
                {
                    "succeeded" => PaymentStatus.Refunded,
                    "pending" => PaymentStatus.Processing,
                    "failed" => PaymentStatus.Failed,
                    _ => PaymentStatus.Failed
                },
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = new Dictionary<string, object>
                {
                    ["stripe_refund_id"] = refund.Id,
                    ["stripe_status"] = refund.Status,
                    ["stripe_charge_id"] = refund.ChargeId ?? ""
                }
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for transaction {TransactionId}", request.TransactionId);
            return new PaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.StripeError?.Code,
                Status = PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<string> CreatePaymentMethodAsync(string customerId, Dictionary<string, object> paymentMethodData)
    {
        try
        {
            var options = new PaymentMethodCreateOptions
            {
                Type = paymentMethodData.GetValueOrDefault("type", "card").ToString(),
                Customer = customerId
            };

            // Add card details if provided
            if (paymentMethodData.ContainsKey("card"))
            {
                var cardData = paymentMethodData["card"] as Dictionary<string, object>;
                if (cardData != null)
                {
                    options.Card = new PaymentMethodCardOptions
                    {
                        Number = cardData.GetValueOrDefault("number", "").ToString(),
                        ExpMonth = long.Parse(cardData.GetValueOrDefault("exp_month", "1").ToString() ?? "1"),
                        ExpYear = long.Parse(cardData.GetValueOrDefault("exp_year", "2025").ToString() ?? "2025"),
                        Cvc = cardData.GetValueOrDefault("cvc", "").ToString()
                    };
                }
            }

            var paymentMethod = await _paymentMethodService.CreateAsync(options);
            return paymentMethod.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe payment method for customer {CustomerId}", customerId);
            throw new InvalidOperationException($"Failed to create payment method: {ex.Message}");
        }
    }

    public async Task<List<PaymentMethodInfo>> GetPaymentMethodsAsync(string customerId)
    {
        try
        {
            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var paymentMethods = await _paymentMethodService.ListAsync(options);

            return paymentMethods.Select(pm => new PaymentMethodInfo
            {
                Id = pm.Id,
                Type = pm.Type,
                Last4 = pm.Card?.Last4 ?? "",
                Brand = pm.Card?.Brand ?? "",
                Name = pm.BillingDetails?.Name ?? "",
                ExpiryDate = pm.Card != null ? new DateTime((int)pm.Card.ExpYear, (int)pm.Card.ExpMonth, 1) : null,
                Metadata = pm.Metadata?.ToDictionary(kv => kv.Key, kv => (object)kv.Value) ?? new Dictionary<string, object>()
            }).ToList();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get payment methods for customer {CustomerId}", customerId);
            return new List<PaymentMethodInfo>();
        }
    }

    public async Task<bool> DeletePaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            await _paymentMethodService.DetachAsync(paymentMethodId);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to delete payment method {PaymentMethodId}", paymentMethodId);
            return false;
        }
    }

    public async Task<string> CreateCustomerAsync(string email, string? name = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = metadata?.ToDictionary(kv => kv.Key, kv => kv.Value.ToString() ?? "") ?? new Dictionary<string, string>()
            };

            var customer = await _customerService.CreateAsync(options);
            return customer.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe customer for email {Email}", email);
            throw new InvalidOperationException($"Failed to create customer: {ex.Message}");
        }
    }

    public async Task<bool> UpdateCustomerAsync(string customerId, Dictionary<string, object> updates)
    {
        try
        {
            var options = new CustomerUpdateOptions();

            if (updates.ContainsKey("email"))
                options.Email = updates["email"].ToString();

            if (updates.ContainsKey("name"))
                options.Name = updates["name"].ToString();

            if (updates.ContainsKey("metadata"))
                options.Metadata = (updates["metadata"] as Dictionary<string, object>)?.ToDictionary(kv => kv.Key, kv => kv.Value.ToString() ?? "");

            await _customerService.UpdateAsync(customerId, options);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to update customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> DeleteCustomerAsync(string customerId)
    {
        try
        {
            await _customerService.DeleteAsync(customerId);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to delete customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, secret);
            return stripeEvent != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook signature validation failed");
            return false;
        }
    }

    public async Task HandleWebhookAsync(string payload, Dictionary<string, object> headers)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            var signature = headers.GetValueOrDefault("Stripe-Signature", "").ToString();

            if (string.IsNullOrEmpty(webhookSecret) || string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe webhook received without proper signature or secret");
                return;
            }

            var isValid = await ValidateWebhookSignatureAsync(payload, signature, webhookSecret);
            if (!isValid)
            {
                _logger.LogWarning("Invalid Stripe webhook signature");
                return;
            }

            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);

            _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

            // Handle different event types
            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                case Events.PaymentIntentPaymentFailed:
                    // Handle payment events
                    break;
                default:
                    _logger.LogDebug("Unhandled Stripe webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
        }

        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetGatewaySpecificDataAsync(string transactionId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(transactionId);

            return new Dictionary<string, object>
            {
                ["stripe_payment_intent_id"] = paymentIntent.Id,
                ["stripe_status"] = paymentIntent.Status,
                ["stripe_client_secret"] = paymentIntent.ClientSecret ?? "",
                ["stripe_payment_method_id"] = paymentIntent.PaymentMethodId ?? "",
                ["stripe_customer_id"] = paymentIntent.CustomerId ?? "",
                ["stripe_amount"] = paymentIntent.Amount,
                ["stripe_currency"] = paymentIntent.Currency,
                ["stripe_created"] = paymentIntent.Created,
                ["stripe_metadata"] = paymentIntent.Metadata ?? new Dictionary<string, string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Stripe-specific data for transaction {TransactionId}", transactionId);
            return new Dictionary<string, object>();
        }
    }

    public async Task<bool> SupportsFunctionality(string functionality)
    {
        return functionality.ToLowerInvariant() switch
        {
            "basic_payment" => true,
            "recurring" => true,
            "marketplace" => true,
            "tokenization" => true,
            "refunds" => true,
            "webhooks" => true,
            "3d_secure" => true,
            "apple_pay" => true,
            "google_pay" => true,
            _ => false
        };
    }

    private PaymentResult MapPaymentResult(PaymentIntent paymentIntent)
    {
        return new PaymentResult
        {
            Success = paymentIntent.Status == "succeeded",
            TransactionId = paymentIntent.Id,
            GatewayTransactionId = paymentIntent.Id,
            AmountProcessed = paymentIntent.Amount / 100m,
            Status = paymentIntent.Status switch
            {
                "requires_payment_method" => PaymentStatus.RequiresPaymentMethod,
                "requires_confirmation" => PaymentStatus.RequiresAction,
                "requires_action" => PaymentStatus.RequiresAction,
                "processing" => PaymentStatus.Processing,
                "requires_capture" => PaymentStatus.Pending,
                "canceled" => PaymentStatus.Cancelled,
                "succeeded" => PaymentStatus.Succeeded,
                _ => PaymentStatus.Failed
            },
            ProcessedAt = DateTime.UtcNow,
            GatewayResponse = new Dictionary<string, object>
            {
                ["stripe_payment_intent_id"] = paymentIntent.Id,
                ["stripe_status"] = paymentIntent.Status,
                ["stripe_client_secret"] = paymentIntent.ClientSecret ?? "",
                ["stripe_payment_method_id"] = paymentIntent.PaymentMethodId ?? ""
            }
        };
    }

    private PaymentStatus MapStripeStatus(string? errorType)
    {
        return errorType switch
        {
            "card_error" => PaymentStatus.RequiresPaymentMethod,
            "authentication_error" => PaymentStatus.RequiresAction,
            _ => PaymentStatus.Failed
        };
    }
}