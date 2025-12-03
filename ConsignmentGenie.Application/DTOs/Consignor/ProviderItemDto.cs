namespace ConsignmentGenie.Application.DTOs.Consignor;

public class ProviderItemDto
{
    public Guid ItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PrimaryImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal MyEarnings { get; set; }  // Price * commission rate
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public DateTime? SoldDate { get; set; }
    public decimal? SalePrice { get; set; }
}

public class ProviderItemDetailDto : ProviderItemDto
{
    public string Description { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class ProviderItemQueryParams
{
    public string? Status { get; set; }
    public string? Category { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}