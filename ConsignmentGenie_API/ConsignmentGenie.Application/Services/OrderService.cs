using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class OrderService : IOrderService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ICartService _cartService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ConsignmentGenieContext context,
        ICartService cartService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _cartService = cartService;
        _logger = logger;
    }

    public async Task<CheckoutValidationDto> ValidateCartForCheckoutAsync(Guid organizationId, string? sessionId, Guid? customerId)
    {
        var cart = await FindCartAsync(organizationId, sessionId, customerId);

        if (cart == null || !cart.CartItems.Any())
        {
            return new CheckoutValidationDto
            {
                Valid = false,
                ErrorMessage = "Cart is empty"
            };
        }

        var unavailableItems = new List<Guid>();

        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Item.Status != ItemStatus.Available)
            {
                unavailableItems.Add(cartItem.ItemId);
            }
        }

        return new CheckoutValidationDto
        {
            Valid = !unavailableItems.Any(),
            UnavailableItems = unavailableItems,
            ErrorMessage = unavailableItems.Any() ? "Some items in your cart are no longer available" : null
        };
    }

    public async Task<PaymentIntentDto> CreatePaymentIntentAsync(Guid organizationId, CheckoutRequestDto request, string? sessionId, Guid? customerId)
    {
        var validation = await ValidateCartForCheckoutAsync(organizationId, sessionId, customerId);
        if (!validation.Valid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Cart validation failed");
        }

        var cart = await FindCartAsync(organizationId, sessionId, customerId);
        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found");
        }

        var organization = await _context.Organizations.FindAsync(organizationId);
        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        var subtotal = cart.CartItems.Sum(ci => ci.Item.Price);
        var taxRate = 0.085m; // TODO: Get from organization settings
        var tax = subtotal * taxRate;
        var shippingAmount = request.FulfillmentType == "shipping" ? 10.00m : 0m; // TODO: Calculate actual shipping
        var total = subtotal + tax + shippingAmount;

        // TODO: Replace with actual Stripe integration
        var paymentIntentId = $"pi_{Guid.NewGuid():N}";
        var clientSecret = $"pi_{Guid.NewGuid():N}_secret_test";

        return new PaymentIntentDto
        {
            PaymentIntentId = paymentIntentId,
            ClientSecret = clientSecret,
            Amount = total
        };
    }

    public async Task<OrderDto> CreateOrderAsync(Guid organizationId, CheckoutRequestDto request, string? sessionId, Guid? customerId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var validation = await ValidateCartForCheckoutAsync(organizationId, sessionId, customerId);
        if (!validation.Valid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Cart validation failed");
        }

        var cart = await FindCartAsync(organizationId, sessionId, customerId);
        if (cart == null)
        {
            throw new InvalidOperationException("Cart not found");
        }

        var organization = await _context.Organizations.FindAsync(organizationId);
        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        // Validate shipping address requirement
        if (request.FulfillmentType == "shipping" && request.ShippingAddress == null)
        {
            throw new InvalidOperationException("Shipping address is required for shipping orders");
        }

        // Calculate totals
        var subtotal = cart.CartItems.Sum(ci => ci.Item.Price);
        var taxRate = 0.085m; // TODO: Get from organization settings
        var tax = subtotal * taxRate;
        var shippingAmount = request.FulfillmentType == "shipping" ? 10.00m : 0m; // TODO: Calculate actual shipping
        var total = subtotal + tax + shippingAmount;

        // 🏗️ AGGREGATE ROOT PATTERN: Create order aggregate root
        var order = new Order
        {
            OrganizationId = organizationId,
            CustomerId = customerId,
            OrderNumber = await GenerateOrderNumberAsync(organizationId),
            Status = OrderStatus.Pending,

            // Customer information
            CustomerEmail = request.Email,
            CustomerName = request.Name,
            CustomerPhone = request.Phone,

            // Fulfillment
            FulfillmentType = request.FulfillmentType,
            ShippingAddress1 = request.ShippingAddress?.Address1,
            ShippingAddress2 = request.ShippingAddress?.Address2,
            ShippingCity = request.ShippingAddress?.City,
            ShippingState = request.ShippingAddress?.State,
            ShippingZip = request.ShippingAddress?.Zip,
            ShippingCountry = request.ShippingAddress?.Country ?? "US",

            // Totals
            Subtotal = subtotal,
            TaxAmount = tax,
            ShippingAmount = shippingAmount,
            TotalAmount = total,

            // Payment
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "pending",

            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // 🏗️ AGGREGATE ROOT PATTERN: Create order items and update item status within aggregate
        foreach (var cartItem in cart.CartItems)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ItemId = cartItem.ItemId,
                ItemName = cartItem.Item.Title,
                ItemPrice = cartItem.Item.Price
            };

            _context.OrderItems.Add(orderItem);

            // Mark item as sold
            cartItem.Item.Status = ItemStatus.Sold;
            cartItem.Item.SoldDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await _context.SaveChangesAsync();

        // Clear the cart
        await _cartService.ClearCartAsync(organizationId, sessionId, customerId);

        return await MapToOrderDtoAsync(order);
    }

    public async Task<OrderDto?> GetOrderAsync(Guid organizationId, Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Item)
            .FirstOrDefaultAsync(o => o.OrganizationId == organizationId && o.Id == orderId);

        return order != null ? await MapToOrderDtoAsync(order) : null;
    }

    public async Task<List<OrderSummaryDto>> GetOrdersAsync(Guid organizationId, Guid customerId, int page = 1, int pageSize = 10)
    {
        var orders = await _context.Orders
            .Where(o => o.OrganizationId == organizationId && o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderItems.Count,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return orders;
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid organizationId, Guid orderId, string status)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrganizationId == organizationId && o.Id == orderId);

        if (order == null)
        {
            return false;
        }

        if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            order.Status = orderStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> ProcessPaymentConfirmationAsync(Guid organizationId, string paymentIntentId)
    {
        // TODO: Verify payment with Stripe API

        // For now, find order by payment intent ID (would need to add field to Order entity)
        // This is a placeholder implementation
        _logger.LogInformation("Payment confirmation received for payment intent: {PaymentIntentId}", paymentIntentId);

        // Update order payment status to completed
        // var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);
        // if (order != null)
        // {
        //     order.PaymentStatus = "completed";
        //     if (order.Status == OrderStatus.Pending)
        //     {
        //         order.Status = OrderStatus.Confirmed;
        //     }
        //     await _context.SaveChangesAsync();
        //     return true;
        // }

        return true; // Placeholder return
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

    private async Task<string> GenerateOrderNumberAsync(Guid organizationId)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var orderCount = await _context.Orders
            .Where(o => o.OrganizationId == organizationId && o.CreatedAt.Date == DateTime.UtcNow.Date)
            .CountAsync();

        return $"{today}-{(orderCount + 1):D3}";
    }

    private async Task<OrderDto> MapToOrderDtoAsync(Order order)
    {
        // Refresh order with items if not already loaded
        if (!_context.Entry(order).Collection(o => o.OrderItems).IsLoaded)
        {
            await _context.Entry(order)
                .Collection(o => o.OrderItems)
                .LoadAsync();
        }

        var orderDto = new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            FulfillmentType = order.FulfillmentType,
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            ShippingAmount = order.ShippingAmount,
            TotalAmount = order.TotalAmount,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                ItemId = oi.ItemId,
                Name = oi.ItemName,
                Price = oi.ItemPrice,
                ImageUrl = oi.Item?.PrimaryImageUrl
            }).ToList()
        };

        // Parse shipping address if exists
        if (!string.IsNullOrEmpty(order.ShippingAddress1))
        {
            orderDto.ShippingAddress = new AddressDto
            {
                Address1 = order.ShippingAddress1,
                Address2 = order.ShippingAddress2,
                City = order.ShippingCity ?? "",
                State = order.ShippingState ?? "",
                Zip = order.ShippingZip ?? "",
                Country = order.ShippingCountry
            };
        }

        return orderDto;
    }
}