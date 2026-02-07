using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Core.Interfaces;

public interface IOrganizationSettingsManager
{
    /// <summary>
    /// Get a specific setting value for a page
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="page">The settings page</param>
    /// <param name="key">The setting key</param>
    /// <returns>The setting value or default if not found</returns>
    Task<T?> GetSettingAsync<T>(Guid organizationId, SettingsPage page, string key);

    /// <summary>
    /// Set a specific setting value for a page
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="page">The settings page</param>
    /// <param name="key">The setting key</param>
    /// <param name="value">The setting value</param>
    Task SetSettingAsync<T>(Guid organizationId, SettingsPage page, string key, T? value);

    /// <summary>
    /// Get all settings for a specific page
    /// </summary>
    /// <typeparam name="TPageDto">The DTO type for the page settings</typeparam>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="page">The settings page</param>
    /// <returns>The page settings DTO</returns>
    Task<TPageDto> GetPageSettingsAsync<TPageDto>(Guid organizationId, SettingsPage page)
        where TPageDto : class, new();

    /// <summary>
    /// Set all settings for a specific page
    /// </summary>
    /// <typeparam name="TPageDto">The DTO type for the page settings</typeparam>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="page">The settings page</param>
    /// <param name="settings">The page settings DTO</param>
    Task SetPageSettingsAsync<TPageDto>(Guid organizationId, SettingsPage page, TPageDto settings)
        where TPageDto : class;

    /// <summary>
    /// Update multiple settings for a page with a dictionary
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="page">The settings page</param>
    /// <param name="changes">Dictionary of key-value pairs to update</param>
    Task UpdateSettingsAsync(Guid organizationId, SettingsPage page, Dictionary<string, object?> changes);

    /// <summary>
    /// Synchronize a setting value to a specific database column (for backward compatibility)
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="settingKey">The setting key</param>
    /// <param name="columnName">The database column name</param>
    /// <param name="value">The value to sync</param>
    Task SyncToColumnAsync(Guid organizationId, string settingKey, string columnName, object? value);

    /// <summary>
    /// Clear cache for an organization's settings
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    Task ClearCacheAsync(Guid organizationId);
}