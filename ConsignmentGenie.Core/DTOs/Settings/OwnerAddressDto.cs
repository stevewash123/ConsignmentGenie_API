namespace ConsignmentGenie.Core.DTOs.Settings;

public class OwnerAddressDto
{
    public string? ShopAddress1 { get; set; }
    public string? ShopAddress2 { get; set; }
    public string? ShopCity { get; set; }
    public string? ShopState { get; set; }
    public string? ShopZip { get; set; }
    public string ShopCountry { get; set; } = "US";
}