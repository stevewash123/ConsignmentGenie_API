namespace ConsignmentGenie.Core.Constants;

public static class NotificationTypes
{
    // Consignor types
    public const string CONSIGNOR_WELCOME = "welcome";
    public const string ITEM_SOLD = "item_sold";
    public const string PAYOUT_PROCESSED = "payout_processed";
    public const string ITEM_REQUEST_APPROVED = "item_request_approved";
    public const string ITEM_REQUEST_REJECTED = "item_request_rejected";
    public const string ITEM_PRICE_CHANGED = "item_price_changed";
    public const string ITEM_RETURNED = "item_returned";
    public const string ITEM_EXPIRED = "item_expired";
    public const string STATEMENT_READY = "statement_ready";

    // Owner types
    public const string OWNER_WELCOME = "welcome";
    public const string NEW_CONSIGNOR_REQUEST = "new_provider_request";
    public const string NEW_ITEM_REQUEST = "new_item_request";
    public const string ITEM_RESERVED = "item_reserved";
    public const string RESERVATION_EXPIRED_OWNER = "reservation_expired_owner";
    public const string SUBSCRIPTION_FAILED = "subscription_failed";
    public const string PROVIDER_APPROVED = "provider_approved";
    public const string DAILY_SALES_SUMMARY = "daily_sales_summary";
    public const string PAYOUT_DUE_REMINDER = "payout_due_reminder";
    public const string LOW_INVENTORY_ALERT = "low_inventory_alert";
    public const string SUBSCRIPTION_REMINDER = "subscription_reminder";
    public const string SQUARE_SYNC_ERROR = "square_sync_error";
    public const string QB_SYNC_ERROR = "qb_sync_error";
    public const string SYSTEM_ANNOUNCEMENT = "system_announcement";

    // Admin types
    public const string NEW_OWNER_REQUEST = "new_owner_request";
    public const string OWNER_APPROVED = "owner_approved";
    public const string SUBSCRIPTION_CREATED = "subscription_created";
    public const string SUBSCRIPTION_CANCELLED = "subscription_cancelled";
    public const string SYSTEM_ERROR = "system_error";
    public const string DAILY_PLATFORM_SUMMARY = "daily_platform_summary";

    // Customer types
    public const string CUSTOMER_WELCOME = "customer_welcome";
    public const string RESERVATION_CONFIRMED = "reservation_confirmed";
    public const string RESERVATION_EXPIRING_SOON = "reservation_expiring_soon";
    public const string RESERVATION_EXPIRED = "reservation_expired";
    public const string ORDER_CONFIRMED = "order_confirmed";
    public const string ORDER_READY_PICKUP = "order_ready_pickup";
    public const string ORDER_SHIPPED = "order_shipped";
}