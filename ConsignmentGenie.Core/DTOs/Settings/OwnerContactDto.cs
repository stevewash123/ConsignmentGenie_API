namespace ConsignmentGenie.Core.DTOs.Settings;

public class OwnerContactDto
{
    public string Name { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}