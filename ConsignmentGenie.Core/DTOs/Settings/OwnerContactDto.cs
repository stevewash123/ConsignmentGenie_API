namespace ConsignmentGenie.Core.DTOs.Settings;

public class OwnerContactDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}