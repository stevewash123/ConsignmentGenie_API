namespace ConsignmentGenie.Core.DTOs.Statements;

public class StatementSaleLineDto
{
    public DateTime Date { get; set; }
    public string ItemSku { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal EarningsAmount { get; set; }
}