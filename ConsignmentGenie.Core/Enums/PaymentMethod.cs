using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Enums;

public enum PaymentMethod
{
    [Display(Name = "Cash")]
    Cash = 1,

    [Display(Name = "Credit Card")]
    CreditCard = 2,

    [Display(Name = "Debit Card")]
    DebitCard = 3,

    [Display(Name = "Check")]
    Check = 4,

    [Display(Name = "Online")]
    Online = 5,

    [Display(Name = "Other")]
    Other = 6
}