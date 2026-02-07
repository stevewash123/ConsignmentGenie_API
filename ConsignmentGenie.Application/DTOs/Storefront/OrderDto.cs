using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Storefront;

public class OrderDto
{
    public Guid Id { get; set; }

    [Required]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    public string FulfillmentType { get; set; } = string.Empty;

    public AddressDto? ShippingAddress { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class OrderItemDto
{
    public Guid ItemId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    [Required]
    public decimal Price { get; set; }
}

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddressDto
{
    [Required]
    public string Address1 { get; set; } = string.Empty;

    public string? Address2 { get; set; }

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string Zip { get; set; } = string.Empty;

    public string Country { get; set; } = "US";
}