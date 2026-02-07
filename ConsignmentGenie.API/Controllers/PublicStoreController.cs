using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/storefront/{storeSlug}")]
public class PublicStoreController : ControllerBase
{
    private readonly IStoreService _storeService;
    private readonly ILogger<PublicStoreController> _logger;

    public PublicStoreController(IStoreService storeService, ILogger<PublicStoreController> logger)
    {
        _storeService = storeService;
        _logger = logger;
    }

    /// <summary>
    /// Get store information
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>Store information</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<StoreInfoDto>>> GetStoreInfo(string storeSlug)
    {
        try
        {
            var storeInfo = await _storeService.GetStoreInfoAsync(storeSlug);

            if (storeInfo == null)
            {
                return NotFound(ApiResponse<StoreInfoDto>.ErrorResult("Store not found"));
            }

            return Ok(ApiResponse<StoreInfoDto>.SuccessResult(storeInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store info for slug {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<StoreInfoDto>.ErrorResult("An error occurred retrieving store information"));
        }
    }

    /// <summary>
    /// Get catalog items with filtering and pagination
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="search">Search query</param>
    /// <param name="category">Category filter</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="sort">Sort option</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paged catalog items</returns>
    [HttpGet("items")]
    public async Task<ActionResult<ApiResponse<PagedResult<PublicItemDto>>>> GetItems(
        string storeSlug,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        try
        {
            var queryParams = new ItemQueryParams
            {
                Search = search,
                Category = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Sort = sort,
                Page = page,
                PageSize = pageSize
            };

            var (items, totalCount) = await _storeService.GetItemsAsync(storeSlug, queryParams);

            if (!items.Any() && page == 1)
            {
                // Check if store exists
                var storeInfo = await _storeService.GetStoreInfoAsync(storeSlug);
                if (storeInfo == null)
                {
                    return NotFound(ApiResponse<PagedResult<PublicItemDto>>.ErrorResult("Store not found"));
                }
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var result = new PagedResult<PublicItemDto>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<PublicItemDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving items for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<PagedResult<PublicItemDto>>.ErrorResult("An error occurred retrieving items"));
        }
    }

    /// <summary>
    /// Get item detail
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <param name="itemId">Item ID</param>
    /// <returns>Item detail</returns>
    [HttpGet("items/{itemId}")]
    public async Task<ActionResult<ApiResponse<PublicItemDetailDto>>> GetItemDetail(string storeSlug, Guid itemId)
    {
        try
        {
            var itemDetail = await _storeService.GetItemDetailAsync(storeSlug, itemId);

            if (itemDetail == null)
            {
                return NotFound(ApiResponse<PublicItemDetailDto>.ErrorResult("Item not found or not available"));
            }

            return Ok(ApiResponse<PublicItemDetailDto>.SuccessResult(itemDetail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item {ItemId} for store {Slug}", itemId, storeSlug);
            return StatusCode(500, ApiResponse<PublicItemDetailDto>.ErrorResult("An error occurred retrieving item"));
        }
    }

    /// <summary>
    /// Get available categories
    /// </summary>
    /// <param name="storeSlug">Store slug identifier</param>
    /// <returns>List of categories with item counts</returns>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories(string storeSlug)
    {
        try
        {
            var categories = await _storeService.GetCategoriesAsync(storeSlug);

            return Ok(ApiResponse<List<CategoryDto>>.SuccessResult(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories for store {Slug}", storeSlug);
            return StatusCode(500, ApiResponse<List<CategoryDto>>.ErrorResult("An error occurred retrieving categories"));
        }
    }
}