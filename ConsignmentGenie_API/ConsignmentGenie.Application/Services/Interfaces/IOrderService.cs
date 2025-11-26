using ConsignmentGenie.Application.DTOs.Storefront;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IOrderService
{
    Task<CheckoutValidationDto> ValidateCartForCheckoutAsync(Guid organizationId, string? sessionId, Guid? customerId);
    Task<PaymentIntentDto> CreatePaymentIntentAsync(Guid organizationId, CheckoutRequestDto request, string? sessionId, Guid? customerId);
    Task<OrderDto> CreateOrderAsync(Guid organizationId, CheckoutRequestDto request, string? sessionId, Guid? customerId);
    Task<OrderDto?> GetOrderAsync(Guid organizationId, Guid orderId);
    Task<List<OrderSummaryDto>> GetOrdersAsync(Guid organizationId, Guid customerId, int page = 1, int pageSize = 10);
    Task<bool> UpdateOrderStatusAsync(Guid organizationId, Guid orderId, string status);
    Task<bool> ProcessPaymentConfirmationAsync(Guid organizationId, string paymentIntentId);
}