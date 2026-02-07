namespace ConsignmentGenie.Core.Services;

public interface IQuickBooksApiService
{
    Task<QBSyncResult> CreateVendor(QBVendor vendor, string companyId);
    Task<QBSyncResult> CreateBill(QBBill bill, string companyId);
    Task<QBSyncResult> CreateBillPayment(QBBillPayment payment, string companyId);
    Task<QBSyncResult> CreateExpense(QBExpense expense, string companyId);
    Task<QBSyncResult> CreateSalesReceipt(QBSalesReceipt receipt, string companyId);
    Task<QBVendor?> GetVendorByName(string name, string companyId);
    Task<bool> ValidateConnection(string companyId);
}

public record QBApiConfig
{
    public string BaseUrl { get; init; } = "";
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public string RedirectUri { get; init; } = "";
    public string Scope { get; init; } = "com.intuit.quickbooks.accounting";
    public bool IsSandbox { get; init; } = true;
}