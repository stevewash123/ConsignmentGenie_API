namespace ConsignmentGenie.Application.DTOs.Shopper;

/// <summary>
/// Paginated catalog response for shoppers
/// </summary>
public class ShopperCatalogDto
{
    public List<ShopperItemListDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public ShopperCatalogFiltersDto Filters { get; set; } = new();
}

/// <summary>
/// Applied filters for catalog request
/// </summary>
public class ShopperCatalogFiltersDto
{
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Condition { get; set; }
    public string? Size { get; set; }
    public string SortBy { get; set; } = "ListedDate";
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Available categories for filtering
/// </summary>
public class ShopperCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

/// <summary>
/// Search results response for shoppers
/// </summary>
public class ShopperSearchResultDto
{
    public List<ShopperItemListDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string SearchQuery { get; set; } = string.Empty;
    public ShopperCatalogFiltersDto Filters { get; set; } = new();
}