using System.ComponentModel.DataAnnotations;
using ConsignmentGenie.Core.Enums;

namespace ConsignmentGenie.Application.DTOs.Payout;

public class CreatePayoutRequestDto
{
    [Required]
    public Guid ConsignorId { get; set; }

    [Required]
    public DateTime PayoutDate { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(100)]
    public string? PaymentReference { get; set; }

    [Required]
    public DateTime PeriodStart { get; set; }

    [Required]
    public DateTime PeriodEnd { get; set; }

    public string? Notes { get; set; }

    [Required]
    public List<Guid> TransactionIds { get; set; } = new();
}

public class UpdatePayoutRequestDto
{
    public DateTime? PayoutDate { get; set; }
    public PayoutStatus? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}

public class PayoutSearchRequestDto
{
    public Guid? ConsignorId { get; set; }
    public DateTime? PayoutDateFrom { get; set; }
    public DateTime? PayoutDateTo { get; set; }
    public PayoutStatus? Status { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "PayoutDate";
    public string? SortDirection { get; set; } = "desc";
}

public class PendingPayoutsRequestDto
{
    public Guid? ConsignorId { get; set; }
    public DateTime? PeriodEndBefore { get; set; }
    public decimal? MinimumAmount { get; set; }
}