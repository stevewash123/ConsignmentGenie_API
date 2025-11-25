namespace ConsignmentGenie.Application.DTOs.Transaction;

public class SalesMetricsDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalShopAmount { get; set; }
    public decimal TotalProviderAmount { get; set; }
    public decimal TotalTax { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionValue { get; set; }

    public List<ProviderSalesDto> TopProviders { get; set; } = new();
    public List<PaymentMethodBreakdownDto> PaymentMethodBreakdown { get; set; } = new();

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class ProviderSalesDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalProviderAmount { get; set; }
}

public class PaymentMethodBreakdownDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Total { get; set; }
}