namespace ConsignmentGenie.Application.DTOs.Shopper;

public class AuthResultDto
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public ShopperProfileDto? Profile { get; set; }
    public string? ErrorMessage { get; set; }
}