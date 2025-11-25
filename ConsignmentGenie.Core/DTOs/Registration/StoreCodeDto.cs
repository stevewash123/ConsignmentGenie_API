namespace ConsignmentGenie.Core.DTOs.Registration;

public class StoreCodeDto
{
    public string StoreCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? LastRegenerated { get; set; }
}