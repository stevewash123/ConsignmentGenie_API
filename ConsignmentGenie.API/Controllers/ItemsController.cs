using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.DTOs.Items;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.DTOs;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class ItemsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(ConsignmentGenieContext context, ILogger<ItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // LIST - Get items with filtering/pagination
    [HttpGet]
    public async Task<ActionResult<PagedResult<ItemListDto>>> GetItems([FromQuery] ItemQueryParams queryParams)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var query = _context.Items
                .Include(i => i.Provider)
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

            if (queryParams.ProviderId.HasValue)
            {
                query = query.Where(i => i.ProviderId == queryParams.ProviderId.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.Category))
            {
                query = query.Where(i => i.Category == queryParams.Category);
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
                    Category = i.Category,
                    Condition = i.Condition,
                    Status = i.Status,
                    PrimaryImageUrl = i.PrimaryImageUrl,
                    ReceivedDate = i.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                    SoldDate = i.SoldDate.HasValue ? i.SoldDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    ProviderId = i.ProviderId,
                    ProviderName = i.Provider.DisplayName,
                    CommissionRate = i.Provider.CommissionRate
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
                .Include(i => i.Provider)
                .Include(i => i.Images.OrderBy(img => img.DisplayOrder))
                .Include(i => i.Transaction)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId);

            if (item == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Item not found"));
            }

            var dto = new ItemDetailDto
            {
                ItemId = item.Id,
                ProviderId = item.ProviderId,
                ProviderName = item.Provider.DisplayName,
                CommissionRate = item.Provider.CommissionRate,
                Sku = item.Sku,
                Barcode = item.Barcode,
                Title = item.Title,
                Description = item.Description,
                Category = item.Category,
                Brand = item.Brand,
                Size = item.Size,
                Color = item.Color,
                Condition = item.Condition,
                Materials = item.Materials,
                Measurements = item.Measurements,
                Price = item.Price,
                OriginalPrice = item.OriginalPrice,
                MinimumPrice = item.MinimumPrice,
                ShopAmount = item.Price * (1 - item.Provider.CommissionRate / 100),
                ProviderAmount = item.Price * (item.Provider.CommissionRate / 100),
                Status = item.Status,
                StatusChangedAt = item.StatusChangedAt,
                StatusChangedReason = item.StatusChangedReason,
                ReceivedDate = item.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                ListedDate = item.ListedDate?.ToDateTime(TimeOnly.MinValue),
                ExpirationDate = item.ExpirationDate?.ToDateTime(TimeOnly.MinValue),
                SoldDate = item.SoldDate?.ToDateTime(TimeOnly.MinValue),
                Images = item.Images.Select(img => new ItemImageDto
                {
                    ItemImageId = img.Id,
                    ImageUrl = img.ImageUrl,
                    DisplayOrder = img.DisplayOrder,
                    IsPrimary = img.IsPrimary
                }).ToList(),
                Location = item.Location,
                Notes = item.Notes,
                InternalNotes = item.InternalNotes,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt ?? item.CreatedAt,
                TransactionId = item.Transaction?.Id,
                SalePrice = item.Transaction?.SalePrice
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

            // Validate provider belongs to organization
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId && p.OrganizationId == organizationId);

            if (provider == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid provider"));
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
                ProviderId = request.ProviderId,
                Sku = sku,
                Barcode = request.Barcode,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
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
                .Include(i => i.Provider)
                .FirstAsync(i => i.Id == item.Id);

            var dto = new ItemDetailDto
            {
                ItemId = savedItem.Id,
                ProviderId = savedItem.ProviderId,
                ProviderName = savedItem.Provider.DisplayName,
                CommissionRate = savedItem.Provider.CommissionRate,
                Sku = savedItem.Sku,
                Barcode = savedItem.Barcode,
                Title = savedItem.Title,
                Description = savedItem.Description,
                Category = savedItem.Category,
                Brand = savedItem.Brand,
                Size = savedItem.Size,
                Color = savedItem.Color,
                Condition = savedItem.Condition,
                Materials = savedItem.Materials,
                Measurements = savedItem.Measurements,
                Price = savedItem.Price,
                OriginalPrice = savedItem.OriginalPrice,
                MinimumPrice = savedItem.MinimumPrice,
                ShopAmount = savedItem.Price * (1 - savedItem.Provider.CommissionRate / 100),
                ProviderAmount = savedItem.Price * (savedItem.Provider.CommissionRate / 100),
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
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        return orgIdClaim != null ? Guid.Parse(orgIdClaim) : Guid.Empty;
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
                .Include(i => i.Provider)
                .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId);

            if (item == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Item not found"));
            }

            // Validate provider belongs to organization
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == request.ProviderId && p.OrganizationId == organizationId);

            if (provider == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid provider"));
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
            item.ProviderId = request.ProviderId;
            item.Sku = request.Sku ?? item.Sku;
            item.Barcode = request.Barcode;
            item.Title = request.Title;
            item.Description = request.Description;
            item.Category = request.Category;
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

            // Return updated item
            return await GetItem(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item status {ItemId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to update item status"));
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }
}