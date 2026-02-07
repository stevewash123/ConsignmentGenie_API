namespace ConsignmentGenie.Application.DTOs.Consignor;

public class ConsignorPayoutDto
{
    public Guid PayoutId { get; set; }
    public string PayoutNumber { get; set; } = string.Empty;
    public DateTime PayoutDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class ConsignorPayoutDetailDto
{
    public Guid PayoutId { get; set; }
    public string PayoutNumber { get; set; } = string.Empty;
    public DateTime PayoutDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public List<ConsignorSaleDto> Items { get; set; } = new();
}