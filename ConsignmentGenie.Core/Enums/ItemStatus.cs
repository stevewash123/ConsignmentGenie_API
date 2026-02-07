namespace ConsignmentGenie.Core.Enums;

public enum ItemStatus
{
    PendingAssignment = 0,  // Imported, needs consignor assignment
    Available = 1,
    Sold = 2,
    Removed = 3  // Updated to match spec (was Returned)
}