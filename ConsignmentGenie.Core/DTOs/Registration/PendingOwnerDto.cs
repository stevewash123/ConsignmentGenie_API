namespace ConsignmentGenie.Core.DTOs.Registration;

public class PendingOwnerDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}