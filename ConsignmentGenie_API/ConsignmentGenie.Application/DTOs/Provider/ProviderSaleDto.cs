namespace ConsignmentGenie.Application.DTOs.Provider;

public class ProviderSaleDto
{
    public Guid TransactionId { get; set; }
    public DateTime SaleDate { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string ItemSku { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal MyEarnings { get; set; }
    public string PayoutStatus { get; set; } = string.Empty;  // Pending, Paid
}

public class ProviderSaleQueryParams
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? PayoutStatus { get; set; }  // All, Pending, Paid
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}