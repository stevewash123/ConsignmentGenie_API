namespace ConsignmentGenie.Application.DTOs.Shopper;

public class ShopperProfileDto
{
    public Guid ShopperId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    public bool EmailNotifications { get; set; }
    public DateTime MemberSince { get; set; }
}

public class AddressDto
{
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
}