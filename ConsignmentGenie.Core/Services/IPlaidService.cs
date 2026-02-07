namespace ConsignmentGenie.Core.Services;

public interface IPlaidService
{
    Task<decimal> GetAccountBalance(string accessToken, string accountId);
    Task<bool> ValidateConnection(string accessToken);
    Task<PlaidAccountInfo[]> GetAccounts(string accessToken);

    /// <summary>
    /// Create a Plaid Link token for the frontend to initiate bank linking
    /// </summary>
    Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(Guid organizationId, Guid? userId = null);

    /// <summary>
    /// Exchange a public token for an access token and create a processor token for Dwolla
    /// </summary>
    Task<PlaidExchangeResult> ExchangePublicTokenAsync(string publicToken, string accountId);
}

public class PlaidLinkTokenResponse
{
    public string LinkToken { get; set; } = null!;
    public DateTime Expiration { get; set; }
}

public class PlaidExchangeResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? ProcessorToken { get; set; }  // Token specifically for Dwolla
    public string? ErrorMessage { get; set; }
}

public record PlaidAccountInfo
{
    public string AccountId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Subtype { get; init; } = "";
    public decimal Balance { get; init; }
    public string Last4 { get; init; } = "";
}