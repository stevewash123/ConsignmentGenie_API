using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/public/storefront")]
[AllowAnonymous]
public class PublicStorefrontController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<PublicStorefrontController> _logger;

    public PublicStorefrontController(ConsignmentGenieContext context, ILogger<PublicStorefrontController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult> GetStorefrontInfo(string slug)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == slug);

            if (organization == null)
                return NotFound(new { message = "Store not found" });

            var storeInfo = new
            {
                id = organization.Id,
                name = organization.Name,
                slug = organization.Slug,
                storeCode = organization.StoreCode,
                description = organization.ShopDescription,
                logoUrl = organization.ShopLogoUrl,
                bannerUrl = organization.ShopBannerUrl,
                address = new
                {
                    street = organization.ShopAddress1,
                    city = organization.ShopCity,
                    state = organization.ShopState,
                    zip = organization.ShopZip,
                    country = organization.ShopCountry
                },
                contact = new
                {
                    phone = organization.ShopPhone,
                    email = organization.ShopEmail,
                    website = organization.ShopWebsite
                },
                timezone = organization.ShopTimezone
            };

            return Ok(storeInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storefront info for slug {Slug}", slug);
            return StatusCode(500, new { message = "Failed to retrieve store information" });
        }
    }

    [HttpGet("{slug}/items")]
    public async Task<ActionResult> GetAvailableItems(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = null)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == slug);

            if (organization == null)
                return NotFound(new { message = "Store not found" });

            // Build the query
            IQueryable<Item> query = _context.Items
                .Where(i => i.OrganizationId == organization.Id && i.Status == ItemStatus.Available)
                .Include(i => i.Consignor)
                .Include(i => i.ItemCategoryNavigation);

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i =>
                    i.Title.Contains(search) ||
                    (i.Description != null && i.Description.Contains(search)) ||
                    (i.Brand != null && i.Brand.Contains(search)));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(i => i.ItemCategoryNavigation != null && i.ItemCategoryNavigation.Name.Contains(category));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(i => i.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(i => i.Price <= maxPrice.Value);
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(i => i.Price),
                "price_desc" => query.OrderByDescending(i => i.Price),
                "name" => query.OrderBy(i => i.Title),
                "newest" => query.OrderByDescending(i => i.CreatedAt),
                _ => query.OrderBy(i => i.Title) // default sort
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    id = i.Id,
                    title = i.Title,
                    description = i.Description,
                    price = i.Price,
                    brand = i.Brand,
                    size = i.Size,
                    color = i.Color,
                    condition = i.Condition.ToString(),
                    category = i.ItemCategoryNavigation != null ? i.ItemCategoryNavigation.Name : null,
                    primaryImageUrl = i.PrimaryImageUrl,
                    consignorName = i.Consignor != null ? i.Consignor.Name : null,
                    createdAt = i.CreatedAt
                })
                .ToListAsync();

            var result = new
            {
                items = items,
                totalCount = totalCount,
                totalPages = totalPages,
                currentPage = page,
                pageSize = pageSize,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items for store {Slug}", slug);
            return StatusCode(500, new { message = "Failed to retrieve store items" });
        }
    }
}