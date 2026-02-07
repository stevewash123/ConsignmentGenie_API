namespace ConsignmentGenie.Core.Services;

public interface IDwollaService
{
    /// <summary>
    /// Create or get a Dwolla Customer for the organization
    /// </summary>
    Task<DwollaCustomerResult> CreateOrGetCustomerAsync(
        Guid organizationId,
        string businessName,
        string email);

    /// <summary>
    /// Create a funding source using a Plaid processor token
    /// </summary>
    Task<DwollaFundingSourceResult> CreateFundingSourceAsync(
        string dwollaCustomerUrl,
        string plaidProcessorToken,
        string accountName);

    /// <summary>
    /// Remove/deactivate a funding source
    /// </summary>
    Task<bool> RemoveFundingSourceAsync(string fundingSourceUrl);

    /// <summary>
    /// Get the status of a funding source
    /// </summary>
    Task<string> GetFundingSourceStatusAsync(string fundingSourceUrl);
}

public class DwollaCustomerResult
{
    public bool Success { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerUrl { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DwollaFundingSourceResult
{
    public bool Success { get; set; }
    public string? FundingSourceId { get; set; }
    public string? FundingSourceUrl { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}