using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.DTOs.Settings;

public class ConsignorDefaultsDto
{
    [Range(1, 99, ErrorMessage = "Shop commission must be between 1 and 99 percent")]
    public decimal ShopCommissionPercent { get; set; } = 50M;

    [Range(1, 3650, ErrorMessage = "Consignment period must be between 1 and 3650 days")]
    public int ConsignmentPeriodDays { get; set; } = 90;

    [Range(1, 365, ErrorMessage = "Retrieval period must be between 1 and 365 days")]
    public int RetrievalPeriodDays { get; set; } = 14;

    [Required]
    public string UnsoldItemPolicy { get; set; } = "return-to-consignor";

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}