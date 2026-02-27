namespace ConsignmentGenie.Core.Enums;

public enum ItemStatus
{
    PendingAssignment = 0,  // Imported, needs consignor assignment
    Available = 1,
    Reserved = 2,          // Added for storefront cart timed holds
    Sold = 3,              // Renumbered to accommodate Reserved
    Removed = 4            // Updated to match spec (was Returned)
}