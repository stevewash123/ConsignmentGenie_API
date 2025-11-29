namespace ConsignmentGenie.Core.Enums;

public enum UserRole
{
    Admin = 0,       // System admin access
    Owner = 1,       // Full admin access
    Provider = 2,    // Provider portal access
    Customer = 3,    // Customer storefront access
    Clerk = 4        // POS-only access for shop employees
}