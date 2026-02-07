namespace ConsignmentGenie.Core.Enums;

public enum ItemSubmissionMode
{
    OwnerOnly = 0,        // Only owner/staff can add items
    ApprovalRequired = 1, // Consignors can submit items but need approval
    DirectAdd = 2         // Consignors can directly add items to inventory
}