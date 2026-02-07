using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.Services.Interfaces;

/// <summary>
/// Service for fast lookup of notification preferences with multi-layer caching
/// </summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Gets notification preferences for a user and role with fast caching
    /// Cache layers: Memory → Redis → Database
    /// </summary>
    Task<NotificationPreferencesMatrix> GetPreferencesAsync(Guid userId, string role);

    /// <summary>
    /// Gets preferences for multiple users efficiently (batch lookup)
    /// </summary>
    Task<Dictionary<Guid, NotificationPreferencesMatrix>> GetPreferencesAsync(IEnumerable<Guid> userIds, string role);

    /// <summary>
    /// Updates preferences and invalidates cache
    /// </summary>
    Task UpdatePreferencesAsync(Guid userId, string role, NotificationPreferencesMatrix preferences);

    /// <summary>
    /// Checks if a specific notification type is enabled for a user/channel combination
    /// </summary>
    Task<bool> IsNotificationEnabledAsync(Guid userId, string role, string notificationType, NotificationChannel channel);

    /// <summary>
    /// Gets consolidated preferences for batch processing (weekly reports, etc.)
    /// Returns users grouped by their delivery preferences
    /// </summary>
    Task<Dictionary<string, List<Guid>>> GetBatchDeliveryPreferencesAsync(string role, string notificationType);

    // Cache methods removed - now using JWT claims for fast access
}

// Types moved to ConsignmentGenie.Core.DTOs.Notifications and ConsignmentGenie.Core.Enums