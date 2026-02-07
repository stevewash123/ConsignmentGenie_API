namespace ConsignmentGenie.Core.DTOs.Statements;

public class StatementPayoutLineDto
{
    public DateTime Date { get; set; }
    public string PayoutNumber { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}