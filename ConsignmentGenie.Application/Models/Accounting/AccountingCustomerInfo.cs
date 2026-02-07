namespace ConsignmentGenie.Application.Models.Accounting;

public class AccountingCustomerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public AccountingAddress? BillingAddress { get; set; }
    public AccountingAddress? ShippingAddress { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AccountingAddress
{
    public string Line1 { get; set; } = string.Empty;
    public string Line2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}