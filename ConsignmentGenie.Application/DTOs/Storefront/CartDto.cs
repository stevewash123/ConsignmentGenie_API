using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Storefront;

public class CartDto
{
    public Guid Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public int ItemCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal EstimatedTax { get; set; }
    public decimal EstimatedTotal { get; set; }
}

public class CartItemDto
{
    public Guid ItemId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
    public string? Category { get; set; }

    [Required]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }
    public DateTime AddedAt { get; set; }
}

public class AddToCartRequest
{
    [Required]
    public Guid ItemId { get; set; }
}