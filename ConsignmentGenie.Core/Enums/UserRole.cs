namespace ConsignmentGenie.Core.Enums;

public enum UserRole
{
    Admin = 0,       // System admin access
    Owner = 1,       // Full admin access
    Consignor = 2,    // Consignor portal access
    Customer = 3,    // Customer storefront access
    Clerk = 4        // POS-only access for shop employees
}