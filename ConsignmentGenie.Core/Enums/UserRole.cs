using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Enums;

/// <summary>
/// User roles with Display attributes for consistent API serialization
/// IMPORTANT: Keep these Display Name values in sync with frontend USER_ROLES constants
/// </summary>
public enum UserRole
{
    [Display(Name = "admin")]
    Admin = 0,       // System admin access

    [Display(Name = "owner")]
    Owner = 1,       // Full admin access

    [Display(Name = "consignor")]
    Consignor = 2,   // Consignor portal access

    [Display(Name = "customer")]
    Customer = 3,    // Customer storefront access

    [Display(Name = "clerk")]
    Clerk = 4        // POS-only access for shop employees
}