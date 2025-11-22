using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Enums;

public enum ItemCondition
{
    [Display(Name = "New")]
    New = 1,

    [Display(Name = "Like New")]
    LikeNew = 2,

    [Display(Name = "Excellent")]
    Excellent = 3,

    [Display(Name = "Good")]
    Good = 4,

    [Display(Name = "Fair")]
    Fair = 5,

    [Display(Name = "Poor")]
    Poor = 6
}