namespace ConsignmentGenie.Core.DTOs.Registration;

public class StoreCodeValidationDto
{
    public bool IsValid { get; set; }
    public string? ShopName { get; set; }
    public string? ErrorMessage { get; set; }
}