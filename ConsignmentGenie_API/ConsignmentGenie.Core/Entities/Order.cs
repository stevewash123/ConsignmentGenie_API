using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class Order : BaseEntity
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public decimal SubTotal { get; set; }

    public decimal Tax { get; set; } = 0;

    public decimal Shipping { get; set; } = 0;

    [Required]
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }

    public string? ShippingAddress { get; set; } // JSON

    public string? BillingAddress { get; set; } // JSON

    public string? CustomerNotes { get; set; }

    public string? InternalNotes { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Computed properties
    public decimal Commission => OrderItems.Sum(item => item.CommissionAmount);
    public int ItemCount => OrderItems.Sum(item => item.Quantity);
}