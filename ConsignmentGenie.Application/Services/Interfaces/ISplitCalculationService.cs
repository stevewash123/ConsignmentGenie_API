namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ISplitCalculationService
{
    SplitResult CalculateSplit(decimal salePrice, decimal splitPercentage);
    Task<PayoutSummary> CalculatePayoutsAsync(Guid providerId, DateTime periodStart, DateTime periodEnd);
}

public class SplitResult
{
    public decimal ProviderAmount { get; set; }
    public decimal ShopAmount { get; set; }
    public decimal SplitPercentage { get; set; }
}

public class PayoutSummary
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public List<PayoutTransaction> Transactions { get; set; } = new();
}

public class PayoutTransaction
{
    public Guid TransactionId { get; set; }
    public string ItemSku { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal ProviderAmount { get; set; }
    public DateTime SaleDate { get; set; }
}