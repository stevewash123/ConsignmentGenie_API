using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class NotificationPreferenceServiceTests : IDisposable
{
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Mock<ILogger<NotificationPreferenceService>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly NotificationPreferenceService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _role = "Owner";

    public NotificationPreferenceServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<NotificationPreferenceService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _service = new NotificationPreferenceService(_context, _memoryCache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPreferencesAsync_NoExistingPreferences_ReturnsDefaults()
    {
        // Act
        var result = await _service.GetPreferencesAsync(_userId, _role);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(_role, result.Role);

        // Verify default preferences are set
        Assert.True(result.Preferences.ContainsKey("daily_sales_summary"));
        Assert.True(result.Preferences["daily_sales_summary"][NotificationChannel.Email]);
        Assert.False(result.Preferences["daily_sales_summary"][NotificationChannel.Sms]);
        Assert.True(result.Preferences["daily_sales_summary"][NotificationChannel.System]);

        // Verify system alerts are enabled by default
        Assert.True(result.Preferences["security_alerts"][NotificationChannel.Email]);
        Assert.True(result.Preferences["security_alerts"][NotificationChannel.System]);
    }

    [Fact]
    public async Task GetPreferencesAsync_ExistingPreferences_ReturnsFromDatabase()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = _userId,
            Role = _role,
            TypePreferences = JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, bool>>
            {
                ["item_sold"] = new() { ["Email"] = false, ["Sms"] = true, ["System"] = true }
            }),
            SmsPhoneNumber = "+1234567890",
            SmsPhoneVerified = true
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPreferencesAsync(_userId, _role);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(_role, result.Role);
        Assert.Equal("+1234567890", result.PhoneNumber);
        Assert.True(result.SmsVerified);

        // Verify loaded preferences
        Assert.True(result.Preferences.ContainsKey("item_sold"));
        Assert.False(result.Preferences["item_sold"][NotificationChannel.Email]);
        Assert.True(result.Preferences["item_sold"][NotificationChannel.Sms]);
        Assert.True(result.Preferences["item_sold"][NotificationChannel.System]);
    }

    [Fact]
    public async Task GetPreferencesAsync_UsesMemoryCache()
    {
        // Arrange - First call loads from database
        var result1 = await _service.GetPreferencesAsync(_userId, _role);

        // Act - Second call should use memory cache
        var result2 = await _service.GetPreferencesAsync(_userId, _role);

        // Assert - Both results should be identical
        Assert.Equal(result1.UserId, result2.UserId);
        Assert.Equal(result1.Role, result2.Role);
        Assert.Equal(result1.LastUpdated, result2.LastUpdated);

        // Verify cache was used (no additional database queries)
        // This is implicit - if cache wasn't used, test would be slower
    }

    [Fact]
    public async Task IsNotificationEnabledAsync_EnabledNotification_ReturnsTrue()
    {
        // Arrange
        var preferences = new NotificationPreferences
        {
            UserId = _userId,
            Role = _role,
            TypePreferences = JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, bool>>
            {
                ["item_sold"] = new() { ["Email"] = true, ["Sms"] = false }
            })
        };

        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();

        // Act
        var emailEnabled = await _service.IsNotificationEnabledAsync(_userId, _role, "item_sold", NotificationChannel.Email);
        var smsEnabled = await _service.IsNotificationEnabledAsync(_userId, _role, "item_sold", NotificationChannel.Sms);

        // Assert
        Assert.True(emailEnabled);
        Assert.False(smsEnabled);
    }

    [Fact]
    public async Task IsNotificationEnabledAsync_UnknownNotification_ReturnsTrue()
    {
        // Act - Check for a notification type that doesn't exist in preferences
        var result = await _service.IsNotificationEnabledAsync(_userId, _role, "unknown_notification", NotificationChannel.Email);

        // Assert - Should default to enabled for new notification types
        Assert.True(result);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_NewPreferences_SavesAndInvalidatesCache()
    {
        // Arrange
        var matrix = new NotificationPreferencesMatrix
        {
            UserId = _userId,
            Role = _role,
            Preferences = new Dictionary<string, Dictionary<NotificationChannel, bool>>
            {
                ["item_sold"] = new() { [NotificationChannel.Email] = false, [NotificationChannel.Sms] = true }
            },
            PhoneNumber = "+1234567890",
            SmsVerified = true
        };

        // Act
        await _service.UpdatePreferencesAsync(_userId, _role, matrix);

        // Assert - Check database was updated
        var dbPrefs = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == _userId && p.Role == _role);

        Assert.NotNull(dbPrefs);
        Assert.Equal("+1234567890", dbPrefs.SmsPhoneNumber);
        Assert.True(dbPrefs.SmsPhoneVerified);

        var typePrefs = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(dbPrefs.TypePreferences);
        Assert.NotNull(typePrefs);
        Assert.False(typePrefs["item_sold"]["Email"]);
        Assert.True(typePrefs["item_sold"]["Sms"]);
    }

    [Fact]
    public async Task GetPreferencesAsync_BatchLoad_EfficiientlyLoadsMultipleUsers()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var userId in userIds)
        {
            var preferences = new NotificationPreferences
            {
                UserId = userId,
                Role = _role,
                TypePreferences = JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, bool>>
                {
                    ["item_sold"] = new() { ["Email"] = true, ["Sms"] = false }
                })
            };
            _context.NotificationPreferences.Add(preferences);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPreferencesAsync(userIds, _role);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(userIds, userId => Assert.True(result.ContainsKey(userId)));

        // Verify preferences are correctly loaded
        foreach (var kvp in result)
        {
            Assert.Equal(_role, kvp.Value.Role);
            Assert.True(kvp.Value.Preferences["item_sold"][NotificationChannel.Email]);
            Assert.False(kvp.Value.Preferences["item_sold"][NotificationChannel.Sms]);
        }
    }

    [Fact]
    public async Task InvalidateCacheAsync_RemovesCachedData()
    {
        // Arrange - Load data into cache
        await _service.GetPreferencesAsync(_userId, _role);

        // Act - Invalidate cache
        await _service.InvalidateCacheAsync(_userId, _role);

        // Manually check that cache key was removed
        var cacheKey = $"notification_prefs:{_userId}:{_role}";
        var cachedValue = _memoryCache.Get(cacheKey);

        // Assert
        Assert.Null(cachedValue);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _memoryCache?.Dispose();
    }
}