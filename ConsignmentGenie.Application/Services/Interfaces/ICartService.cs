using ConsignmentGenie.Application.DTOs.Storefront;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid organizationId, string? sessionId, Guid? customerId);
    Task<CartDto> AddItemToCartAsync(Guid organizationId, Guid itemId, string? sessionId, Guid? customerId);
    Task<CartDto> RemoveItemFromCartAsync(Guid organizationId, Guid itemId, string? sessionId, Guid? customerId);
    Task<bool> ClearCartAsync(Guid organizationId, string? sessionId, Guid? customerId);
    Task<CartDto> MergeCartAsync(Guid organizationId, string sessionId, Guid customerId);
    Task CleanupExpiredCartsAsync();
    Task<bool> IsItemInCartAsync(Guid organizationId, Guid itemId);
}