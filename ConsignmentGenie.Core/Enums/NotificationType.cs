namespace ConsignmentGenie.Core.Enums;

public enum NotificationType
{
    // Consignor notifications
    ConsignorApproved,
    ConsignorRejected,
    ItemSold,
    PayoutReady,
    PayoutProcessed,

    // Owner notifications
    NewConsignorRequest,
    LowInventoryAlert,
    DailySalesSummary,
    SuggestionSubmitted,
    OwnerInvitationSent,

    // System notifications
    PasswordReset,
    WelcomeEmail,
    AccountActivated,
    AccountDeactivated,

    // Payment notifications
    PaymentReceived,
    PaymentFailed,
    SubscriptionExpiring,

    // Integration notifications
    SyncError,
    SyncCompleted,

    // Legacy UI notification types (for in-app notifications)
    Info,
    Success,
    Warning,
    Error,
    Promotion,
    Reminder,
    System,
    Payment,
    Inventory,
    Consignor,
    Report
}

public enum NotificationChannel
{
    Email,
    Sms,
    Slack,
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