using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class StoreService : IStoreService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<StoreService> _logger;

    public StoreService(ConsignmentGenieContext context, ILogger<StoreService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StoreInfoDto?> GetStoreInfoAsync(string storeSlug)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Slug == storeSlug);

        if (organization == null)
        {
            return null;
        }

        return new StoreInfoDto
        {
            Slug = organization.Slug!,
            Name = organization.Name,
            Description = null, // TODO: Add to Organization entity
            LogoUrl = null, // TODO: Add to Organization entity
            Address = null, // TODO: Add to Organization entity
            Phone = null, // TODO: Add to Organization entity
            Email = null, // TODO: Add to Organization entity
            Hours = null, // TODO: Add store hours functionality
            ShippingEnabled = true, // TODO: Add to Organization entity
            ShippingFlatRate = 10.00m, // TODO: Add to Organization entity
            TaxRate = 0.085m // TODO: Add to Organization entity or settings
        };
    }

    public async Task<(List<PublicItemDto> items, int totalCount)> GetItemsAsync(string storeSlug, ItemQueryParams queryParams)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Slug == storeSlug);

        if (organization == null)
        {
            return (new List<PublicItemDto>(), 0);
        }

        var query = _context.Items
            .Where(i => i.OrganizationId == organization.Id && i.Status == ItemStatus.Available);

        // Apply search filter
        if (!string.IsNullOrEmpty(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower();
            query = query.Where(i =>
                i.Title.ToLower().Contains(searchLower) ||
                (i.Description != null && i.Description.ToLower().Contains(searchLower)) ||
                (i.Brand != null && i.Brand.ToLower().Contains(searchLower)) ||
                (i.Category != null && i.Category.ToLower().Contains(searchLower)));
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(queryParams.Category))
        {
            query = query.Where(i => i.Category == queryParams.Category);
        }

        // Apply price filters
        if (queryParams.MinPrice.HasValue)
        {
            query = query.Where(i => i.Price >= queryParams.MinPrice.Value);
        }

        if (queryParams.MaxPrice.HasValue)
        {
            query = query.Where(i => i.Price <= queryParams.MaxPrice.Value);
        }

        // Apply sorting
        query = queryParams.Sort.ToLower() switch
        {
            "price-low-high" => query.OrderBy(i => i.Price),
            "price-high-low" => query.OrderByDescending(i => i.Price),
            "name-a-z" => query.OrderBy(i => i.Title),
            "name-z-a" => query.OrderByDescending(i => i.Title),
            _ => query.OrderByDescending(i => i.ListedDate) // newest (default)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(i => new PublicItemDto
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                Price = i.Price,
                Category = i.Category,
                PrimaryImageUrl = i.PrimaryImageUrl,
                IsAvailable = i.Status == ItemStatus.Available,
                ListedDate = i.ListedDate.HasValue ? i.ListedDate.Value.ToDateTime(TimeOnly.MinValue) : null
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<PublicItemDetailDto?> GetItemDetailAsync(string storeSlug, Guid itemId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Slug == storeSlug);

        if (organization == null)
        {
            return null;
        }

        var item = await _context.Items
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.OrganizationId == organization.Id &&
                                    i.Id == itemId &&
                                    i.Status == ItemStatus.Available);

        if (item == null)
        {
            return null;
        }

        return new PublicItemDetailDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Price = item.Price,
            Category = item.Category,
            Condition = item.Condition.ToString(),
            Size = item.Size,
            Color = item.Color,
            Brand = item.Brand,
            Materials = item.Materials,
            Measurements = item.Measurements,
            Images = item.Images?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
            IsAvailable = item.Status == ItemStatus.Available,
            ListedDate = item.ListedDate.HasValue ? item.ListedDate.Value.ToDateTime(TimeOnly.MinValue) : null
        };
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(string storeSlug)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Slug == storeSlug);

        if (organization == null)
        {
            return new List<CategoryDto>();
        }

        var categories = await _context.Items
            .Where(i => i.OrganizationId == organization.Id &&
                       i.Status == ItemStatus.Available &&
                       i.Category != null)
            .GroupBy(i => i.Category!)
            .Select(g => new CategoryDto
            {
                Name = g.Key,
                ItemCount = g.Count()
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories;
    }
}