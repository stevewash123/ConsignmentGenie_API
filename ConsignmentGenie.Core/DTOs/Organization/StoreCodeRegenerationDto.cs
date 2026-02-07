namespace ConsignmentGenie.Core.DTOs.Organization;

public class StoreCodeRegenerationDto
{
    public string NewStoreCode { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}