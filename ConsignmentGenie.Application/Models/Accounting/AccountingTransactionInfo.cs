namespace ConsignmentGenie.Application.Models.Accounting;

public class AccountingTransactionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "sale", "refund", "expense", etc.
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public List<LineItem> LineItems { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}

public class LineItem
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? TaxCode { get; set; }
}