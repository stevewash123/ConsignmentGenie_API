namespace ConsignmentGenie.Core.Services;

public interface IQBSyncService
{
    Task SyncSale(Guid transactionId);
    Task SyncVendor(Guid consignorId);
    Task SyncPayoutCreated(Guid payoutId);
    Task SyncPayoutPaid(Guid payoutId);
    Task<bool> EnsureVendorExists(Guid consignorId);
    Task<string?> GetQBVendorId(Guid consignorId);
}

public record QBSyncResult
{
    public bool Success { get; init; }
    public string? QBId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SyncedAt { get; init; } = DateTime.UtcNow;
}

public record QBVendor
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public QBAddress? BillAddr { get; init; }
}

public record QBAddress
{
    public string? Line1 { get; init; }
    public string? Line2 { get; init; }
    public string? City { get; init; }
    public string? CountrySubDivisionCode { get; init; } // State
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

public record QBBill
{
    public string VendorRef { get; init; } = "";
    public DateTime TxnDate { get; init; }
    public decimal TotalAmt { get; init; }
    public string? DocNumber { get; init; }
    public string? PrivateNote { get; init; }
    public List<QBBillLine> Lines { get; init; } = new();
}

public record QBBillLine
{
    public decimal Amount { get; init; }
    public string? Description { get; init; }
}

public record QBBillPayment
{
    public string VendorRef { get; init; } = "";
    public DateTime TxnDate { get; init; }
    public decimal TotalAmt { get; init; }
    public string? CheckNum { get; init; }
    public List<QBLinkedTxn> LinkedTxns { get; init; } = new();
}

public record QBLinkedTxn
{
    public string TxnId { get; init; } = "";
    public string TxnType { get; init; } = "";
    public decimal TxnLineId { get; init; }
}

public record QBExpense
{
    public string VendorRef { get; init; } = "";
    public DateTime TxnDate { get; init; }
    public decimal TotalAmt { get; init; }
    public string? DocNumber { get; init; }
    public string? PrivateNote { get; init; }
    public string PaymentType { get; init; } = "Check"; // Check, Cash, CreditCard
    public List<QBExpenseLine> Lines { get; init; } = new();
}

public record QBExpenseLine
{
    public decimal Amount { get; init; }
    public string? Description { get; init; }
    public string AccountRef { get; init; } = ""; // Expense account
}

public record QBSalesReceipt
{
    public DateTime TxnDate { get; init; }
    public decimal TotalAmt { get; init; }
    public string? DocNumber { get; init; }
    public string? PrivateNote { get; init; }
    public string? PaymentMethodRef { get; init; }
    public List<QBSalesReceiptLine> Lines { get; init; } = new();
}

public record QBSalesReceiptLine
{
    public decimal Amount { get; init; }
    public string? Description { get; init; }
    public string? ItemRef { get; init; }
    public int Qty { get; init; } = 1;
}