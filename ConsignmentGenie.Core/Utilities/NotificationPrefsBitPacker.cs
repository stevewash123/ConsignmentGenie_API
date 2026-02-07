using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Utilities;

/// <summary>
/// Utility class for packing/unpacking notification preferences into a single long (64 bits)
/// Layout: 18 notification types × 3 channels = 54 bits (10 bits reserved for future expansion)
/// Bit pattern: [Type0_Email, Type0_SMS, Type0_System, Type1_Email, Type1_SMS, Type1_System, ...]
/// </summary>
public static class NotificationPrefsBitPacker
{
    // Notification type mappings based on the UI screenshot
    private static readonly Dictionary<string, int> NotificationTypeIndices = new()
    {
        // Business Operations (0-4)
        ["daily_sales_summary"] = 0,
        ["weekly_report"] = 1,
        ["monthly_statement"] = 2,

        // Consignor Activity (3-7)
        ["consignor_signup"] = 3,
        ["consignor_item_added"] = 4,
        ["pending_approval"] = 5,
        ["daily_payout_ready"] = 6,
        ["weekly_payout_ready"] = 7,

        // Sales & Inventory (8-11)
        ["item_sold"] = 8,
        ["high_value_sale"] = 9,
        ["low_inventory"] = 10,
        ["pricing_suggestions"] = 11,

        // System Alerts (12-16)
        ["system_maintenance"] = 12,
        ["security_alerts"] = 13,
        ["account_changes"] = 14,
        ["backup_status"] = 15,

        // Reserved for future expansion (16-20)
    };

    private static readonly Dictionary<NotificationChannel, int> ChannelIndices = new()
    {
        [NotificationChannel.Email] = 0,
        [NotificationChannel.Sms] = 1,
        [NotificationChannel.System] = 2
    };

    /// <summary>
    /// Checks if a specific notification type and channel is enabled in the packed preferences
    /// </summary>
    public static bool IsEnabled(long packedPrefs, string notificationType, NotificationChannel channel)
    {
        if (!NotificationTypeIndices.TryGetValue(notificationType, out var typeIndex))
        {
            // Unknown notification type - default to enabled for new types
            return true;
        }

        var bitIndex = (typeIndex * 3) + ChannelIndices[channel];
        return (packedPrefs & (1L << bitIndex)) != 0;
    }

    /// <summary>
    /// Sets a specific notification preference in the packed long
    /// </summary>
    public static long SetPreference(long packedPrefs, string notificationType, NotificationChannel channel, bool enabled)
    {
        if (!NotificationTypeIndices.TryGetValue(notificationType, out var typeIndex))
        {
            // Can't set preference for unknown notification type
            return packedPrefs;
        }

        var bitIndex = (typeIndex * 3) + ChannelIndices[channel];

        return enabled
            ? packedPrefs | (1L << bitIndex)      // Set bit
            : packedPrefs & ~(1L << bitIndex);    // Clear bit
    }

    /// <summary>
    /// Packs a NotificationPreferencesMatrix into a single long
    /// </summary>
    public static long PackMatrix(NotificationPreferencesMatrix matrix)
    {
        var packedPrefs = 0L;

        foreach (var typePrefs in matrix.Preferences)
        {
            var notificationType = typePrefs.Key;

            foreach (var channelPref in typePrefs.Value)
            {
                var channel = channelPref.Key;
                var enabled = channelPref.Value;

                packedPrefs = SetPreference(packedPrefs, notificationType, channel, enabled);
            }
        }

        return packedPrefs;
    }

    /// <summary>
    /// Unpacks a long into a NotificationPreferencesMatrix
    /// </summary>
    public static NotificationPreferencesMatrix UnpackMatrix(long packedPrefs, Guid userId, string role)
    {
        var preferences = new Dictionary<string, Dictionary<NotificationChannel, bool>>();

        foreach (var kvp in NotificationTypeIndices)
        {
            var notificationType = kvp.Key;
            var channelPrefs = new Dictionary<NotificationChannel, bool>();

            foreach (var channel in ChannelIndices.Keys)
            {
                channelPrefs[channel] = IsEnabled(packedPrefs, notificationType, channel);
            }

            preferences[notificationType] = channelPrefs;
        }

        return new NotificationPreferencesMatrix
        {
            UserId = userId,
            Role = role,
            Preferences = preferences,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts packed preferences to hex string for JWT storage
    /// </summary>
    public static string ToHexString(long packedPrefs)
    {
        return packedPrefs.ToString("X");
    }

    /// <summary>
    /// Parses hex string from JWT back to packed preferences
    /// </summary>
    public static long FromHexString(string hexString)
    {
        return Convert.ToInt64(hexString, 16);
    }

    /// <summary>
    /// Gets all supported notification types
    /// </summary>
    public static IReadOnlyCollection<string> SupportedNotificationTypes => NotificationTypeIndices.Keys;

    /// <summary>
    /// Debug helper to visualize packed preferences as binary string
    /// </summary>
    public static string ToBinaryString(long packedPrefs)
    {
        return Convert.ToString(packedPrefs, 2).PadLeft(64, '0');
    }
}