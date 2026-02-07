namespace ConsignmentGenie.Application.Models.PaymentGateway;

public class PaymentMethodInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "card", "bank_account", etc.
    public string Last4 { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty; // "visa", "mastercard", etc.
    public string Name { get; set; } = string.Empty; // Cardholder name
    public DateTime? ExpiryDate { get; set; }
    public bool IsDefault { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}