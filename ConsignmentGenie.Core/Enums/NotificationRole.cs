namespace ConsignmentGenie.Core.Enums;

/// <summary>
/// Roles for notification targeting and preferences
/// </summary>
public enum NotificationRole
{
    /// <summary>
    /// Consignor role - artists/vendors who provide items
    /// </summary>
    Consignor,

    /// <summary>
    /// Shop owner role - business owners who manage the shop
    /// </summary>
    Owner,

    /// <summary>
    /// Admin role - system administrators
    /// </summary>
    Admin,

    /// <summary>
    /// Clerk role - shop employees with limited permissions
    /// </summary>
    Clerk,

    /// <summary>
    /// Shopper role - customers who purchase items
    /// </summary>
    Shopper
}