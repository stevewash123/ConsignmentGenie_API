namespace ConsignmentGenie.Application.DTOs.Consignor;

public class ProviderDashboardDto
{
    public string ShopName { get; set; } = string.Empty;
    public string ConsignorName { get; set; } = string.Empty;

    // Items
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int SoldItems { get; set; }
    public decimal InventoryValue { get; set; }

    // Earnings
    public decimal PendingBalance { get; set; }      // Unpaid earnings
    public decimal TotalEarningsAllTime { get; set; }
    public decimal EarningsThisMonth { get; set; }

    // Recent activity
    public List<ProviderSaleDto> RecentSales { get; set; } = new();  // Last 5
    public ProviderPayoutDto? LastPayout { get; set; }
}