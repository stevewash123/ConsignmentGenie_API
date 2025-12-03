namespace ConsignmentGenie.Application.DTOs.Consignor;

public class ConsignorDashboardDto
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
    public List<ConsignorSaleDto> RecentSales { get; set; } = new();  // Last 5
    public ConsignorPayoutDto? LastPayout { get; set; }
}