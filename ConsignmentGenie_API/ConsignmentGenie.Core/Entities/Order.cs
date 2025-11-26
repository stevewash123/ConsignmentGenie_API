using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Order : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    // Customer info (may or may not have account)
    public Guid? CustomerId { get; set; }  // Nullable for guest checkout

    [Required]
    [MaxLength(255)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CustomerPhone { get; set; }

    // Shipping/Pickup
    [Required]
    [MaxLength(20)]
    public string FulfillmentType { get; set; } = "pickup";  // 'pickup', 'shipping'

    // Shipping address (if shipping)
    [MaxLength(200)]
    public string? ShippingAddress1 { get; set; }

    [MaxLength(200)]
    public string? ShippingAddress2 { get; set; }

    [MaxLength(100)]
    public string? ShippingCity { get; set; }

    [MaxLength(50)]
    public string? ShippingState { get; set; }

    [MaxLength(20)]
    public string? ShippingZip { get; set; }

    [MaxLength(50)]
    public string ShippingCountry { get; set; } = "US";

    // Totals
    [Required]
    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; } = 0;

    public decimal ShippingAmount { get; set; } = 0;

    [Required]
    public decimal TotalAmount { get; set; }

    // Payment
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }  // 'card', 'cash_on_pickup'

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "pending";  // 'pending', 'paid', 'refunded'

    [MaxLength(100)]
    public string? PaymentIntentId { get; set; }  // Stripe PaymentIntent ID

    public DateTime? PaidAt { get; set; }

    // Order status
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string? Notes { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    // Navigation properties
    public Customer? Customer { get; set; }  // Nullable for guest checkout
    public Organization Organization { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Computed properties - updated for consignment
    public decimal Commission => OrderItems.Sum(item => item.CommissionAmount);
    public int ItemCount => OrderItems.Count;
}