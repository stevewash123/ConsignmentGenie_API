using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Utilities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Notification preference service for managing user notification preferences
/// </summary>
public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<NotificationPreferenceService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationPreferenceService(
        ConsignmentGenieContext context,
        ILogger<NotificationPreferenceService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<NotificationPreferencesMatrix> GetPreferencesAsync(Guid userId, string role)
    {
        return await LoadFromDatabaseAsync(userId, role);
    }

    public async Task<Dictionary<Guid, NotificationPreferencesMatrix>> GetPreferencesAsync(IEnumerable<Guid> userIds, string role)
    {
        return await LoadBatchFromDatabaseAsync(userIds, role);
    }

    public async Task UpdatePreferencesAsync(Guid userId, string role, NotificationPreferencesMatrix preferences)
    {
        await SaveToDatabaseAsync(userId, role, preferences);

        _logger.LogInformation("Updated notification preferences for user {UserId}, role {Role}", userId, role);

        // TODO: Trigger JWT token refresh to update packed preferences in claims
        // This ensures the user gets updated preferences on their next request
    }

    public async Task<bool> IsNotificationEnabledAsync(Guid userId, string role, string notificationType, NotificationChannel channel)
    {
        // Fast path: Try to get preferences from JWT claims first
        var packedPrefs = GetPackedPreferencesFromClaims(userId, role);
        if (packedPrefs.HasValue)
        {
            _logger.LogDebug("Using cached notification preferences from JWT claims for user {UserId}, role {Role}", userId, role);
            return NotificationPrefsBitPacker.IsEnabled(packedPrefs.Value, notificationType, channel);
        }

        // Fallback: Load from database
        _logger.LogDebug("JWT claims not available, loading notification preferences from database for user {UserId}, role {Role}", userId, role);
        var preferences = await LoadFromDatabaseAsync(userId, role);

        if (preferences.Preferences.TryGetValue(notificationType, out var channelPrefs))
        {
            return channelPrefs.TryGetValue(channel, out var enabled) && enabled;
        }

        // Default to enabled for new notification types
        return true;
    }

    public async Task<Dictionary<string, List<Guid>>> GetBatchDeliveryPreferencesAsync(string role, string notificationType)
    {
        // This would typically query all users for the given role who have the notification type enabled
        // and group them by delivery preferences (daily, weekly, immediate)

        var preferences = await _context.NotificationPreferences
            .Where(np => np.Role == role)
            .Where(np => np.TypePreferences.Contains(notificationType))
            .ToListAsync();

        var result = new Dictionary<string, List<Guid>>();

        foreach (var pref in preferences)
        {
            try
            {
                var typePrefs = JsonSerializer.Deserialize<Dictionary<string, object>>(pref.TypePreferences);
                if (typePrefs?.TryGetValue(notificationType, out var notificationConfig) == true)
                {
                    var configStr = notificationConfig.ToString();
                    var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configStr ?? "{}");

                    var batchMode = config?.TryGetValue("batch", out var batch) == true ? batch.ToString() : "immediate";

                    if (!result.ContainsKey(batchMode))
                        result[batchMode] = new List<Guid>();

                    result[batchMode].Add(pref.UserId);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse TypePreferences for user {UserId}", pref.UserId);
            }
        }

        return result;
    }

    /// <summary>
    /// Helper method to extract packed notification preferences from JWT claims
    /// </summary>
    private long? GetPackedPreferencesFromClaims(Guid userId, string role)
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            // Verify the claims belong to the requested user
            var claimUserId = user.FindFirst("sub")?.Value;
            var claimRole = user.FindFirst("role")?.Value;

            if (claimUserId != userId.ToString() || claimRole != role)
            {
                _logger.LogDebug("JWT claims user/role mismatch: claim user {ClaimUserId} vs requested {RequestedUserId}, claim role {ClaimRole} vs requested {RequestedRole}",
                    claimUserId, userId, claimRole, role);
                return null;
            }

            // Extract packed preferences from claims
            var packedPrefsHex = user.FindFirst("notif_prefs")?.Value;
            if (string.IsNullOrEmpty(packedPrefsHex))
            {
                return null;
            }

            return NotificationPrefsBitPacker.FromHexString(packedPrefsHex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract notification preferences from JWT claims for user {UserId}, role {Role}", userId, role);
            return null;
        }
    }

    private async Task<NotificationPreferencesMatrix> LoadFromDatabaseAsync(Guid userId, string role)
    {
        var preferences = await _context.NotificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId && np.Role == role);

        if (preferences == null)
        {
            // Return default preferences
            return CreateDefaultPreferences(userId, role);
        }

        return ConvertToMatrix(preferences);
    }

    private async Task<Dictionary<Guid, NotificationPreferencesMatrix>> LoadBatchFromDatabaseAsync(IEnumerable<Guid> userIds, string role)
    {
        var preferences = await _context.NotificationPreferences
            .Where(np => userIds.Contains(np.UserId) && np.Role == role)
            .ToListAsync();

        var result = new Dictionary<Guid, NotificationPreferencesMatrix>();

        foreach (var userId in userIds)
        {
            var userPref = preferences.FirstOrDefault(p => p.UserId == userId);
            result[userId] = userPref != null ? ConvertToMatrix(userPref) : CreateDefaultPreferences(userId, role);
        }

        return result;
    }

    private async Task SaveToDatabaseAsync(Guid userId, string role, NotificationPreferencesMatrix matrix)
    {
        var existing = await _context.NotificationPreferences
            .FirstOrDefaultAsync(np => np.UserId == userId && np.Role == role);

        if (existing == null)
        {
            existing = new NotificationPreferences
            {
                UserId = userId,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };
            _context.NotificationPreferences.Add(existing);
        }

        // Convert matrix back to JSONB format
        existing.TypePreferences = JsonSerializer.Serialize(matrix.Preferences);
        existing.SmsPhoneNumber = matrix.PhoneNumber;
        existing.SmsPhoneVerified = matrix.SmsVerified;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static NotificationPreferencesMatrix CreateDefaultPreferences(Guid userId, string role)
    {
        var defaultPrefs = new Dictionary<string, Dictionary<NotificationChannel, bool>>
        {
            // Business Operations
            ["daily_sales_summary"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["weekly_report"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["monthly_statement"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },

            // Consignor Activity
            ["consignor_signup"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["consignor_item_added"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["pending_approval"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["daily_payout_ready"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["weekly_payout_ready"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },

            // Sales & Inventory
            ["item_sold"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["high_value_sale"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["low_inventory"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["pricing_suggestions"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },

            // System Alerts - must be enabled for system channel, email/sms configurable but defaulted on
            ["system_maintenance"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["security_alerts"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["account_changes"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true },
            ["backup_status"] = new() { [NotificationChannel.Email] = true, [NotificationChannel.Sms] = false, [NotificationChannel.System] = true }
        };

        return new NotificationPreferencesMatrix
        {
            UserId = userId,
            Role = role,
            Preferences = defaultPrefs,
            BatchPreferences = new() { ["weekly_payout_ready"] = "monday" },
            Thresholds = new() { ["high_value_sale"] = 500, ["low_inventory"] = 10 },
            LastUpdated = DateTime.UtcNow
        };
    }

    private static NotificationPreferencesMatrix ConvertToMatrix(NotificationPreferences dbPrefs)
    {
        var matrix = new NotificationPreferencesMatrix
        {
            UserId = dbPrefs.UserId,
            Role = dbPrefs.Role,
            PhoneNumber = dbPrefs.SmsPhoneNumber,
            SmsVerified = dbPrefs.SmsPhoneVerified,
            LastUpdated = dbPrefs.UpdatedAt ?? DateTime.UtcNow
        };

        try
        {
            var typePrefs = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(dbPrefs.TypePreferences);
            if (typePrefs != null)
            {
                foreach (var kvp in typePrefs)
                {
                    var channelPrefs = new Dictionary<NotificationChannel, bool>();
                    foreach (var channelKvp in kvp.Value)
                    {
                        if (Enum.TryParse<NotificationChannel>(channelKvp.Key, true, out var channel))
                        {
                            channelPrefs[channel] = channelKvp.Value;
                        }
                    }
                    matrix.Preferences[kvp.Key] = channelPrefs;
                }
            }
        }
        catch (JsonException ex)
        {
            // Fall back to default preferences if JSON is malformed
            matrix = CreateDefaultPreferences(dbPrefs.UserId, dbPrefs.Role);
        }

        return matrix;
    }
}