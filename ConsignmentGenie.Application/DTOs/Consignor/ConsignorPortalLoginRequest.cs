using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Application.DTOs.Consignor;

public class ConsignorLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string InviteCode { get; set; } = string.Empty;
}

public class ConsignorRegistrationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string InviteCode { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ConsignorDashboardResponse
{
    public string ConsignorName { get; set; } = string.Empty;
    public decimal TotalEarnings { get; set; }
    public decimal PendingPayouts { get; set; }
    public int ActiveItems { get; set; }
    public int SoldItems { get; set; }
    public DateTime? LastPayoutDate { get; set; }
    public decimal CommissionRate { get; set; }
}

public class ConsignorItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
    public DateTime? DateSold { get; set; }
    public decimal? ConsignorAmount { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}

public class ConsignorPayoutResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? PayoutDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public List<ConsignorItemResponse> Items { get; set; } = new();
}