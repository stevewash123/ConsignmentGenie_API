using ConsignmentGenie.Core.Attributes;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// Centralized service for managing organization settings with caching and automatic synchronization
/// </summary>
public class OrganizationSettingsManager : IOrganizationSettingsManager
{
    private readonly ConsignmentGenieContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrganizationSettingsManager> _logger;

    // Cache configuration
    private const int CacheExpirationMinutes = 30;
    private const string CacheKeyPrefix = "org_settings";

    // JsonSerializer options for consistent serialization
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OrganizationSettingsManager(
        ConsignmentGenieContext context,
        IMemoryCache cache,
        ILogger<OrganizationSettingsManager> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific setting value for a page
    /// </summary>
    public async Task<T?> GetSettingAsync<T>(Guid organizationId, SettingsPage page, string key)
    {
        _logger.LogDebug("[SETTINGS] Getting setting {Key} for page {Page} in organization {OrgId}", key, page.Name, organizationId);

        var settings = await GetPageJsonAsync(organizationId, page);
        if (string.IsNullOrEmpty(settings))
        {
            _logger.LogDebug("[SETTINGS] No settings found for page {Page}, returning default", page.Name);
            return default;
        }

        try
        {
            using var document = JsonDocument.Parse(settings);
            if (document.RootElement.TryGetProperty(key, out var property))
            {
                return JsonSerializer.Deserialize<T>(property.GetRawText(), _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETTINGS] Failed to parse setting {Key} for page {Page}", key, page.Name);
        }

        return default;
    }

    /// <summary>
    /// Set a specific setting value for a page
    /// </summary>
    public async Task SetSettingAsync<T>(Guid organizationId, SettingsPage page, string key, T? value)
    {
        _logger.LogDebug("[SETTINGS] Setting {Key} = {Value} for page {Page} in organization {OrgId}", key, value, page.Name, organizationId);

        var organization = await GetOrganizationAsync(organizationId);
        if (organization == null)
        {
            _logger.LogWarning("[SETTINGS] Organization {OrgId} not found", organizationId);
            throw new InvalidOperationException($"Organization {organizationId} not found");
        }

        // Get current settings JSON
        var currentJson = GetJsonColumn(organization, page);
        var settingsDict = string.IsNullOrEmpty(currentJson)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(currentJson, _jsonOptions) ?? new Dictionary<string, object?>();

        // Update the specific setting
        if (value == null)
        {
            settingsDict.Remove(key);
        }
        else
        {
            settingsDict[key] = value;
        }

        // Serialize back to JSON
        var updatedJson = JsonSerializer.Serialize(settingsDict, _jsonOptions);

        // Update the organization
        SetJsonColumn(organization, page, updatedJson);

        // Check if this property needs to be synchronized to a database column
        await SynchronizeColumnIfNeededAsync(organization, key, value);

        await _context.SaveChangesAsync();
        ClearCacheForOrganization(organizationId);

        _logger.LogInformation("[SETTINGS] Updated setting {Key} for page {Page} in organization {OrgId}", key, page.Name, organizationId);
    }

    /// <summary>
    /// Get all settings for a specific page
    /// </summary>
    public async Task<TPageDto> GetPageSettingsAsync<TPageDto>(Guid organizationId, SettingsPage page)
        where TPageDto : class, new()
    {
        _logger.LogDebug("[SETTINGS] Getting page settings for {Page} in organization {OrgId}", page.Name, organizationId);

        var cacheKey = GetCacheKey(organizationId, page);
        if (_cache.TryGetValue(cacheKey, out TPageDto? cachedSettings) && cachedSettings != null)
        {
            _logger.LogDebug("[SETTINGS] Returning cached settings for {Page}", page.Name);
            return cachedSettings;
        }

        var settingsJson = await GetPageJsonAsync(organizationId, page);
        TPageDto settings;

        if (string.IsNullOrEmpty(settingsJson))
        {
            _logger.LogDebug("[SETTINGS] No existing settings for {Page}, creating defaults", page.Name);
            settings = new TPageDto();
        }
        else
        {
            try
            {
                settings = JsonSerializer.Deserialize<TPageDto>(settingsJson, _jsonOptions) ?? new TPageDto();
                _logger.LogDebug("[SETTINGS] Deserialized settings for {Page}", page.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SETTINGS] Failed to deserialize settings for {Page}, using defaults", page.Name);
                settings = new TPageDto();
            }
        }

        // Cache the settings
        _cache.Set(cacheKey, settings, TimeSpan.FromMinutes(CacheExpirationMinutes));
        return settings;
    }

    /// <summary>
    /// Set all settings for a specific page
    /// </summary>
    public async Task SetPageSettingsAsync<TPageDto>(Guid organizationId, SettingsPage page, TPageDto settings)
        where TPageDto : class
    {
        _logger.LogDebug("[SETTINGS] Setting page settings for {Page} in organization {OrgId}", page.Name, organizationId);

        // Validate settings if they implement IPageSettings
        if (settings is IPageSettings validatableSettings)
        {
            var validation = validatableSettings.Validate();
            if (!validation.IsValid)
            {
                _logger.LogWarning("[SETTINGS] Validation failed for {Page}: {Errors}", page.Name, string.Join(", ", validation.Errors));
                throw new ArgumentException($"Settings validation failed: {string.Join(", ", validation.Errors)}");
            }
        }

        var organization = await GetOrganizationAsync(organizationId);
        if (organization == null)
        {
            _logger.LogWarning("[SETTINGS] Organization {OrgId} not found", organizationId);
            throw new InvalidOperationException($"Organization {organizationId} not found");
        }

        // Serialize settings to JSON
        var settingsJson = JsonSerializer.Serialize(settings, _jsonOptions);
        SetJsonColumn(organization, page, settingsJson);

        // Synchronize any attributed properties to database columns
        await SynchronizeAttributedPropertiesAsync(organization, settings);

        await _context.SaveChangesAsync();
        ClearCacheForOrganization(organizationId);

        _logger.LogInformation("[SETTINGS] Updated page settings for {Page} in organization {OrgId}", page.Name, organizationId);
    }

    /// <summary>
    /// Update multiple settings for a page with a dictionary
    /// </summary>
    public async Task UpdateSettingsAsync(Guid organizationId, SettingsPage page, Dictionary<string, object?> changes)
    {
        _logger.LogDebug("[SETTINGS] Updating {Count} settings for page {Page} in organization {OrgId}", changes.Count, page.Name, organizationId);

        var organization = await GetOrganizationAsync(organizationId);
        if (organization == null)
        {
            _logger.LogWarning("[SETTINGS] Organization {OrgId} not found", organizationId);
            throw new InvalidOperationException($"Organization {organizationId} not found");
        }

        // Get current settings
        var currentJson = GetJsonColumn(organization, page);
        var settingsDict = string.IsNullOrEmpty(currentJson)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(currentJson, _jsonOptions) ?? new Dictionary<string, object?>();

        // Apply changes
        foreach (var change in changes)
        {
            if (change.Value == null)
            {
                settingsDict.Remove(change.Key);
            }
            else
            {
                settingsDict[change.Key] = change.Value;
            }

            // Check if this property needs column synchronization
            await SynchronizeColumnIfNeededAsync(organization, change.Key, change.Value);
        }

        // Update JSON column
        var updatedJson = JsonSerializer.Serialize(settingsDict, _jsonOptions);
        SetJsonColumn(organization, page, updatedJson);

        await _context.SaveChangesAsync();
        ClearCacheForOrganization(organizationId);

        _logger.LogInformation("[SETTINGS] Updated {Count} settings for page {Page} in organization {OrgId}", changes.Count, page.Name, organizationId);
    }

    /// <summary>
    /// Synchronize a setting value to a specific database column (for backward compatibility)
    /// </summary>
    public async Task SyncToColumnAsync(Guid organizationId, string settingKey, string columnName, object? value)
    {
        var organization = await GetOrganizationAsync(organizationId);
        if (organization == null) return;

        var property = typeof(Organization).GetProperty(columnName);
        if (property == null)
        {
            _logger.LogWarning("[SETTINGS] Column {ColumnName} not found on Organization entity", columnName);
            return;
        }

        property.SetValue(organization, value);
        await _context.SaveChangesAsync();

        _logger.LogDebug("[SETTINGS] Synchronized {Key} to column {ColumnName}", settingKey, columnName);
    }

    /// <summary>
    /// Clear cache for an organization's settings
    /// </summary>
    public async Task ClearCacheAsync(Guid organizationId)
    {
        ClearCacheForOrganization(organizationId);
        _logger.LogDebug("[SETTINGS] Cleared cache for organization {OrgId}", organizationId);
        await Task.CompletedTask;
    }

    #region Private Helper Methods

    private async Task<Organization?> GetOrganizationAsync(Guid organizationId)
    {
        return await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);
    }

    private async Task<string?> GetPageJsonAsync(Guid organizationId, SettingsPage page)
    {
        var organization = await GetOrganizationAsync(organizationId);
        return organization == null ? null : GetJsonColumn(organization, page);
    }

    private static string? GetJsonColumn(Organization organization, SettingsPage page)
    {
        return page.Name switch
        {
            "Organization" => organization.Settings,
            "Business" => organization.BusinessSettings,
            "Consignor" => organization.ConsignorSettings,
            "Storefront" => organization.StorefrontSettings,
            "Notifications" => organization.NotificationSettings,
            "Receipts" => organization.ReceiptSettings,
            _ => throw new ArgumentException($"Unknown settings page: {page.Name}")
        };
    }

    private static void SetJsonColumn(Organization organization, SettingsPage page, string json)
    {
        switch (page.Name)
        {
            case "Organization":
                organization.Settings = json;
                break;
            case "Business":
                organization.BusinessSettings = json;
                break;
            case "Consignor":
                organization.ConsignorSettings = json;
                break;
            case "Storefront":
                organization.StorefrontSettings = json;
                break;
            case "Notifications":
                organization.NotificationSettings = json;
                break;
            case "Receipts":
                organization.ReceiptSettings = json;
                break;
            default:
                throw new ArgumentException($"Unknown settings page: {page.Name}");
        }
    }

    private async Task SynchronizeAttributedPropertiesAsync<T>(Organization organization, T settings)
    {
        var type = typeof(T);
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var syncAttribute = property.GetCustomAttribute<SyncToColumnAttribute>();
            if (syncAttribute != null)
            {
                var value = property.GetValue(settings);
                await SynchronizeToColumnAsync(organization, syncAttribute.ColumnName, value);
            }
        }
    }

    private async Task SynchronizeColumnIfNeededAsync(Organization organization, string settingKey, object? value)
    {
        // For known settings that need column synchronization
        var columnMapping = GetColumnMapping(settingKey);
        if (!string.IsNullOrEmpty(columnMapping))
        {
            await SynchronizeToColumnAsync(organization, columnMapping, value);
        }
    }

    private async Task SynchronizeToColumnAsync(Organization organization, string columnName, object? value)
    {
        var property = typeof(Organization).GetProperty(columnName);
        if (property == null)
        {
            _logger.LogWarning("[SETTINGS] Column {ColumnName} not found on Organization entity", columnName);
            return;
        }

        var currentValue = property.GetValue(organization);
        if (!Equals(currentValue, value))
        {
            property.SetValue(organization, value);
            _logger.LogDebug("[SETTINGS] Synchronized column {ColumnName} to value {Value}", columnName, value);
        }
    }

    private static string? GetColumnMapping(string settingKey)
    {
        // Map known settings to database columns for business logic access
        return settingKey switch
        {
            "agreementTemplateId" or "AgreementTemplateId" => nameof(Organization.AgreementTemplateCloudinaryUrl),
            "requireSignedAgreement" or "RequireSignedAgreement" => nameof(Organization.AgreementMethod), // Now maps to AgreementMethod - if not "none", agreements are required
            "agreementRequirement" or "AgreementRequirement" => nameof(Organization.AgreementMethod),
            "approvalMode" or "ApprovalMode" => nameof(Organization.ApprovalMode),
            _ => null
        };
    }

    private string GetCacheKey(Guid organizationId, SettingsPage page) =>
        $"{CacheKeyPrefix}:{organizationId}:{page.Name}";

    private void ClearCacheForOrganization(Guid organizationId)
    {
        // Clear all cached settings for this organization
        foreach (var page in SettingsPage.All)
        {
            var cacheKey = GetCacheKey(organizationId, page);
            _cache.Remove(cacheKey);
        }
    }

    #endregion
}