using ConsignmentGenie.Application.Models.Accounting;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IAccountingService
{
    string ConsignorName { get; }

    // Authentication & Connection
    Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);
    Task<bool> ExchangeAuthorizationCodeAsync(string code, string redirectUri);
    Task<bool> RefreshTokenAsync();
    Task<bool> TestConnectionAsync();

    // Customer Management
    Task<string> CreateCustomerAsync(AccountingCustomerInfo customer);
    Task<AccountingCustomerInfo?> GetCustomerAsync(string customerId);
    Task<List<AccountingCustomerInfo>> GetCustomersAsync(int limit = 100, int offset = 0);
    Task<bool> UpdateCustomerAsync(string customerId, AccountingCustomerInfo customer);
    Task<bool> DeleteCustomerAsync(string customerId);

    // Transaction Management
    Task<string> CreateTransactionAsync(AccountingTransactionInfo transaction);
    Task<AccountingTransactionInfo?> GetTransactionAsync(string transactionId);
    Task<List<AccountingTransactionInfo>> GetTransactionsAsync(DateTime? startDate = null, DateTime? endDate = null, int limit = 100, int offset = 0);
    Task<bool> UpdateTransactionAsync(string transactionId, AccountingTransactionInfo transaction);
    Task<bool> DeleteTransactionAsync(string transactionId);

    // Sync Operations
    Task<List<AccountingTransactionInfo>> SyncTransactionsAsync(DateTime? since = null);
    Task<List<AccountingCustomerInfo>> SyncCustomersAsync(DateTime? since = null);
    Task<Dictionary<string, object>> GetSyncStatusAsync();

    // Consignor-specific features
    Task<bool> SupportsFunctionality(string functionality); // "invoicing", "inventory", "reports", etc.
    Task<Dictionary<string, object>> GetProviderSpecificDataAsync(string entityId, string entityType);

    // Webhooks
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret);
    Task HandleWebhookAsync(string payload, Dictionary<string, object> headers);
}