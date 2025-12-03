namespace ConsignmentGenie.Application.DTOs.Consignor;

public class ProviderPayoutDto
{
    public Guid PayoutId { get; set; }
    public string PayoutNumber { get; set; } = string.Empty;
    public DateTime PayoutDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class ProviderPayoutDetailDto : ProviderPayoutDto
{
    public string PaymentReference { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<ProviderSaleDto> Items { get; set; } = new();
}