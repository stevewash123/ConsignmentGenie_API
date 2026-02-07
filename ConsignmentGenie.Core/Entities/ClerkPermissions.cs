using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class ClerkPermissions : BaseEntity
{
    [Required]
    public Guid OrganizationId { get; set; }

    // What clerks can see
    public bool ShowConsignorNames { get; set; } = false;
    public bool ShowItemCost { get; set; } = false;

    // What clerks can do without PIN
    public bool AllowReturns { get; set; } = true;
    public decimal MaxReturnAmountWithoutPin { get; set; } = 50.00m;
    public bool AllowDiscounts { get; set; } = false;
    public int MaxDiscountPercentWithoutPin { get; set; } = 0;
    public bool AllowVoid { get; set; } = false;
    public bool AllowDrawerOpen { get; set; } = false;
    public bool AllowEndOfDayCount { get; set; } = true;
    public bool AllowPriceOverride { get; set; } = false;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}