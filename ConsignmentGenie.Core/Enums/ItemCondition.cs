using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Enums;

public enum ItemCondition
{
    [Display(Name = "New")]
    New = 1,        // Brand new, tags attached

    [Display(Name = "Like New")]
    LikeNew = 2,    // Excellent condition, barely used

    [Display(Name = "Good")]
    Good = 3,       // Normal wear, good condition

    [Display(Name = "Fair")]
    Fair = 4,       // Visible wear, still functional

    [Display(Name = "Poor")]
    Poor = 5        // Significant wear, priced accordingly
}