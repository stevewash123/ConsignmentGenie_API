using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Enums;

public enum NotificationType
{
    // Consignor notifications
    [Display(Name = "consignor_approved")]
    ConsignorApproved,
    [Display(Name = "consignor_rejected")]
    ConsignorRejected,
    [Display(Name = "item_sold")]
    ItemSold,
    [Display(Name = "item_rejected")]
    ItemRejected,
    [Display(Name = "item_activated")]
    ItemActivated,
    [Display(Name = "payout_ready")]
    PayoutReady,
    [Display(Name = "payout_processed")]
    PayoutProcessed,

    // Owner notifications
    [Display(Name = "new_provider_request")]
    NewConsignorRequest,
    [Display(Name = "low_inventory_alert")]
    LowInventoryAlert,
    [Display(Name = "daily_sales_summary")]
    DailySalesSummary,
    [Display(Name = "suggestion_submitted")]
    SuggestionSubmitted,
    [Display(Name = "owner_invitation_sent")]
    OwnerInvitationSent,
    [Display(Name = "dropoff_manifest")]
    DropoffManifest,
    [Display(Name = "item_reserved")]
    ItemReserved,
    [Display(Name = "reservation_expired_owner")]
    ReservationExpiredOwner,

    // Customer notifications
    [Display(Name = "customer_welcome")]
    CustomerWelcome,
    [Display(Name = "reservation_confirmed")]
    ReservationConfirmed,
    [Display(Name = "reservation_expiring_soon")]
    ReservationExpiringSoon,
    [Display(Name = "reservation_expired")]
    ReservationExpired,

    // System notifications
    [Display(Name = "password_reset")]
    PasswordReset,
    [Display(Name = "welcome")]
    WelcomeEmail,
    [Display(Name = "account_activated")]
    AccountActivated,
    [Display(Name = "account_deactivated")]
    AccountDeactivated,

    // Payment notifications
    [Display(Name = "payment_received")]
    PaymentReceived,
    [Display(Name = "payment_failed")]
    PaymentFailed,
    [Display(Name = "subscription_expiring")]
    SubscriptionExpiring,

    // Integration notifications
    [Display(Name = "sync_error")]
    SyncError,
    [Display(Name = "sync_completed")]
    SyncCompleted,

    // Legacy UI notification types (for in-app notifications)
    [Display(Name = "info")]
    Info,
    [Display(Name = "success")]
    Success,
    [Display(Name = "warning")]
    Warning,
    [Display(Name = "error")]
    Error,
    [Display(Name = "promotion")]
    Promotion,
    [Display(Name = "reminder")]
    Reminder,
    [Display(Name = "system_announcement")]
    System,
    [Display(Name = "payment")]
    Payment,
    [Display(Name = "inventory")]
    Inventory,
    [Display(Name = "consignor")]
    Consignor,
    [Display(Name = "report")]
    Report
}

public enum NotificationChannel
{
    Email,
    Sms,
    System,
    Push,
    InApp  // For the existing Notification entity
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}