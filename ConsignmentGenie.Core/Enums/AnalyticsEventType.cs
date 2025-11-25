namespace ConsignmentGenie.Core.Enums;

public enum AnalyticsEventType
{
    // Customer Events
    CustomerRegistered = 1,
    CustomerLogin = 2,
    CustomerLogout = 3,

    // Product Events
    ProductViewed = 10,
    ProductAddedToCart = 11,
    ProductRemovedFromCart = 12,
    ProductAddedToWishlist = 13,
    ProductRemovedFromWishlist = 14,
    ProductSearched = 15,

    // Order Events
    OrderCreated = 20,
    OrderPaid = 21,
    OrderShipped = 22,
    OrderDelivered = 23,
    OrderCancelled = 24,
    OrderRefunded = 25,

    // Provider Events
    ProviderRegistered = 30,
    ProviderLogin = 31,
    ItemUploaded = 32,
    ItemApproved = 33,
    ItemRejected = 34,

    // Admin Events
    UserLogin = 40,
    UserCreated = 41,
    ItemCreated = 42,
    ItemModified = 43,
    PayoutProcessed = 44,

    // System Events
    EmailSent = 50,
    ErrorOccurred = 51,
    PaymentFailed = 52,
    SyncCompleted = 53
}