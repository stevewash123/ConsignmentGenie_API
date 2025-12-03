using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

public class StorefrontSettingsDto
{
    [Required]
    public string SelectedChannel { get; set; } = "cg-storefront";
    public SquareSettingsDto? Square { get; set; }
    public ShopifySettingsDto? Shopify { get; set; }
    public CgStorefrontSettingsDto? CgStorefront { get; set; }
    public InStoreSettingsDto? InStore { get; set; }
}

public class SquareSettingsDto
{
    public bool Connected { get; set; } = false;
    [MaxLength(100)]
    public string? BusinessName { get; set; }
    [MaxLength(100)]
    public string? LocationName { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public bool SyncInventory { get; set; } = true;
    public bool ImportSales { get; set; } = true;
    public bool SyncCustomers { get; set; } = false;
    [Required]
    public string SyncFrequency { get; set; } = "daily";
    public List<CategoryMappingDto> CategoryMappings { get; set; } = new();
}

public class ShopifySettingsDto
{
    public bool Connected { get; set; } = false;
    [MaxLength(100)]
    public string? StoreName { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public bool PushInventory { get; set; } = true;
    public bool ImportOrders { get; set; } = true;
    public bool SyncImages { get; set; } = true;
    public bool AutoMarkSold { get; set; } = true;
    public List<CollectionMappingDto> CollectionMappings { get; set; } = new();
}

public class CgStorefrontSettingsDto
{
    [Required]
    [MaxLength(50)]
    public string StoreSlug { get; set; } = "";
    [MaxLength(100)]
    public string? CustomDomain { get; set; }
    public bool DnsVerified { get; set; } = false;
    public bool StripeConnected { get; set; } = false;
    [MaxLength(100)]
    public string? StripeAccountName { get; set; }
    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }
    [Required]
    public string PrimaryColor { get; set; } = "#2563eb";
    [Required]
    public string AccentColor { get; set; } = "#1d4ed8";
    [MaxLength(160)]
    public string? MetaTitle { get; set; }
    [MaxLength(320)]
    public string? MetaDescription { get; set; }
}

public class InStoreSettingsDto
{
    public bool UseReceiptNumbers { get; set; } = true;
    [MaxLength(10)]
    public string? ReceiptPrefix { get; set; }
    public int NextReceiptNumber { get; set; } = 1;
    public bool RequireManagerApproval { get; set; } = false;
    public bool AllowLayaway { get; set; } = false;
}

public class CategoryMappingDto
{
    [Required]
    public string CgCategory { get; set; } = "";
    [Required]
    public string SquareCategory { get; set; } = "";
}

public class CollectionMappingDto
{
    [Required]
    public string CgCategory { get; set; } = "";
    [Required]
    public string ShopifyCollection { get; set; } = "";
}