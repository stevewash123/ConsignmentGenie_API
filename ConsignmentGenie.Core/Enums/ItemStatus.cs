namespace ConsignmentGenie.Core.Enums;

public enum ItemStatus
{
    Available = 1,         // Ready for sale (items only enter main table after consignor assignment)
    Reserved = 2,          // Storefront cart timed holds
    Sold = 3,              // Completed sale
    Removed = 4            // Withdrawn from sale
}