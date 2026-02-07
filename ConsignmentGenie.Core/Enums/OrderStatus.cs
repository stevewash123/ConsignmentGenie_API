namespace ConsignmentGenie.Core.Enums;

public enum OrderStatus
{
    Pending = 0,
    PaymentProcessing = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7,
    Failed = 8
}