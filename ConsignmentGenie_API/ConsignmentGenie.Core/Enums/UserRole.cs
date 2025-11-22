namespace ConsignmentGenie.Core.Enums;

public enum UserRole
{
    Owner = 1,       // Full admin access (Phase 5: renamed from ShopOwner)
    Manager = 2,     // All operations, no financial settings (Phase 5)
    Staff = 3,       // Item entry, transaction processing (Phase 5)
    Cashier = 4,     // POS operations only (Phase 5)
    Accountant = 5,  // Financial reports, payouts only (Phase 5)
    Provider = 6,    // Provider portal access (Phase 3)
    Customer = 7     // Customer storefront access (Phase 5: renamed from Shopper)
}