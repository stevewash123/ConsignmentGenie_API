namespace ConsignmentGenie.Core.Enums;

public enum SyncDirection
{
    ToShopify = 0,        // One-way: ConsignmentGenie -> Shopify
    FromShopify = 1,      // One-way: Shopify -> ConsignmentGenie
    Bidirectional = 2     // Two-way sync
}