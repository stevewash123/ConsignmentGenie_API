using ConsignmentGenie.Application.DTOs.Bookkeeping;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Reflection;

namespace ConsignmentGenie.API.Controllers.Settings;

[ApiController]
[Route("api/owner/settings/bookkeeping")]
[Authorize(Roles = "Owner")]
public class OrganizationBookkeepingSettingsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<OrganizationBookkeepingSettingsController> _logger;

    public OrganizationBookkeepingSettingsController(ConsignmentGenieContext context, ILogger<OrganizationBookkeepingSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("general")]
    public async Task<IActionResult> GetBookkeepingSettings()
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.BookkeepingSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            // Return default settings if none exist
            var defaultSettings = new BookkeepingSettingsDto
            {
                Id = Guid.Empty,
                OrganizationId = organizationId,
                UseQuickBooks = false,
                QuickBooksConnected = false,
                QuickBooksCompanyId = null,
                QuickBooksCompanyName = null,
                QuickBooksLastSync = null,
                SalesSyncEnabled = false,
                ConsignorSyncEnabled = true,
                PayoutRecordingMethod = "direct-expense",
                LineItemDetail = "lump-sum",
                SyncFrequency = "manual",
                AccountMappings = "{}",
                AutoCreateVendors = true,
                SyncItemsAsProducts = false,
                TrackInventoryQuantities = false,
                ContinueOnSyncErrors = true,
                LastSyncAttempt = null,
                LastSuccessfulSync = null,
                LastSyncError = null,
                EnableCsvExport = true,
                EnableExcelExport = true,
                ExportFilePrefix = "ConsignmentGenie",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(defaultSettings);
        }

        var settingsDto = MapToDto(settings);
        return Ok(settingsDto);
    }

    [HttpPost("general")]
    public async Task<IActionResult> CreateBookkeepingSettings([FromBody] CreateBookkeepingSettingsRequest request)
    {
        var organizationId = GetOrganizationId();

        // Check if settings already exist
        var existingSettings = await _context.BookkeepingSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (existingSettings != null)
        {
            return BadRequest(new { success = false, message = "Bookkeeping settings already exist for this organization. Use PUT to update." });
        }

        var settings = new BookkeepingSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UseQuickBooks = request.UseQuickBooks,
            QuickBooksConnected = request.QuickBooksConnected,
            QuickBooksCompanyId = request.QuickBooksCompanyId,
            QuickBooksCompanyName = request.QuickBooksCompanyName,
            SalesSyncEnabled = request.SalesSyncEnabled,
            ConsignorSyncEnabled = request.ConsignorSyncEnabled,
            PayoutRecordingMethod = request.PayoutRecordingMethod,
            LineItemDetail = request.LineItemDetail,
            SyncFrequency = request.SyncFrequency,
            AccountMappings = request.AccountMappings,
            AutoCreateVendors = request.AutoCreateVendors,
            SyncItemsAsProducts = request.SyncItemsAsProducts,
            TrackInventoryQuantities = request.TrackInventoryQuantities,
            ContinueOnSyncErrors = request.ContinueOnSyncErrors,
            EnableCsvExport = request.EnableCsvExport,
            EnableExcelExport = request.EnableExcelExport,
            ExportFilePrefix = request.ExportFilePrefix,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BookkeepingSettings.Add(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created bookkeeping settings for organization {OrganizationId}", organizationId);

        var settingsDto = MapToDto(settings);
        return CreatedAtAction(nameof(GetBookkeepingSettings), new { id = settings.Id }, settingsDto);
    }

    [HttpPut("general")]
    public async Task<IActionResult> UpdateBookkeepingSettings([FromBody] UpdateBookkeepingSettingsRequest request)
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.BookkeepingSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            // Create new settings with default values if none exist, then apply updates
            settings = new BookkeepingSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UseQuickBooks = false,
                QuickBooksConnected = false,
                SalesSyncEnabled = false,
                ConsignorSyncEnabled = true,
                PayoutRecordingMethod = "direct-expense",
                LineItemDetail = "lump-sum",
                SyncFrequency = "manual",
                AccountMappings = "{}",
                AutoCreateVendors = true,
                SyncItemsAsProducts = false,
                TrackInventoryQuantities = false,
                ContinueOnSyncErrors = true,
                EnableCsvExport = true,
                EnableExcelExport = true,
                ExportFilePrefix = "ConsignmentGenie",
                CreatedAt = DateTime.UtcNow
            };
            _context.BookkeepingSettings.Add(settings);
        }

        // Apply updates using reflection to handle any property
        UpdateEntityFromRequest(settings, request);
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated bookkeeping settings for organization {OrganizationId}", organizationId);

        var settingsDto = MapToDto(settings);
        return Ok(settingsDto);
    }

    // Special endpoint for partial updates (auto-save functionality)
    [HttpPatch("general")]
    public async Task<IActionResult> PatchBookkeepingSettings([FromBody] JsonElement updates)
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.BookkeepingSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            // Create with defaults first
            settings = new BookkeepingSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UseQuickBooks = false,
                QuickBooksConnected = false,
                SalesSyncEnabled = false,
                ConsignorSyncEnabled = true,
                PayoutRecordingMethod = "direct-expense",
                LineItemDetail = "lump-sum",
                SyncFrequency = "manual",
                AccountMappings = "{}",
                AutoCreateVendors = true,
                SyncItemsAsProducts = false,
                TrackInventoryQuantities = false,
                ContinueOnSyncErrors = true,
                EnableCsvExport = true,
                EnableExcelExport = true,
                ExportFilePrefix = "ConsignmentGenie",
                CreatedAt = DateTime.UtcNow
            };
            _context.BookkeepingSettings.Add(settings);
        }

        // Apply JSON patch-like updates
        ApplyJsonUpdates(settings, updates);
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Patched bookkeeping settings for organization {OrganizationId}", organizationId);

        var settingsDto = MapToDto(settings);
        return Ok(settingsDto);
    }

    [HttpDelete("general")]
    public async Task<IActionResult> DeleteBookkeepingSettings()
    {
        var organizationId = GetOrganizationId();

        var settings = await _context.BookkeepingSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        if (settings == null)
        {
            return NotFound(new { success = false, message = "Bookkeeping settings not found" });
        }

        _context.BookkeepingSettings.Remove(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted bookkeeping settings for organization {OrganizationId}", organizationId);

        return Ok(new { success = true, message = "Bookkeeping settings deleted successfully" });
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            throw new UnauthorizedAccessException("Invalid organization");
        }
        return organizationId;
    }

    private BookkeepingSettingsDto MapToDto(BookkeepingSettings settings)
    {
        return new BookkeepingSettingsDto
        {
            Id = settings.Id,
            OrganizationId = settings.OrganizationId,
            UseQuickBooks = settings.UseQuickBooks,
            QuickBooksConnected = settings.QuickBooksConnected,
            QuickBooksCompanyId = settings.QuickBooksCompanyId,
            QuickBooksCompanyName = settings.QuickBooksCompanyName,
            QuickBooksLastSync = settings.QuickBooksLastSync,
            SalesSyncEnabled = settings.SalesSyncEnabled,
            ConsignorSyncEnabled = settings.ConsignorSyncEnabled,
            PayoutRecordingMethod = settings.PayoutRecordingMethod,
            LineItemDetail = settings.LineItemDetail,
            SyncFrequency = settings.SyncFrequency,
            AccountMappings = settings.AccountMappings,
            AutoCreateVendors = settings.AutoCreateVendors,
            SyncItemsAsProducts = settings.SyncItemsAsProducts,
            TrackInventoryQuantities = settings.TrackInventoryQuantities,
            ContinueOnSyncErrors = settings.ContinueOnSyncErrors,
            LastSyncAttempt = settings.LastSyncAttempt,
            LastSuccessfulSync = settings.LastSuccessfulSync,
            LastSyncError = settings.LastSyncError,
            EnableCsvExport = settings.EnableCsvExport,
            EnableExcelExport = settings.EnableExcelExport,
            ExportFilePrefix = settings.ExportFilePrefix,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt ?? settings.CreatedAt
        };
    }

    private void UpdateEntityFromRequest(BookkeepingSettings entity, UpdateBookkeepingSettingsRequest request)
    {
        var entityType = typeof(BookkeepingSettings);
        var requestType = typeof(UpdateBookkeepingSettingsRequest);

        foreach (var requestProp in requestType.GetProperties())
        {
            var value = requestProp.GetValue(request);
            if (value != null)
            {
                var entityProp = entityType.GetProperty(requestProp.Name);
                if (entityProp != null && entityProp.CanWrite)
                {
                    // Handle nullable types
                    var actualValue = requestProp.PropertyType.IsGenericType &&
                                    requestProp.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                                    ? value
                                    : value;
                    entityProp.SetValue(entity, actualValue);
                }
            }
        }
    }

    private void ApplyJsonUpdates(BookkeepingSettings entity, JsonElement updates)
    {
        var entityType = typeof(BookkeepingSettings);

        foreach (var property in updates.EnumerateObject())
        {
            var entityProp = entityType.GetProperty(property.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (entityProp != null && entityProp.CanWrite)
            {
                try
                {
                    var value = ConvertJsonValue(property.Value, entityProp.PropertyType);
                    entityProp.SetValue(entity, value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to update property {PropertyName}: {Error}", property.Name, ex.Message);
                }
            }
        }
    }

    private object? ConvertJsonValue(JsonElement jsonValue, Type targetType)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlyingType.Name switch
        {
            nameof(Boolean) => jsonValue.GetBoolean(),
            nameof(Int32) => jsonValue.GetInt32(),
            nameof(Decimal) => jsonValue.GetDecimal(),
            nameof(String) => jsonValue.GetString(),
            nameof(DateTime) => jsonValue.GetDateTime(),
            nameof(Guid) => jsonValue.GetGuid(),
            _ => JsonSerializer.Deserialize(jsonValue.GetRawText(), targetType)
        };
    }
}