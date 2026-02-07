using ConsignmentGenie.Application.DTOs.Storefront;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IStripePaymentService
{
    Task<PaymentIntentDto> CreatePaymentIntentAsync(decimal amount, string currency = "usd", string? description = null);
    Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId);
    Task<bool> CancelPaymentIntentAsync(string paymentIntentId);
    Task<string> GetPaymentIntentStatusAsync(string paymentIntentId);
}