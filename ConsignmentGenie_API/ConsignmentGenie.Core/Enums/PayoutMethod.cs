using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Enums;

public enum PayoutMethod
{
    [Display(Name = "Cash")]
    Cash = 1,

    [Display(Name = "Check")]
    Check = 2,

    [Display(Name = "Venmo")]
    Venmo = 3,

    [Display(Name = "PayPal")]
    PayPal = 4,

    [Display(Name = "Zelle")]
    Zelle = 5,

    [Display(Name = "Bank Transfer")]
    BankTransfer = 6,

    [Display(Name = "Other")]
    Other = 7
}