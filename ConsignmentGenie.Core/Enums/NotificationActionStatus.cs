namespace ConsignmentGenie.Core.Enums;

/// <summary>
/// Action status for actionable notifications
/// </summary>
public static class NotificationActionStatus
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// User types for FROM/TO notification pattern
/// </summary>
public static class NotificationUserType
{
    public const string Owner = "owner";
    public const string Consignor = "consignor";
    public const string Admin = "admin";
    public const string Customer = "customer";
    public const string System = "system";
}