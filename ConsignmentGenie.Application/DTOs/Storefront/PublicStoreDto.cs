using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Storefront;

public class StoreInfoDto
{
    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public StoreHoursDto? Hours { get; set; }
    public bool ShippingEnabled { get; set; }
    public decimal? ShippingFlatRate { get; set; }
    public decimal TaxRate { get; set; }
}

public class StoreHoursDto
{
    public string? Monday { get; set; }
    public string? Tuesday { get; set; }
    public string? Wednesday { get; set; }
    public string? Thursday { get; set; }
    public string? Friday { get; set; }
    public string? Saturday { get; set; }
    public string? Sunday { get; set; }
}

public class PublicItemDto
{
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    public string? Category { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime? ListedDate { get; set; }
}

public class PublicItemDetailDto
{
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    public string? Category { get; set; }
    public string? Condition { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Brand { get; set; }
    public string? Materials { get; set; }
    public string? Measurements { get; set; }

    public List<string> Images { get; set; } = new();
    public bool IsAvailable { get; set; }

    public DateTime? ListedDate { get; set; }
}

public class CategoryDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public int ItemCount { get; set; }
}

public class ItemQueryParams
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "newest";  // newest, price-low-high, price-high-low, name-a-z
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}