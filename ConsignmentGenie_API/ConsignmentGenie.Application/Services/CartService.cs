using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class CartService : ICartService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<CartService> _logger;

    public CartService(ConsignmentGenieContext context, ILogger<CartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(Guid organizationId, string? sessionId, Guid? customerId)
    {
        var cart = await FindOrCreateCartAsync(organizationId, sessionId, customerId);
        return await MapToCartDtoAsync(cart);
    }

    public async Task<CartDto> AddItemToCartAsync(Guid organizationId, Guid itemId, string? sessionId, Guid? customerId)
    {
        // Check if item exists and is available
        var item = await _context.Items
            .FirstOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == organizationId);

        if (item == null)
        {
            throw new ArgumentException("Item not found");
        }

        if (item.Status != ItemStatus.Available)
        {
            throw new InvalidOperationException("Item is not available for purchase");
        }

        // Check if item is already in another cart (reservation)
        var isReserved = await IsItemInCartAsync(organizationId, itemId);
        if (isReserved)
        {
            throw new InvalidOperationException("Item is already reserved in another cart");
        }

        var cart = await FindOrCreateCartAsync(organizationId, sessionId, customerId);

        // Check if item is already in this cart
        var existingCartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == itemId);

        if (existingCartItem == null)
        {
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ItemId = itemId,
                AddedAt = DateTime.UtcNow
            };

            _context.CartItems.Add(cartItem);
            cart.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return await MapToCartDtoAsync(cart);
    }

    public async Task<CartDto> RemoveItemFromCartAsync(Guid organizationId, Guid itemId, string? sessionId, Guid? customerId)
    {
        var cart = await FindCartAsync(organizationId, sessionId, customerId);

        if (cart != null)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ItemId == itemId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                cart.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return await MapToCartDtoAsync(cart);
        }

        return new CartDto();
    }

    public async Task<bool> ClearCartAsync(Guid organizationId, string? sessionId, Guid? customerId)
    {
        var cart = await FindCartAsync(organizationId, sessionId, customerId);

        if (cart != null)
        {
            var cartItems = await _context.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<CartDto> MergeCartAsync(Guid organizationId, string sessionId, Guid customerId)
    {
        var anonymousCart = await _context.ShoppingCarts
            .Include(sc => sc.CartItems)
            .ThenInclude(ci => ci.Item)
            .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId && sc.SessionId == sessionId);

        var userCart = await _context.ShoppingCarts
            .Include(sc => sc.CartItems)
            .ThenInclude(ci => ci.Item)
            .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId && sc.CustomerId == customerId);

        if (anonymousCart == null)
        {
            return userCart != null ? await MapToCartDtoAsync(userCart) : new CartDto();
        }

        if (userCart == null)
        {
            // Convert anonymous cart to user cart
            anonymousCart.CustomerId = customerId;
            anonymousCart.SessionId = null;
            anonymousCart.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await MapToCartDtoAsync(anonymousCart);
        }

        // Merge anonymous cart items into user cart
        foreach (var anonymousItem in anonymousCart.CartItems)
        {
            var existingItem = userCart.CartItems.FirstOrDefault(ci => ci.ItemId == anonymousItem.ItemId);
            if (existingItem == null)
            {
                anonymousItem.CartId = userCart.Id;
            }
        }

        // Remove anonymous cart
        _context.ShoppingCarts.Remove(anonymousCart);
        userCart.LastUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await MapToCartDtoAsync(userCart);
    }

    public async Task CleanupExpiredCartsAsync()
    {
        var expiredCarts = await _context.ShoppingCarts
            .Where(sc => sc.ExpiresAt.HasValue && sc.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredCarts.Any())
        {
            _context.ShoppingCarts.RemoveRange(expiredCarts);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired carts", expiredCarts.Count);
        }
    }

    public async Task<bool> IsItemInCartAsync(Guid organizationId, Guid itemId)
    {
        return await _context.CartItems
            .AnyAsync(ci => ci.Item.OrganizationId == organizationId && ci.ItemId == itemId);
    }

    private async Task<ShoppingCart> FindOrCreateCartAsync(Guid organizationId, string? sessionId, Guid? customerId)
    {
        var cart = await FindCartAsync(organizationId, sessionId, customerId);

        if (cart == null)
        {
            cart = new ShoppingCart
            {
                OrganizationId = organizationId,
                SessionId = sessionId,
                CustomerId = customerId,
                LastUpdatedAt = DateTime.UtcNow,
                ExpiresAt = sessionId != null ? DateTime.UtcNow.AddDays(7) : null // Only expire anonymous carts
            };

            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    private async Task<ShoppingCart?> FindCartAsync(Guid organizationId, string? sessionId, Guid? customerId)
    {
        if (customerId.HasValue)
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                .ThenInclude(ci => ci.Item)
                .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId && sc.CustomerId == customerId);
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.CartItems)
                .ThenInclude(ci => ci.Item)
                .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId && sc.SessionId == sessionId);
        }

        return null;
    }

    private async Task<CartDto> MapToCartDtoAsync(ShoppingCart cart)
    {
        // Refresh cart with items
        var cartWithItems = await _context.ShoppingCarts
            .Include(sc => sc.CartItems)
            .ThenInclude(ci => ci.Item)
            .FirstOrDefaultAsync(sc => sc.Id == cart.Id);

        if (cartWithItems == null)
        {
            return new CartDto();
        }

        var cartDto = new CartDto
        {
            Id = cartWithItems.Id,
            ItemCount = cartWithItems.CartItems.Count,
            Subtotal = cartWithItems.CartItems.Sum(ci => ci.Item.Price),
            Items = cartWithItems.CartItems.Select(ci => new CartItemDto
            {
                ItemId = ci.ItemId,
                Name = ci.Item.Title,
                ImageUrl = ci.Item.PrimaryImageUrl,
                Category = ci.Item.Category,
                Price = ci.Item.Price,
                IsAvailable = ci.Item.Status == ItemStatus.Available,
                AddedAt = ci.AddedAt
            }).ToList()
        };

        // Get tax rate from organization settings (simplified - assume 8.5% for now)
        var taxRate = 0.085m;
        cartDto.EstimatedTax = cartDto.Subtotal * taxRate;
        cartDto.EstimatedTotal = cartDto.Subtotal + cartDto.EstimatedTax;

        return cartDto;
    }
}