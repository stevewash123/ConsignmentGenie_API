using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.DTOs.Items;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Models;
using System.Security.Claims;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Interfaces;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner")]
public class ItemsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ItemsController> _logger;
    private readonly IItemImportService _importService;
    private readonly IFileUploadTrackingService _fileTrackingService;
    private readonly PendingImportAssignmentService _assignmentService;
    private readonly INotificationService _notificationService;

    public ItemsController(
        ConsignmentGenieContext context,
        ILogger<ItemsController> logger,
        IItemImportService importService,
        IFileUploadTrackingService fileTrackingService,
        PendingImportAssignmentService assignmentService,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _importService = importService;
        _fileTrackingService = fileTrackingService;
        _assignmentService = assignmentService;
        _notificationService = notificationService;
    }

    // LIST - Get items with filtering/pagination
    [HttpGet]
    public async Task<ActionResult<PagedResult<ItemListDto>>> GetItems([FromQuery] ItemQueryParams queryParams)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var query = _context.Items
                .Include(i => i.Consignor)
                .Where(i => i.OrganizationId == organizationId);

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(i => i.Title.Contains(queryParams.Search) ||
                                       i.Sku.Contains(queryParams.Search) ||
                                       (i.Description != null && i.Description.Contains(queryParams.Search)));
            }

            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                if (Enum.TryParse<ItemStatus>(queryParams.Status, out var status))
                {
                    query = query.Where(i => i.Status == status);
                }
            }

            if (queryParams.ConsignorId.HasValue)
            {
                query = query.Where(i => i.ConsignorId == queryParams.ConsignorId.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.Category))
            {
                query = query.Where(i => i.ItemCategoryNavigation != null && i.ItemCategoryNavigation.Name == queryParams.Category);
            }

            if (!string.IsNullOrEmpty(queryParams.Condition))
            {
                if (Enum.TryParse<ItemCondition>(queryParams.Condition, out var condition))
                {
                    query = query.Where(i => i.Condition == condition);
                }
            }

            if (queryParams.MinPrice.HasValue)
            {
                query = query.Where(i => i.Price >= queryParams.MinPrice.Value);
            }

            if (queryParams.MaxPrice.HasValue)
            {
                query = query.Where(i => i.Price <= queryParams.MaxPrice.Value);
            }

            if (queryParams.ReceivedAfter.HasValue)
            {
                var dateOnly = DateOnly.FromDateTime(queryParams.ReceivedAfter.Value);
                query = query.Where(i => i.ReceivedDate >= dateOnly);
            }

            if (queryParams.ReceivedBefore.HasValue)
            {
                var dateOnly = DateOnly.FromDateTime(queryParams.ReceivedBefore.Value);
                query = query.Where(i => i.ReceivedDate <= dateOnly);
            }

            // Apply sorting
            query = queryParams.SortBy.ToLower() switch
            {
                "title" => queryParams.SortDirection == "asc" ? query.OrderBy(i => i.Title) : query.OrderByDescending(i => i.Title),
                "price" => queryParams.SortDirection == "asc" ? query.OrderBy(i => i.Price) : query.OrderByDescending(i => i.Price),
                "receiveddate" => queryParams.SortDirection == "asc" ? query.OrderBy(i => i.ReceivedDate) : query.OrderByDescending(i => i.ReceivedDate),
                _ => queryParams.SortDirection == "asc" ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(i => new ItemListDto
                {
                    ItemId = i.Id,
                    Sku = i.Sku,
                    Title = i.Title,
                    Price = i.Price,
                    MinimumPrice = i.MinimumPrice,
                    Category = i.ItemCategoryNavigation != null ? i.ItemCategoryNavigation.Name : null,
                    Condition = i.Condition,
                    ConditionLabel = i.Condition.ToDisplayName(),
                    Status = i.Status,
                    PrimaryImageUrl = i.PrimaryImageUrl,
                    ReceivedDate = i.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                    SoldDate = i.SoldDate.HasValue ? i.SoldDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    ConsignorId = i.ConsignorId,
                    ConsignorName = (i.Consignor.FirstName + " " + i.Consignor.LastName).Trim(),
                    CommissionRate = i.Consignor.CommissionRate
                })
                .ToListAsync();

            return Ok(new PagedResult<ItemListDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                OrganizationId = organizationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to retrieve items"));
        }
    }

    // GET ONE - Get item by ID with full details
    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDetailDto>> GetItem(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var item = await _context.Items
                .Include(i => i.Consignor)
                .Include(i => i.Transaction)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId);

            if (item == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Item not found"));
            }

            var dto = new ItemDetailDto
            {
                ItemId = item.Id,
                ConsignorId = item.ConsignorId,
                ConsignorName = (item.Consignor.FirstName + " " + item.Consignor.LastName).Trim(),
                CommissionRate = item.Consignor.CommissionRate,
                Sku = item.Sku,
                Barcode = item.Barcode,
                Title = item.Title,
                Description = item.Description,
                Category = item.ItemCategoryNavigation?.Name,
                Brand = item.Brand,
                Size = item.Size,
                Color = item.Color,
                Condition = item.Condition,
                ConditionLabel = item.Condition.ToDisplayName(),
                Materials = item.Materials,
                Measurements = item.Measurements,
                Price = item.Price,
                OriginalPrice = item.OriginalPrice,
                MinimumPrice = item.MinimumPrice,
                ShopAmount = item.Price * (1 - item.Consignor.CommissionRate / 100),
                ConsignorAmount = item.Price * (item.Consignor.CommissionRate / 100),
                Status = item.Status,
                StatusChangedAt = item.StatusChangedAt,
                StatusChangedReason = item.StatusChangedReason,
                ReceivedDate = item.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                ListedDate = item.ListedDate?.ToDateTime(TimeOnly.MinValue),
                ExpirationDate = item.ExpirationDate?.ToDateTime(TimeOnly.MinValue),
                SoldDate = item.SoldDate?.ToDateTime(TimeOnly.MinValue),
                Images = !string.IsNullOrEmpty(item.PrimaryImageUrl)
                    ? new List<ItemImageDto>
                    {
                        new ItemImageDto
                        {
                            ItemImageId = Guid.NewGuid(), // Temporary ID for compatibility
                            ImageUrl = item.PrimaryImageUrl,
                            DisplayOrder = 0,
                            IsPrimary = true
                        }
                    }
                    : new List<ItemImageDto>(),
                Photos = !string.IsNullOrEmpty(item.PrimaryImageUrl)
                    ? new List<PhotoInfoDto>
                    {
                        new PhotoInfoDto
                        {
                            Url = item.PrimaryImageUrl,
                            PublicId = "primary",
                            IsPrimary = true,
                            Order = 0,
                            UploadedAt = item.CreatedAt.ToString("O")
                        }
                    }
                    : new List<PhotoInfoDto>(),
                Location = item.Location,
                Notes = item.Notes,
                InternalNotes = item.InternalNotes,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt ?? item.CreatedAt,
                TransactionId = item.Transaction?.Id,
                SalePrice = item.Transaction?.SalePrice()
            };

            return Ok(ApiResponse<ItemDetailDto>.SuccessResult(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item {ItemId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to retrieve item"));
        }
    }

    // CREATE - Add new item
    [HttpPost]
    public async Task<ActionResult<ItemDetailDto>> CreateItem([FromBody] CreateItemRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            // Validate consignor belongs to organization
            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(p => p.Id == request.ConsignorId && p.OrganizationId == organizationId);

            if (consignor == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid consignor"));
            }

            // Check if organization requires agreement on file and consignor doesn't have one
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            // Check if organization requires agreement and consignor has one uploaded
            if (organization?.AgreementMethod != "none")
            {
                var hasAgreement = await _context.ConsignorAgreements
                    .AnyAsync(a => a.ConsignorId == consignor.Id && a.Status == "uploaded");

                if (!hasAgreement)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Consignor must have a signed agreement on file before submitting items"));
                }
            }

            // Generate SKU if not provided
            var sku = !string.IsNullOrEmpty(request.Sku) ? request.Sku : await GenerateSkuAsync(organizationId);

            // Check SKU uniqueness
            var existingSku = await _context.Items
                .AnyAsync(i => i.OrganizationId == organizationId && i.Sku == sku);

            if (existingSku)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("SKU already exists"));
            }

            var item = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ConsignorId = request.ConsignorId,
                Sku = sku,
                Barcode = request.Barcode,
                Title = request.Title,
                Description = request.Description,
                // TODO: Handle CategoryId based on request.Category
                Brand = request.Brand,
                Size = request.Size,
                Color = request.Color,
                Condition = request.Condition,
                Materials = request.Materials,
                Measurements = request.Measurements,
                Price = request.Price,
                OriginalPrice = request.OriginalPrice,
                MinimumPrice = request.MinimumPrice,
                Status = ItemStatus.Available,
                ReceivedDate = request.ReceivedDate.HasValue
                    ? DateOnly.FromDateTime(request.ReceivedDate.Value)
                    : DateOnly.FromDateTime(DateTime.UtcNow),
                ExpirationDate = request.ExpirationDate.HasValue
                    ? DateOnly.FromDateTime(request.ExpirationDate.Value)
                    : null,
                Location = request.Location,
                Notes = request.Notes,
                InternalNotes = request.InternalNotes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Reload with provider info for response
            var savedItem = await _context.Items
                .Include(i => i.Consignor)
                .FirstAsync(i => i.Id == item.Id);

            var dto = new ItemDetailDto
            {
                ItemId = savedItem.Id,
                ConsignorId = savedItem.ConsignorId,
                ConsignorName = (savedItem.Consignor.FirstName + " " + savedItem.Consignor.LastName).Trim(),
                CommissionRate = savedItem.Consignor.CommissionRate,
                Sku = savedItem.Sku,
                Barcode = savedItem.Barcode,
                Title = savedItem.Title,
                Description = savedItem.Description,
                Category = savedItem.ItemCategoryNavigation?.Name,
                Brand = savedItem.Brand,
                Size = savedItem.Size,
                Color = savedItem.Color,
                Condition = savedItem.Condition,
                ConditionLabel = savedItem.Condition.ToDisplayName(),
                Materials = savedItem.Materials,
                Measurements = savedItem.Measurements,
                Price = savedItem.Price,
                OriginalPrice = savedItem.OriginalPrice,
                MinimumPrice = savedItem.MinimumPrice,
                ShopAmount = savedItem.Price * (1 - savedItem.Consignor.CommissionRate / 100),
                ConsignorAmount = savedItem.Price * (savedItem.Consignor.CommissionRate / 100),
                Status = savedItem.Status,
                StatusChangedAt = savedItem.StatusChangedAt,
                StatusChangedReason = savedItem.StatusChangedReason,
                ReceivedDate = savedItem.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                ListedDate = savedItem.ListedDate?.ToDateTime(TimeOnly.MinValue),
                ExpirationDate = savedItem.ExpirationDate?.ToDateTime(TimeOnly.MinValue),
                SoldDate = savedItem.SoldDate?.ToDateTime(TimeOnly.MinValue),
                Images = new List<ItemImageDto>(),
                Location = savedItem.Location,
                Notes = savedItem.Notes,
                InternalNotes = savedItem.InternalNotes,
                CreatedAt = savedItem.CreatedAt,
                UpdatedAt = savedItem.UpdatedAt ?? savedItem.CreatedAt
            };

            return CreatedAtAction(nameof(GetItem), new { id = savedItem.Id }, ApiResponse<ItemDetailDto>.SuccessResult(dto, "Item created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to create item"));
        }
    }

    // SKU GENERATION - Generate next available SKU
    [HttpGet("generate-sku")]
    public async Task<ActionResult<string>> GenerateSku([FromQuery] string? prefix = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var sku = await GenerateSkuAsync(organizationId, prefix);
            return Ok(ApiResponse<string>.SuccessResult(sku));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SKU");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to generate SKU"));
        }
    }

    private async Task<string> GenerateSkuAsync(Guid organizationId, string? prefix = null)
    {
        prefix ??= "ITEM";

        // Get highest existing SKU number for this prefix
        var lastSku = await _context.Items
            .Where(i => i.OrganizationId == organizationId && i.Sku.StartsWith(prefix + "-"))
            .OrderByDescending(i => i.Sku)
            .Select(i => i.Sku)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastSku != null)
        {
            var parts = lastSku.Split('-');
            if (parts.Length > 1 && int.TryParse(parts.Last(), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{nextNumber:D5}";
    }

    private Guid GetOrganizationId()
    {
        // Use standard organizationId claim (lowercase following JWT conventions)
        var orgIdClaim = User.FindFirst("organizationId")?.Value;

        if (Guid.TryParse(orgIdClaim, out var orgId))
        {
            return orgId;
        }

        _logger.LogError("Organization ID not found in token. organizationId claim: {OrgIdClaim}", orgIdClaim ?? "NULL");
        throw new UnauthorizedAccessException("Organization ID not found in token");
    }

    // UPDATE - Edit item
    [HttpPut("{id}")]
    public async Task<ActionResult<ItemDetailDto>> UpdateItem(Guid id, [FromBody] UpdateItemRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var item = await _context.Items
                .Include(i => i.Consignor)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId);

            if (item == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Item not found"));
            }

            // Validate consignor belongs to organization
            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(p => p.Id == request.ConsignorId && p.OrganizationId == organizationId);

            if (consignor == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid consignor"));
            }

            // Check SKU uniqueness if changed
            if (!string.IsNullOrEmpty(request.Sku) && request.Sku != item.Sku)
            {
                var existingSku = await _context.Items
                    .AnyAsync(i => i.OrganizationId == organizationId && i.Sku == request.Sku && i.Id != id);

                if (existingSku)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("SKU already exists"));
                }
            }

            // Update item properties
            item.ConsignorId = request.ConsignorId;
            item.Sku = request.Sku ?? item.Sku;
            item.Barcode = request.Barcode;
            item.Title = request.Title;
            item.Description = request.Description;
            // TODO: Handle CategoryId based on request.Category
            item.Brand = request.Brand;
            item.Size = request.Size;
            item.Color = request.Color;
            item.Condition = request.Condition;
            item.Materials = request.Materials;
            item.Measurements = request.Measurements;
            item.Price = request.Price;
            item.OriginalPrice = request.OriginalPrice;
            item.MinimumPrice = request.MinimumPrice;

            if (request.ReceivedDate.HasValue)
            {
                item.ReceivedDate = DateOnly.FromDateTime(request.ReceivedDate.Value);
            }

            item.ExpirationDate = request.ExpirationDate.HasValue
                ? DateOnly.FromDateTime(request.ExpirationDate.Value)
                : null;

            item.Location = request.Location;
            item.Notes = request.Notes;
            item.InternalNotes = request.InternalNotes;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            // Return updated item
            return await GetItem(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to update item"));
        }
    }

    // DELETE - Permanently delete item (only if never sold)
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteItem(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var item = await _context.Items
                .Include(i => i.Transaction)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId);

            if (item == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Item not found"));
            }

            // Check if item was ever sold
            if (item.Transaction != null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete item that has been sold"));
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Item deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {ItemId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete item"));
        }
    }

    // STATUS CHANGE - Update item status
    [HttpPut("{id}/status")]
    public async Task<ActionResult<ItemDetailDto>> UpdateItemStatus(Guid id, [FromBody] UpdateItemStatusRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId);

            if (item == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Item not found"));
            }

            // Parse and validate status
            if (!Enum.TryParse<ItemStatus>(request.Status, out var newStatus))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid status"));
            }

            // Don't allow setting status to Sold via this endpoint
            if (newStatus == ItemStatus.Sold)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Items can only be marked as sold through transactions"));
            }

            item.Status = newStatus;
            item.StatusChangedAt = DateTime.UtcNow;
            item.StatusChangedReason = request.Reason;
            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            // Send notification to consignor if requested and item is being removed
            if (request.NotifyConsignor && newStatus == ItemStatus.Removed && item.ConsignorId != Guid.Empty)
            {
                try
                {
                    await _notificationService.CreateAsync(new ConsignmentGenie.Core.DTOs.Notifications.CreateNotificationRequest
                    {
                        OrganizationId = organizationId,
                        FromUserId = userId,
                        FromType = "owner",
                        ToUserId = item.ConsignorId,
                        ToType = "consignor",
                        Type = "item_removed",
                        Title = "Item Removed from Inventory",
                        Message = $"Your item '{item.Title}' (SKU: {item.Sku}) has been removed from inventory. Reason: {request.Reason ?? "No reason provided"}",
                        ItemId = item.Id,
                        ReferenceType = "item_status_change",
                        ReferenceId = item.Id,
                        Payload = new
                        {
                            ItemTitle = item.Title,
                            ItemSku = item.Sku,
                            RemovalReason = request.Reason ?? "No reason provided",
                            RemovedAt = DateTime.UtcNow,
                            RemovedBy = userId
                        }
                    });
                }
                catch (Exception notificationEx)
                {
                    // Log notification failure but don't fail the status update
                    _logger.LogError(notificationEx, "Failed to send notification for item removal {ItemId} to consignor {ConsignorId}",
                        item.Id, item.ConsignorId);
                }
            }

            // Return updated item
            return await GetItem(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item status {ItemId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to update item status"));
        }
    }

    // BULK STATUS UPDATE - Update multiple items status at once
    [HttpPost("bulk-status")]
    public async Task<ActionResult<BulkUpdateResultDto>> BulkUpdateStatus([FromBody] BulkStatusUpdateRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            // Validate request
            if (request.ItemIds == null || !request.ItemIds.Any())
            {
                return BadRequest(ApiResponse<object>.ErrorResult("No items specified"));
            }

            if (request.ItemIds.Count > 100)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Maximum 100 items allowed per bulk update"));
            }

            // Parse and validate status
            if (!Enum.TryParse<ItemStatus>(request.Status, out var newStatus))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid status"));
            }

            // Don't allow setting status to Sold via this endpoint
            if (newStatus == ItemStatus.Sold)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Items can only be marked as sold through transactions"));
            }

            var result = new BulkUpdateResultDto();
            var now = DateTime.UtcNow;

            // Get items that belong to this organization
            var items = await _context.Items
                .Where(i => request.ItemIds.Contains(i.Id) && i.OrganizationId == organizationId)
                .ToListAsync();

            foreach (var itemId in request.ItemIds)
            {
                var item = items.FirstOrDefault(i => i.Id == itemId);
                if (item == null)
                {
                    result.FailedUpdates++;
                    result.FailedItemIds.Add(itemId);
                    result.ErrorMessages.Add($"Item {itemId} not found");
                    continue;
                }

                try
                {
                    item.Status = newStatus;
                    item.StatusChangedAt = now;
                    item.StatusChangedReason = request.Reason;
                    item.UpdatedAt = now;
                    item.UpdatedBy = userId;

                    result.SuccessfulUpdates++;
                    result.UpdatedItemIds.Add(itemId);
                }
                catch (Exception ex)
                {
                    result.FailedUpdates++;
                    result.FailedItemIds.Add(itemId);
                    result.ErrorMessages.Add($"Failed to update item {itemId}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<BulkUpdateResultDto>.SuccessResult(result,
                $"Bulk update completed: {result.SuccessfulUpdates} successful, {result.FailedUpdates} failed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk status update");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to perform bulk status update"));
        }
    }

    // METRICS - Get inventory metrics for dashboard
    [HttpGet("metrics")]
    public async Task<ActionResult<InventoryMetricsDto>> GetMetrics()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);

            var items = await _context.Items
                .Include(i => i.Consignor)
                .Where(i => i.OrganizationId == organizationId)
                .ToListAsync();

            var metrics = new InventoryMetricsDto
            {
                TotalItems = items.Count,
                AvailableItems = items.Count(i => i.Status == ItemStatus.Available),
                SoldItems = items.Count(i => i.Status == ItemStatus.Sold),
                RemovedItems = items.Count(i => i.Status == ItemStatus.Removed),
                ItemsAddedThisMonth = items.Count(i => i.CreatedAt >= thisMonthStart),
                ItemsSoldThisMonth = items.Count(i => i.Status == ItemStatus.Sold &&
                                                    i.SoldDate.HasValue &&
                                                    i.SoldDate.Value.ToDateTime(TimeOnly.MinValue) >= thisMonthStart)
            };

            var availableItems = items.Where(i => i.Status == ItemStatus.Available).ToList();

            metrics.TotalValue = availableItems.Sum(i => i.Price);
            metrics.AveragePrice = availableItems.Any() ? availableItems.Average(i => i.Price) : 0;

            // Category breakdown
            metrics.ByCategory = availableItems
                .Where(i => i.ItemCategoryNavigation != null && !string.IsNullOrEmpty(i.ItemCategoryNavigation.Name))
                .GroupBy(i => i.ItemCategoryNavigation.Name)
                .Select(g => new CategoryBreakdownDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Value = g.Sum(i => i.Price)
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            // Consignor breakdown
            metrics.ByProvider = availableItems
                .GroupBy(i => new { i.ConsignorId, ConsignorName = (i.Consignor.FirstName + " " + i.Consignor.LastName).Trim() })
                .Select(g => new ConsignorBreakdownDto
                {
                    ConsignorId = g.Key.ConsignorId,
                    ConsignorName = g.Key.ConsignorName,
                    Count = g.Count(),
                    Value = g.Sum(i => i.Price)
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            return Ok(ApiResponse<InventoryMetricsDto>.SuccessResult(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory metrics for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to retrieve inventory metrics"));
        }
    }

    // BULK IMPORT - Import items from CSV
    [HttpPost("import")]
    public async Task<ActionResult<ImportResultDto>> ImportItems([FromForm] IFormFile file)
    {
        try
        {
            var organizationId = GetOrganizationId();

            // Check if Square integration is enabled (bulk import disabled in Square mode)
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            // For now, assume Square integration would be indicated by a future SquareConnected field
            // Since it's not implemented yet, we'll skip this check for now
            // if (organization?.SquareConnected == true)
            // {
            //     return StatusCode(403, ApiResponse<object>.ErrorResult("Bulk import is disabled when Square integration is enabled"));
            // }

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("No file provided"));
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Only CSV files are supported"));
            }

            // Check file size (5MB limit)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("File size exceeds 5MB limit"));
            }

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportFromCsvAsync(stream, organizationId);

            // Check for row limit (1000 recommended)
            const int maxRows = 1000;
            if (result.TotalRows > maxRows)
            {
                return BadRequest(ApiResponse<object>.ErrorResult($"Import exceeds {maxRows} row limit"));
            }

            var message = result.ErrorCount > 0
                ? $"Import completed with {result.SuccessCount} successful and {result.ErrorCount} failed items"
                : $"Import completed successfully. {result.SuccessCount} items imported";

            return Ok(ApiResponse<ImportResultDto>.SuccessResult(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing items for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Import failed: {ex.Message}"));
        }
    }

    // IMPORT TEMPLATE - Download CSV template with headers and sample data
    [HttpGet("import/template")]
    public ActionResult GetImportTemplate()
    {
        try
        {
            var templateBytes = _importService.GenerateTemplateAsync();
            return File(templateBytes, "text/csv", "item_import_template.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating import template");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to generate template"));
        }
    }

    // CHECK FILE DUPLICATE - Check if a file might be a duplicate based on first row hash
    [HttpPost("check-duplicate")]
    public async Task<ActionResult<object>> CheckFileDuplicate([FromBody] CheckFileDuplicateRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();

            if (string.IsNullOrEmpty(request.FirstDataRow))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("First data row is required"));
            }

            var result = await _fileTrackingService.CheckForDuplicateAsync(
                organizationId,
                request.FileName ?? "unknown",
                "bulk_import",
                request.FirstDataRow,
                request.RowCount);

            return Ok(ApiResponse<object>.SuccessResult(new
            {
                isDuplicate = result.IsDuplicate,
                lastUploadDate = result.LastUploadDate,
                lastFileName = result.LastFileName,
                hash = result.Hash
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file duplicate");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to check duplicate"));
        }
    }

    // BULK IMPORT JSON - Import items from JSON array (used by UI bulk import modal)
    [HttpPost("bulk-import")]
    public async Task<ActionResult<BulkImportResultDto>> BulkImportItems([FromBody] BulkImportRequest request)
    {
        try
        {
            _logger.LogInformation("🚀 API: Starting bulk import with {ItemCount} items", request.Items?.Count ?? 0);

            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            // Validate request
            if (request.Items == null || !request.Items.Any())
            {
                _logger.LogWarning("❌ API: No items provided in bulk import request");
                return BadRequest(ApiResponse<object>.ErrorResult("No items provided"));
            }

            if (request.Items.Count > 1000)
            {
                _logger.LogWarning("❌ API: Bulk import exceeds maximum of 1000 items: {ItemCount}", request.Items.Count);
                return BadRequest(ApiResponse<object>.ErrorResult("Maximum 1000 items allowed per bulk import"));
            }

            var result = new BulkImportResultDto
            {
                TotalItems = request.Items.Count,
                SuccessfulImports = 0,
                FailedImports = 0,
                Errors = new List<string>()
            };

            _logger.LogInformation("📊 API: Processing {ItemCount} items for organization {OrganizationId}", request.Items.Count, organizationId);

            // Track SKUs within this batch to prevent duplicates
            var skusInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in request.Items)
            {
                try
                {
                    _logger.LogDebug("🔄 API: Processing item: {Title}", item.Title);

                    // Validate consignor belongs to organization
                    var consignor = await _context.Consignors
                        .FirstOrDefaultAsync(p => p.Id == item.ConsignorId && p.OrganizationId == organizationId);

                    if (consignor == null)
                    {
                        var error = $"Invalid consignor ID {item.ConsignorId} for item '{item.Title}'";
                        _logger.LogWarning("⚠️ API: {Error}", error);
                        result.Errors.Add(error);
                        result.FailedImports++;
                        continue;
                    }

                    // Check if organization requires agreement on file
                    var organization = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.Id == organizationId);

                    // Check if organization requires agreement and consignor has one uploaded
                    if (organization?.AgreementMethod != "none")
                    {
                        var hasAgreement = await _context.ConsignorAgreements
                            .AnyAsync(a => a.ConsignorId == consignor.Id && a.Status == "uploaded");

                        if (!hasAgreement)
                        {
                            var error = $"Consignor '{consignor.DisplayName}' must have a signed agreement on file for item '{item.Title}'";
                            _logger.LogWarning("⚠️ API: {Error}", error);
                            result.Errors.Add(error);
                            result.FailedImports++;
                            continue;
                        }
                    }

                    // Generate SKU if not provided
                    var wasSkuProvided = !string.IsNullOrEmpty(item.Sku);
                    var sku = wasSkuProvided ? item.Sku : await GenerateSkuAsync(organizationId);

                    // Handle SKU duplicates (both existing DB SKUs and batch duplicates) by auto-incrementing
                    var originalSku = sku;
                    int counter = 1;
                    while (skusInBatch.Contains(sku) || await _context.Items.AnyAsync(i => i.OrganizationId == organizationId && i.Sku == sku))
                    {
                        // Try to extract base SKU and number if it already has a suffix
                        var baseSku = originalSku;
                        var match = System.Text.RegularExpressions.Regex.Match(originalSku, @"^(.+)-(\d+)$");
                        if (match.Success)
                        {
                            baseSku = match.Groups[1].Value;
                            if (int.TryParse(match.Groups[2].Value, out var existingCounter))
                            {
                                counter = Math.Max(counter, existingCounter + 1);
                            }
                        }

                        sku = $"{baseSku}-{counter:D3}"; // Format as 001, 002, etc.
                        counter++;
                    }

                    // Log if SKU was changed - only report as error if user provided the SKU
                    if (sku != originalSku)
                    {
                        var skuChangeMessage = $"SKU changed from '{originalSku}' to '{sku}' for item '{item.Title}' to avoid duplicates";
                        _logger.LogInformation("ℹ️ API: {Message}", skuChangeMessage);

                        // Only add to errors if the user provided the SKU (not auto-generated)
                        if (wasSkuProvided)
                        {
                            result.Errors.Add(skuChangeMessage);
                        }
                    }

                    // Add SKU to batch tracker
                    skusInBatch.Add(sku);

                    // Handle category lookup/creation
                    Guid? categoryId = null;
                    if (!string.IsNullOrWhiteSpace(item.Category))
                    {
                        // Look up existing ItemCategory by name
                        var existingCategory = await _context.ItemCategories
                            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId &&
                                                    c.Name == item.Category &&
                                                    c.IsActive);

                        if (existingCategory != null)
                        {
                            categoryId = existingCategory.Id;
                            _logger.LogDebug("🔗 API: Found existing category '{CategoryName}' for item '{Title}'", item.Category, item.Title);
                        }
                        else
                        {
                            // Create new ItemCategory
                            var newCategory = new ItemCategory
                            {
                                Id = Guid.NewGuid(),
                                OrganizationId = organizationId,
                                Name = item.Category,
                                IsActive = true,
                                SortOrder = 0
                            };

                            _context.ItemCategories.Add(newCategory);
                            await _context.SaveChangesAsync(); // Save immediately to get the ID
                            categoryId = newCategory.Id;
                            _logger.LogInformation("➕ API: Created new category '{CategoryName}' for item '{Title}'", item.Category, item.Title);
                        }
                    }

                    var newItem = new Item
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        ConsignorId = item.ConsignorId,
                        ItemCategoryId = categoryId,
                        Sku = sku,
                        Barcode = item.Barcode,
                        Title = item.Title,
                        Description = item.Description,
                        Brand = item.Brand,
                        Size = item.Size,
                        Color = item.Color,
                        Condition = item.Condition,
                        Materials = item.Materials,
                        Measurements = item.Measurements,
                        Price = item.Price,
                        OriginalPrice = item.OriginalPrice,
                        MinimumPrice = item.MinimumPrice,
                        Status = ItemStatus.Available,
                        ReceivedDate = item.ReceivedDate.HasValue
                            ? DateOnly.FromDateTime(item.ReceivedDate.Value)
                            : DateOnly.FromDateTime(DateTime.UtcNow),
                        ExpirationDate = item.ExpirationDate.HasValue
                            ? DateOnly.FromDateTime(item.ExpirationDate.Value)
                            : null,
                        Location = item.Location,
                        Notes = item.Notes,
                        InternalNotes = item.InternalNotes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedBy = userId
                    };

                    _context.Items.Add(newItem);
                    result.SuccessfulImports++;
                    _logger.LogDebug("✅ API: Successfully prepared item for save: {Title} with SKU {Sku}", item.Title, sku);
                }
                catch (Exception ex)
                {
                    var error = $"Error processing item '{item.Title}': {ex.Message}";
                    _logger.LogError(ex, "❌ API: {Error}", error);
                    result.Errors.Add(error);
                    result.FailedImports++;
                }
            }

            // Save all items to database
            _logger.LogInformation("💾 API: Saving {SuccessCount} items to database", result.SuccessfulImports);
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ API: Successfully saved all items to database");

            // Complete notification actions for dropoff manifests
            if (result.SuccessfulImports > 0 &&
                request.SourceType == "dropoff_manifest" &&
                request.SourceReferenceId.HasValue)
            {
                try
                {
                    await _notificationService.CompleteNotificationActionAsync(
                        request.SourceReferenceId.Value,
                        "dropoff_manifest",
                        userId);
                    _logger.LogInformation("✅ API: Completed dropoff manifest notification for {ReferenceId}",
                        request.SourceReferenceId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ API: Failed to complete notification action for dropoff manifest {ReferenceId}, but import was successful",
                        request.SourceReferenceId.Value);
                }
            }

            // Track file hash if successful import and file data provided
            if (result.SuccessfulImports > 0 && !string.IsNullOrEmpty(request.FirstDataRow))
            {
                try
                {
                    await _fileTrackingService.SaveFileHashAsync(
                        organizationId,
                        request.FileName ?? "bulk_import.csv",
                        "bulk_import",
                        request.FirstDataRow,
                        request.Items.Count);
                    _logger.LogInformation("📝 API: File hash saved for duplicate detection");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ API: Failed to save file hash, but import was successful");
                }
            }

            var message = result.FailedImports > 0
                ? $"Bulk import completed with {result.SuccessfulImports} successful and {result.FailedImports} failed items"
                : $"Bulk import completed successfully. {result.SuccessfulImports} items imported";

            _logger.LogInformation("🎉 API: Bulk import complete - {Message}", message);
            return Ok(ApiResponse<BulkImportResultDto>.SuccessResult(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 API: Error during bulk import for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<object>.ErrorResult($"Bulk import failed: {ex.Message}"));
        }
    }

    // PENDING ASSIGNMENT ENDPOINTS

    /// <summary>
    /// Get pending Square import items awaiting consignor assignment
    /// </summary>
    [HttpGet("pending-assignment")]
    public async Task<ActionResult<ApiResponse<ConsignmentGenie.Core.DTOs.PagedResult<PendingSquareImportDto>>>> GetPendingAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var pendingImports = await _assignmentService.GetPendingImportsAsync(organizationId);

            var dtos = pendingImports.Select(p => new PendingSquareImportDto
            {
                PendingImportId = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Sku = p.Sku,
                Category = p.Category,
                ImportedAt = p.ImportedAt,
                SquareCatalogId = p.SquareCatalogId
            }).ToList();

            // Simple pagination
            var skip = (page - 1) * pageSize;
            var pagedItems = dtos.Skip(skip).Take(pageSize).ToList();
            var totalCount = dtos.Count;

            var result = new ConsignmentGenie.Core.DTOs.PagedResult<PendingSquareImportDto>(pagedItems, totalCount, page, pageSize);

            return Ok(ApiResponse<ConsignmentGenie.Core.DTOs.PagedResult<PendingSquareImportDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending assignments for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<PagedResult<PendingSquareImportDto>>.ErrorResult($"Failed to retrieve pending assignments: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get count of pending assignment items
    /// </summary>
    [HttpGet("pending-assignment/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetPendingAssignmentCount()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var count = await _assignmentService.GetPendingCountAsync(organizationId);
            return Ok(ApiResponse<int>.SuccessResult(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending assignment count for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<int>.ErrorResult($"Failed to retrieve pending count: {ex.Message}"));
        }
    }

    /// <summary>
    /// Assign a pending import to a consignor (promotes to Items table)
    /// </summary>
    [HttpPut("{pendingImportId}/assign")]
    public async Task<ActionResult<ApiResponse<ItemDetailDto>>> AssignPendingImport(Guid pendingImportId, [FromBody] AssignConsignorRequest request)
    {
        try
        {
            var item = await _assignmentService.AssignConsignorAsync(pendingImportId, request.ConsignorId);

            // Get the consignor details for the DTO
            var consignor = await _context.Consignors.FindAsync(item.ConsignorId);

            var dto = new ItemDetailDto
            {
                ItemId = item.Id,
                ConsignorId = item.ConsignorId,
                ConsignorName = consignor?.DisplayName ?? "Unknown",
                CommissionRate = consignor?.CommissionRate ?? 0.5m,
                Sku = item.Sku ?? string.Empty,
                Title = item.Title,
                Description = item.Description,
                Condition = item.Condition,
                ConditionLabel = item.Condition.ToString(),
                Price = item.Price,
                ShopAmount = item.Price * (1 - (consignor?.CommissionRate ?? 0.5m)),
                ConsignorAmount = item.Price * (consignor?.CommissionRate ?? 0.5m),
                Status = item.Status,
                ReceivedDate = item.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt ?? item.CreatedAt
            };

            return Ok(ApiResponse<ItemDetailDto>.SuccessResult(dto));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<ItemDetailDto>.ErrorResult(ex.Message));
        }
        catch (ForbiddenException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning pending import {PendingImportId} to consignor {ConsignorId}",
                pendingImportId, request.ConsignorId);
            return StatusCode(500, ApiResponse<ItemDetailDto>.ErrorResult($"Failed to assign consignor: {ex.Message}"));
        }
    }

    /// <summary>
    /// Bulk assign multiple pending imports to a consignor
    /// </summary>
    [HttpPut("bulk-assign")]
    public async Task<ActionResult<ApiResponse<BulkAssignResponse>>> BulkAssignPendingImports([FromBody] BulkAssignRequest request)
    {
        try
        {
            var result = await _assignmentService.BulkAssignAsync(request.PendingImportIds, request.ConsignorId);

            var response = new BulkAssignResponse
            {
                Assigned = result.Assigned,
                Failed = result.Failed.Select(f => new FailedAssignmentDto
                {
                    PendingImportId = f.PendingImportId,
                    Reason = f.Reason
                }).ToList()
            };

            return Ok(ApiResponse<BulkAssignResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk assignment of {Count} imports to consignor {ConsignorId}",
                request.PendingImportIds.Count, request.ConsignorId);
            return StatusCode(500, ApiResponse<BulkAssignResponse>.ErrorResult($"Bulk assignment failed: {ex.Message}"));
        }
    }

    private Guid GetUserId()
    {
        var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(nameIdentifierClaim, out var userId))
            return userId;
        throw new UnauthorizedAccessException("User ID not found in token");
    }
}