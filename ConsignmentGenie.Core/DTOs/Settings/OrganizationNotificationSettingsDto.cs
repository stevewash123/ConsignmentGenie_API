using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

/// <summary>
/// Notification event categories and their notification type preferences
/// </summary>
public class NotificationEvent
{
    public string EventName { get; set; } = string.Empty;
    public bool Email { get; set; } = false;
    public bool SMS { get; set; } = false;
    public bool System { get; set; } = false;
}

/// <summary>
/// Notification category grouping related events
/// </summary>
public class NotificationCategory
{
    public string CategoryName { get; set; } = string.Empty;
    public List<NotificationEvent> Events { get; set; } = new();
}

/// <summary>
/// Contact information for notifications
/// </summary>
public class NotificationContactInfo
{
    [EmailAddress]
    public string? PrimaryEmailAddress { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Notification threshold settings
/// </summary>
public class NotificationThresholds
{
    public decimal? HighValueSaleThreshold { get; set; } = 500;
    public int? LowInventoryAlertThreshold { get; set; } = 10;
}

/// <summary>
/// Organization notification settings DTO matching the matrix UI structure
/// </summary>
public class OrganizationNotificationSettingsDto
{
    /// <summary>
    /// Contact information for notifications
    /// </summary>
    public NotificationContactInfo ContactInfo { get; set; } = new();

    /// <summary>
    /// Notification preferences organized by category and event
    /// </summary>
    public List<NotificationCategory> NotificationPreferences { get; set; } = new()
    {
        new()
        {
            CategoryName = "Business Operations",
            Events = new()
            {
                new() { EventName = "Daily Sales Summary", Email = true, SMS = false, System = true },
                new() { EventName = "Weekly Report", Email = true, SMS = false, System = false },
                new() { EventName = "Monthly Summary", Email = true, SMS = false, System = false }
            }
        },
        new()
        {
            CategoryName = "Consignor Activity",
            Events = new()
            {
                new() { EventName = "New Consignor Registration", Email = true, SMS = false, System = true },
                new() { EventName = "dropoff_manifest", Email = true, SMS = false, System = true },
                new() { EventName = "Item Request Approved", Email = false, SMS = false, System = true },
                new() { EventName = "Item Request Rejected", Email = false, SMS = false, System = true },
                new() { EventName = "Payout Available", Email = true, SMS = true, System = true },
                new() { EventName = "Consignor Message", Email = true, SMS = false, System = true }
            }
        },
        new()
        {
            CategoryName = "Sales & Inventory",
            Events = new()
            {
                new() { EventName = "Item Sold", Email = false, SMS = false, System = true },
                new() { EventName = "High Value Sale", Email = true, SMS = true, System = true },
                new() { EventName = "Item Expired", Email = true, SMS = false, System = true },
                new() { EventName = "Low Inventory Alert", Email = true, SMS = false, System = true }
            }
        },
        new()
        {
            CategoryName = "System Alerts",
            Events = new()
            {
                new() { EventName = "System Maintenance", Email = true, SMS = false, System = true },
                new() { EventName = "Security Alerts", Email = true, SMS = true, System = true },
                new() { EventName = "Feature Updates", Email = true, SMS = false, System = false },
                new() { EventName = "Integration Issues", Email = true, SMS = false, System = true }
            }
        }
    };

    /// <summary>
    /// Notification threshold settings
    /// </summary>
    public NotificationThresholds Thresholds { get; set; } = new();
}

/// <summary>
/// Request DTO for updating organization notification settings
/// </summary>
public class UpdateOrganizationNotificationSettingsRequest
{
    public NotificationContactInfo? ContactInfo { get; set; }
    public List<NotificationCategory>? NotificationPreferences { get; set; }
    public NotificationThresholds? Thresholds { get; set; }
}