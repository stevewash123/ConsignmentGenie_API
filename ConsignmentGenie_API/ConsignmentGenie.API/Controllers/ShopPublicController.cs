using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/shop/{storeSlug}")]
public class ShopPublicController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ShopPublicController> _logger;

    public ShopPublicController(ConsignmentGenieContext context, ILogger<ShopPublicController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get store info (name, logo, hours, etc.)
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Store information</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<StoreInfoDto>>> GetStoreInfo(string storeSlug)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == storeSlug);

            if (organization == null)
            {
                return NotFound(ApiResponse<StoreInfoDto>.ErrorResult("Store not found"));
            }

            var storeInfo = new StoreInfoDto
            {
                OrganizationId = organization.Id,
                Name = organization.Name,
                Slug = organization.Slug!,
                Description = null, // TODO: Add description field to Organization entity in future
                LogoUrl = null, // TODO: Add logo URL field to Organization entity in future
                Address = null, // TODO: Add address fields to Organization entity in future
                Phone = null, // TODO: Add phone field to Organization entity in future
                Email = null, // TODO: Add email field to Organization entity in future
                Hours = null, // TODO: Add store hours functionality in future
                IsOpen = true // TODO: Implement business hours logic in future
            };

            return Ok(ApiResponse<StoreInfoDto>.SuccessResult(storeInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store info for slug {StoreSlug}", storeSlug);
            return StatusCode(500, ApiResponse<StoreInfoDto>.ErrorResult("An error occurred retrieving store information"));
        }
    }

    /// <summary>
    /// Get catalog with pagination and filters
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="category">Category filter</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="condition">Condition filter</param>
    /// <param name="size">Size filter</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged catalog items</returns>
    [HttpGet("items")]
    public async Task<ActionResult<ApiResponse<ShopperCatalogDto>>> GetItems(
        string storeSlug,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? condition = null,
        [FromQuery] string? size = null,
        [FromQuery] string sortBy = "ListedDate",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == storeSlug);

            if (organization == null)
            {
                return NotFound(ApiResponse<ShopperCatalogDto>.ErrorResult("Store not found"));
            }

            // Build query for available items in this store
            var query = _context.Items
                .Where(i => i.OrganizationId == organization.Id &&
                           i.Status == ItemStatus.Available)
                .Include(i => i.ItemCategory)
                .Include(i => i.ItemImages);

            // Apply filters
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(i => i.ItemCategory != null && i.ItemCategory.Name == category);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(i => i.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(i => i.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(condition))
            {
                if (Enum.TryParse<ItemCondition>(condition, true, out var conditionEnum))
                {
                    query = query.Where(i => i.Condition == conditionEnum);
                }
            }

            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(i => i.Size == size);
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "price" => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.Price)
                    : query.OrderByDescending(i => i.Price),
                "title" => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.Title)
                    : query.OrderByDescending(i => i.Title),
                "condition" => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.Condition)
                    : query.OrderByDescending(i => i.Condition),
                _ => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.ListedDate)
                    : query.OrderByDescending(i => i.ListedDate)
            };

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var itemDtos = items.Select(item => new ShopperItemListDto
            {
                ItemId = item.Id,
                Title = item.Title,
                Description = item.Description,
                Price = item.Price,
                Category = item.ItemCategory?.Name,
                Brand = item.Brand,
                Size = item.Size,
                Color = item.Color,
                Condition = item.Condition,
                PrimaryImageUrl = item.ItemImages
                    .Where(img => img.IsPrimary)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault(),
                ListedDate = item.ListedDate,
                Images = item.ItemImages
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => new ShopperItemImageDto
                    {
                        ImageId = img.Id,
                        ImageUrl = img.ImageUrl,
                        AltText = img.AltText,
                        DisplayOrder = img.DisplayOrder,
                        IsPrimary = img.IsPrimary
                    })
                    .ToList()
            }).ToList();

            var result = new ShopperCatalogDto
            {
                Items = itemDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Filters = new ShopperCatalogFiltersDto
                {
                    Category = category,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    Condition = condition,
                    Size = size,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                }
            };

            return Ok(ApiResponse<ShopperCatalogDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items for store {StoreSlug}", storeSlug);
            return StatusCode(500, ApiResponse<ShopperCatalogDto>.ErrorResult("An error occurred retrieving items"));
        }
    }

    /// <summary>
    /// Get single item detail
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="itemId">Item ID</param>
    /// <returns>Item detail</returns>
    [HttpGet("items/{itemId}")]
    public async Task<ActionResult<ApiResponse<ShopperItemDetailDto>>> GetItem(string storeSlug, Guid itemId)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == storeSlug);

            if (organization == null)
            {
                return NotFound(ApiResponse<ShopperItemDetailDto>.ErrorResult("Store not found"));
            }

            var item = await _context.Items
                .Where(i => i.OrganizationId == organization.Id &&
                           i.Id == itemId &&
                           i.Status == ItemStatus.Available)
                .Include(i => i.ItemCategory)
                .Include(i => i.ItemImages)
                .FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound(ApiResponse<ShopperItemDetailDto>.ErrorResult("Item not found"));
            }

            var itemDto = new ShopperItemDetailDto
            {
                ItemId = item.Id,
                Title = item.Title,
                Description = item.Description,
                Price = item.Price,
                Category = item.ItemCategory?.Name,
                Brand = item.Brand,
                Size = item.Size,
                Color = item.Color,
                Condition = item.Condition,
                Materials = item.Materials,
                Measurements = item.Measurements,
                IsAvailable = item.Status == ItemStatus.Available,
                ListedDate = item.ListedDate,
                Images = item.ItemImages
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => new ShopperItemImageDto
                    {
                        ImageId = img.Id,
                        ImageUrl = img.ImageUrl,
                        AltText = img.AltText,
                        DisplayOrder = img.DisplayOrder,
                        IsPrimary = img.IsPrimary
                    })
                    .ToList()
            };

            return Ok(ApiResponse<ShopperItemDetailDto>.SuccessResult(itemDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item {ItemId} for store {StoreSlug}", itemId, storeSlug);
            return StatusCode(500, ApiResponse<ShopperItemDetailDto>.ErrorResult("An error occurred retrieving item"));
        }
    }

    /// <summary>
    /// Get categories for this store
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>List of categories with item counts</returns>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<ShopperCategoryDto>>>> GetCategories(string storeSlug)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == storeSlug);

            if (organization == null)
            {
                return NotFound(ApiResponse<List<ShopperCategoryDto>>.ErrorResult("Store not found"));
            }

            // Get categories with available item counts for this store
            var categories = await _context.Items
                .Where(i => i.OrganizationId == organization.Id &&
                           i.Status == ItemStatus.Available &&
                           i.ItemCategory != null)
                .Include(i => i.ItemCategory)
                .GroupBy(i => i.ItemCategory!.Name)
                .Select(g => new ShopperCategoryDto
                {
                    Name = g.Key,
                    ItemCount = g.Count()
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(ApiResponse<List<ShopperCategoryDto>>.SuccessResult(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories for store {StoreSlug}", storeSlug);
            return StatusCode(500, ApiResponse<List<ShopperCategoryDto>>.ErrorResult("An error occurred retrieving categories"));
        }
    }

    /// <summary>
    /// Search items
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="q">Search query</param>
    /// <param name="category">Category filter</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="condition">Condition filter</param>
    /// <param name="size">Size filter</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged search results</returns>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<ShopperSearchResultDto>>> SearchItems(
        string storeSlug,
        [FromQuery] string q,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? condition = null,
        [FromQuery] string? size = null,
        [FromQuery] string sortBy = "Relevance",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == storeSlug);

            if (organization == null)
            {
                return NotFound(ApiResponse<ShopperSearchResultDto>.ErrorResult("Store not found"));
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(ApiResponse<ShopperSearchResultDto>.ErrorResult("Search query is required"));
            }

            // Build search query for available items in this store
            var query = _context.Items
                .Where(i => i.OrganizationId == organization.Id &&
                           i.Status == ItemStatus.Available)
                .Include(i => i.ItemCategory)
                .Include(i => i.ItemImages);

            // Apply text search across multiple fields
            var searchTerms = q.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in searchTerms)
            {
                query = query.Where(i =>
                    i.Title.ToLower().Contains(term) ||
                    (i.Description != null && i.Description.ToLower().Contains(term)) ||
                    (i.Brand != null && i.Brand.ToLower().Contains(term)) ||
                    (i.Color != null && i.Color.ToLower().Contains(term)) ||
                    (i.Materials != null && i.Materials.ToLower().Contains(term)) ||
                    (i.ItemCategory != null && i.ItemCategory.Name.ToLower().Contains(term)));
            }

            // Apply the same filters as GetItems
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(i => i.ItemCategory != null && i.ItemCategory.Name == category);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(i => i.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(i => i.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(condition))
            {
                if (Enum.TryParse<ItemCondition>(condition, true, out var conditionEnum))
                {
                    query = query.Where(i => i.Condition == conditionEnum);
                }
            }

            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(i => i.Size == size);
            }

            // Apply sorting (relevance = title matches first, then newest)
            query = sortBy.ToLower() switch
            {
                "price" => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.Price)
                    : query.OrderByDescending(i => i.Price),
                "title" => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.Title)
                    : query.OrderByDescending(i => i.Title),
                "condition" => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.Condition)
                    : query.OrderByDescending(i => i.Condition),
                "relevance" => query.OrderByDescending(i =>
                    (i.Title.ToLower().Contains(q.ToLower()) ? 100 : 0) +
                    (i.Description != null && i.Description.ToLower().Contains(q.ToLower()) ? 50 : 0) +
                    (i.Brand != null && i.Brand.ToLower().Contains(q.ToLower()) ? 25 : 0))
                    .ThenByDescending(i => i.ListedDate),
                _ => sortDirection.ToLower() == "asc"
                    ? query.OrderBy(i => i.ListedDate)
                    : query.OrderByDescending(i => i.ListedDate)
            };

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var itemDtos = items.Select(item => new ShopperItemListDto
            {
                ItemId = item.Id,
                Title = item.Title,
                Description = item.Description,
                Price = item.Price,
                Category = item.ItemCategory?.Name,
                Brand = item.Brand,
                Size = item.Size,
                Color = item.Color,
                Condition = item.Condition,
                PrimaryImageUrl = item.ItemImages
                    .Where(img => img.IsPrimary)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault(),
                ListedDate = item.ListedDate,
                Images = item.ItemImages
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => new ShopperItemImageDto
                    {
                        ImageId = img.Id,
                        ImageUrl = img.ImageUrl,
                        AltText = img.AltText,
                        DisplayOrder = img.DisplayOrder,
                        IsPrimary = img.IsPrimary
                    })
                    .ToList()
            }).ToList();

            var result = new ShopperSearchResultDto
            {
                Items = itemDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                SearchQuery = q,
                Filters = new ShopperCatalogFiltersDto
                {
                    Category = category,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    Condition = condition,
                    Size = size,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                }
            };

            return Ok(ApiResponse<ShopperSearchResultDto>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching items for store {StoreSlug} with query {Query}", storeSlug, q);
            return StatusCode(500, ApiResponse<ShopperSearchResultDto>.ErrorResult("An error occurred searching items"));
        }
    }
}