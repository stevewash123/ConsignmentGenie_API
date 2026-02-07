namespace ConsignmentGenie.Application.DTOs.Shopper;

public class GuestSessionDto
{
    public string SessionToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}