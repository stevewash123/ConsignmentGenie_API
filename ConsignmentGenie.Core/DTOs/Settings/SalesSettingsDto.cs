using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

public class SalesSettingsDto
{
    [Required]
    public string ShopChoice { get; set; } = "consignmentgenie";

    /// <summary>
    /// Square connection details - only populated when ShopChoice is "square"
    /// </summary>
    public SquareConnectionDto? SquareConnection { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class SquareConnectionDto
{
    public bool Connected { get; set; } = false;

    [MaxLength(100)]
    public string? MerchantId { get; set; }

    [MaxLength(100)]
    public string? MerchantName { get; set; }

    public DateTime? ConnectedAt { get; set; }

    public DateTime? LastSync { get; set; }

    public int ItemCount { get; set; } = 0;

    public string? Error { get; set; }
}