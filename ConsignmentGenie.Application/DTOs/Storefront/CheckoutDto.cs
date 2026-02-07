using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Storefront;

public class CheckoutRequestDto
{
    // Customer info
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    // Fulfillment
    [Required]
    public string FulfillmentType { get; set; } = "pickup";  // "pickup" or "shipping"

    public AddressDto? ShippingAddress { get; set; }

    // Payment
    [Required]
    public string PaymentMethod { get; set; } = string.Empty;  // "card" or "cash_on_pickup"

    public string? PaymentMethodId { get; set; }  // Stripe payment method
}

public class CheckoutValidationDto
{
    public bool Valid { get; set; }
    public List<Guid> UnavailableItems { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class PaymentIntentDto
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}