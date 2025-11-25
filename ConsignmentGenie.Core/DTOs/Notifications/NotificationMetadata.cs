namespace ConsignmentGenie.Core.DTOs.Notifications;

public class NotificationMetadata
{
    // For item_sold
    public string? ItemTitle { get; set; }
    public string? ItemSku { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? EarningsAmount { get; set; }

    // For payout_processed
    public decimal? PayoutAmount { get; set; }
    public string? PayoutMethod { get; set; }
    public string? PayoutNumber { get; set; }

    // For statement_ready
    public string? StatementPeriod { get; set; }
    public Guid? StatementId { get; set; }
}