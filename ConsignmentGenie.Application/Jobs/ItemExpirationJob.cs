using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Jobs;

/// <summary>
/// Background job for processing item expiration status updates and notifications
/// Runs daily to check for items that need status updates or notifications
/// </summary>
public class ItemExpirationJob
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConsignorNotificationService _notificationService;
    private readonly ILogger<ItemExpirationJob> _logger;

    public ItemExpirationJob(
        ConsignmentGenieContext context,
        IConsignorNotificationService notificationService,
        ILogger<ItemExpirationJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Main job entry point - processes all expiration events for all organizations
    /// </summary>
    public async Task ProcessItemExpirationsAsync()
    {
        try
        {
            _logger.LogInformation("Starting item expiration processing job at {Timestamp}", DateTime.UtcNow);

            var processedCounts = new
            {
                ExpiredItems = 0,
                NearExpirationWarnings = 0,
                OverduePickupItems = 0,
                OrganizationsProcessed = 0,
                Errors = 0
            };

            // Get all organizations to process their items
            var organizations = await _context.Organizations
                .ToListAsync();

            foreach (var organization in organizations)
            {
                try
                {
                    _logger.LogDebug("Processing expiration events for organization {OrganizationId} - {OrganizationName}",
                        organization.Id, organization.Name);

                    var orgResults = await ProcessOrganizationExpirations(organization);

                    processedCounts = new
                    {
                        ExpiredItems = processedCounts.ExpiredItems + orgResults.ExpiredItems,
                        NearExpirationWarnings = processedCounts.NearExpirationWarnings + orgResults.NearExpirationWarnings,
                        OverduePickupItems = processedCounts.OverduePickupItems + orgResults.OverduePickupItems,
                        OrganizationsProcessed = processedCounts.OrganizationsProcessed + 1,
                        Errors = processedCounts.Errors
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expiration events for organization {OrganizationId}",
                        organization.Id);
                    processedCounts = processedCounts with { Errors = processedCounts.Errors + 1 };
                }
            }

            _logger.LogInformation("Item expiration processing completed. Results: {@Results}", processedCounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in item expiration processing job");
            throw;
        }
    }

    /// <summary>
    /// Process expiration events for a specific organization
    /// </summary>
    private async Task<(int ExpiredItems, int NearExpirationWarnings, int OverduePickupItems)> ProcessOrganizationExpirations(Organization organization)
    {
        var results = (ExpiredItems: 0, NearExpirationWarnings: 0, OverduePickupItems: 0);

        // Get organization's consignor settings to extract default periods
        var defaultConsignmentDays = 90; // Default value
        var defaultRetrievalDays = 14; // Default value

        // Parse ConsignorSettings JSON to get default values
        if (!string.IsNullOrEmpty(organization.ConsignorSettings))
        {
            try
            {
                using var doc = JsonDocument.Parse(organization.ConsignorSettings);
                if (doc.RootElement.TryGetProperty("defaults", out var defaults))
                {
                    if (defaults.TryGetProperty("consignmentPeriodDays", out var consignmentProperty))
                    {
                        defaultConsignmentDays = consignmentProperty.GetInt32();
                    }
                    if (defaults.TryGetProperty("retrievalPeriodDays", out var retrievalProperty))
                    {
                        defaultRetrievalDays = retrievalProperty.GetInt32();
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse ConsignorSettings for organization {OrganizationId}, using defaults", organization.Id);
            }
        }

        var today = DateTime.UtcNow.Date;

        // Get all active items for this organization that might need processing
        var items = await _context.Items
            .Include(i => i.Consignor)
            .Where(i => i.OrganizationId == organization.Id &&
                       (i.Status == ItemStatus.Available || i.Status == ItemStatus.Removed))
            .ToListAsync();

        foreach (var item in items)
        {
            try
            {
                var itemResults = await ProcessItemExpiration(item, organization, defaultConsignmentDays, defaultRetrievalDays, today);
                results = (
                    results.ExpiredItems + itemResults.ExpiredItems,
                    results.NearExpirationWarnings + itemResults.NearExpirationWarnings,
                    results.OverduePickupItems + itemResults.OverduePickupItems
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item expiration for item {ItemId}", item.Id);
            }
        }

        return results;
    }

    /// <summary>
    /// Process expiration logic for a single item
    /// </summary>
    private async Task<(int ExpiredItems, int NearExpirationWarnings, int OverduePickupItems)> ProcessItemExpiration(
        Item item, Organization organization, int defaultConsignmentDays, int defaultRetrievalDays, DateTime today)
    {
        var results = (ExpiredItems: 0, NearExpirationWarnings: 0, OverduePickupItems: 0);

        // Calculate expiration dates based on item's received date
        var consignmentDays = defaultConsignmentDays; // Use organization defaults
        var retrievalDays = defaultRetrievalDays;

        // Convert DateOnly to DateTime for calculations
        var receivedDateTime = item.ReceivedDate.ToDateTime(TimeOnly.MinValue);
        var expirationDate = receivedDateTime.AddDays(consignmentDays).Date;
        var retrievalDeadline = expirationDate.AddDays(retrievalDays);

        // Calculate days until expiration/pickup deadline
        var daysUntilExpiration = (expirationDate - today).Days;
        var daysUntilPickupDeadline = (retrievalDeadline - today).Days;

        // 1. Check if item needs to be marked as removed (expired)
        if (item.Status == ItemStatus.Available && daysUntilExpiration < 0)
        {
            await MarkItemAsExpired(item);
            results = results with { ExpiredItems = results.ExpiredItems + 1 };

            // Send expiration notification to consignor
            await SendExpirationNotification(item, organization, expirationDate, retrievalDeadline);
        }

        // 2. Check for near-expiration warnings (7, 3, 1 days before)
        else if (item.Status == ItemStatus.Available && ShouldSendNearExpirationWarning(item, daysUntilExpiration))
        {
            await SendNearExpirationWarning(item, organization, expirationDate, daysUntilExpiration);
            results = results with { NearExpirationWarnings = results.NearExpirationWarnings + 1 };
        }

        // 3. Check for overdue pickup reminders (expired items past retrieval deadline)
        else if (item.Status == ItemStatus.Removed && daysUntilPickupDeadline < 0)
        {
            await SendOverduePickupNotification(item, organization, retrievalDeadline);
            results = results with { OverduePickupItems = results.OverduePickupItems + 1 };
        }

        // 4. Check for pickup reminders during retrieval period (3, 1 days before deadline)
        else if (item.Status == ItemStatus.Removed && ShouldSendPickupReminder(item, daysUntilPickupDeadline))
        {
            await SendPickupReminder(item, organization, retrievalDeadline, daysUntilPickupDeadline);
        }

        return results;
    }

    /// <summary>
    /// Mark an item as removed (expired) and update its status
    /// </summary>
    private async Task MarkItemAsExpired(Item item)
    {
        item.Status = ItemStatus.Removed; // Use Removed instead of Expired
        item.StatusChangedAt = DateTime.UtcNow;
        item.StatusChangedReason = "Consignment period expired";
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Marked item {ItemId} (SKU: {SKU}) as expired/removed", item.Id, item.Sku);
    }

    /// <summary>
    /// Check if we should send a near-expiration warning
    /// </summary>
    private bool ShouldSendNearExpirationWarning(Item item, int daysUntilExpiration)
    {
        // Send warnings at 7, 3, and 1 days before expiration
        return daysUntilExpiration == 7 || daysUntilExpiration == 3 || daysUntilExpiration == 1;
    }

    /// <summary>
    /// Check if we should send a pickup reminder
    /// </summary>
    private bool ShouldSendPickupReminder(Item item, int daysUntilPickupDeadline)
    {
        // Send reminders at 3 and 1 days before pickup deadline
        return daysUntilPickupDeadline == 3 || daysUntilPickupDeadline == 1;
    }

    /// <summary>
    /// Send near-expiration warning to consignor
    /// </summary>
    private async Task SendNearExpirationWarning(Item item, Organization organization, DateTime expirationDate, int daysLeft)
    {
        try
        {
            if (item.Consignor?.UserId == null)
            {
                _logger.LogWarning("Cannot send near-expiration warning for item {ItemId} - consignor has no UserId", item.Id);
                return;
            }

            await _notificationService.CreateNotificationAsync(new Core.DTOs.Notifications.CreateNotificationRequest
            {
                OrganizationId = organization.Id,
                ToUserId = item.Consignor.UserId.Value,
                ToType = "consignor",
                FromType = "system",
                Type = "ItemNearExpiration",
                Title = $"Item Expiring Soon - {daysLeft} Day(s) Left",
                Message = $"Your item \"{item.Title}\" (SKU: {item.Sku}) will expire in {daysLeft} day(s) on {expirationDate:MMM d, yyyy}. " +
                         $"If it doesn't sell, you'll have {defaultRetrievalDays} days to pick it up.",
                ItemId = item.Id,
                ActionUrl = $"/consignor/items/{item.Id}",
                Payload = JsonSerializer.Serialize(new
                {
                    ItemId = item.Id,
                    ItemTitle = item.Title,
                    ItemSku = item.Sku,
                    ExpirationDate = expirationDate,
                    DaysLeft = daysLeft
                })
            });

            _logger.LogDebug("Sent near-expiration warning for item {ItemId} to consignor {ConsignorId} ({DaysLeft} days left)",
                item.Id, item.ConsignorId, daysLeft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send near-expiration warning for item {ItemId}", item.Id);
        }
    }

    /// <summary>
    /// Send expiration notification to consignor
    /// </summary>
    private async Task SendExpirationNotification(Item item, Organization organization, DateTime expirationDate, DateTime pickupDeadline)
    {
        try
        {
            if (item.Consignor?.UserId == null)
            {
                _logger.LogWarning("Cannot send expiration notification for item {ItemId} - consignor has no UserId", item.Id);
                return;
            }

            await _notificationService.CreateNotificationAsync(new Core.DTOs.Notifications.CreateNotificationRequest
            {
                OrganizationId = organization.Id,
                ToUserId = item.Consignor.UserId.Value,
                ToType = "consignor",
                FromType = "system",
                Type = "ItemExpired",
                Title = "Item Has Expired - Pickup Required",
                Message = $"Your item \"{item.Title}\" (SKU: {item.Sku}) has expired and is ready for pickup. " +
                         $"Please collect it by {pickupDeadline:MMM d, yyyy} to avoid additional fees.",
                ItemId = item.Id,
                ActionUrl = $"/consignor/items/{item.Id}",
                Payload = JsonSerializer.Serialize(new
                {
                    ItemId = item.Id,
                    ItemTitle = item.Title,
                    ItemSku = item.Sku,
                    ExpirationDate = expirationDate,
                    PickupDeadline = pickupDeadline
                })
            });

            _logger.LogDebug("Sent expiration notification for item {ItemId} to consignor {ConsignorId}",
                item.Id, item.ConsignorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiration notification for item {ItemId}", item.Id);
        }
    }

    /// <summary>
    /// Send pickup reminder during retrieval period
    /// </summary>
    private async Task SendPickupReminder(Item item, Organization organization, DateTime pickupDeadline, int daysLeft)
    {
        try
        {
            if (item.Consignor?.UserId == null)
            {
                _logger.LogWarning("Cannot send pickup reminder for item {ItemId} - consignor has no UserId", item.Id);
                return;
            }

            await _notificationService.CreateNotificationAsync(new Core.DTOs.Notifications.CreateNotificationRequest
            {
                OrganizationId = organization.Id,
                ToUserId = item.Consignor.UserId.Value,
                ToType = "consignor",
                FromType = "system",
                Type = "ItemPickupReminder",
                Title = $"Pickup Reminder - {daysLeft} Day(s) Left",
                Message = $"Reminder: Your expired item \"{item.Title}\" (SKU: {item.Sku}) must be picked up by {pickupDeadline:MMM d, yyyy} " +
                         $"({daysLeft} day(s) remaining). Please arrange pickup to avoid additional fees.",
                ItemId = item.Id,
                ActionUrl = $"/consignor/items/{item.Id}",
                Payload = JsonSerializer.Serialize(new
                {
                    ItemId = item.Id,
                    ItemTitle = item.Title,
                    ItemSku = item.Sku,
                    PickupDeadline = pickupDeadline,
                    DaysLeft = daysLeft
                })
            });

            _logger.LogDebug("Sent pickup reminder for item {ItemId} to consignor {ConsignorId} ({DaysLeft} days left)",
                item.Id, item.ConsignorId, daysLeft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send pickup reminder for item {ItemId}", item.Id);
        }
    }

    /// <summary>
    /// Send overdue pickup notification when deadline has passed
    /// </summary>
    private async Task SendOverduePickupNotification(Item item, Organization organization, DateTime pickupDeadline)
    {
        try
        {
            if (item.Consignor?.UserId == null)
            {
                _logger.LogWarning("Cannot send overdue pickup notification for item {ItemId} - consignor has no UserId", item.Id);
                return;
            }

            await _notificationService.CreateNotificationAsync(new Core.DTOs.Notifications.CreateNotificationRequest
            {
                OrganizationId = organization.Id,
                ToUserId = item.Consignor.UserId.Value,
                ToType = "consignor",
                FromType = "system",
                Type = "ItemPickupOverdue",
                Title = "Item Pickup Overdue - Action Required",
                Message = $"Your item \"{item.Title}\" (SKU: {item.Sku}) was due for pickup on {pickupDeadline:MMM d, yyyy} " +
                         $"and is now overdue. Please contact the shop immediately to arrange pickup and discuss any applicable fees.",
                ItemId = item.Id,
                ActionUrl = $"/consignor/items/{item.Id}",
                Payload = JsonSerializer.Serialize(new
                {
                    ItemId = item.Id,
                    ItemTitle = item.Title,
                    ItemSku = item.Sku,
                    PickupDeadline = pickupDeadline,
                    DaysOverdue = (DateTime.UtcNow.Date - pickupDeadline).Days
                })
            });

            _logger.LogDebug("Sent overdue pickup notification for item {ItemId} to consignor {ConsignorId}",
                item.Id, item.ConsignorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send overdue pickup notification for item {ItemId}", item.Id);
        }
    }

    private int defaultRetrievalDays = 14; // Field to reference in notification messages
}