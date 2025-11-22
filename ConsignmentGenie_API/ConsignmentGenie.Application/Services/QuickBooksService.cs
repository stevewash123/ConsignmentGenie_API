using ConsignmentGenie.Application.DTOs.QuickBooks;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public interface IQuickBooksService
{
    string GetAuthorizationUrl(string state);
    Task<QuickBooksTokenResponse> ExchangeCodeForTokensAsync(string code, string realmId, string state);
    Task<bool> RefreshTokenAsync(string organizationId);
    Task SyncTransactionsAsync(string organizationId);
    Task CreateCustomerAsync(string organizationId, Provider provider);
    Task CreatePaymentAsync(string organizationId, Payout payout);
}

public class QuickBooksService : IQuickBooksService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuickBooksService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;

    // QuickBooks Online OAuth endpoints
    private const string AuthorizationUrl = "https://appcenter.intuit.com/connect/oauth2";
    private const string TokenEndpoint = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
    private const string BaseApiUrl = "https://sandbox-quickbooks.api.intuit.com";

    public QuickBooksService(
        IConfiguration configuration,
        ILogger<QuickBooksService> logger,
        HttpClient httpClient,
        IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
    }

    public string GetAuthorizationUrl(string state)
    {
        var clientId = _configuration["QuickBooks:ClientId"];
        var redirectUri = _configuration["QuickBooks:RedirectUri"];
        var scope = "com.intuit.quickbooks.accounting";

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = clientId!,
            ["scope"] = scope,
            ["redirect_uri"] = redirectUri!,
            ["response_type"] = "code",
            ["access_type"] = "offline",
            ["state"] = state
        };

        var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
        return $"{AuthorizationUrl}?{queryString}";
    }

    public async Task<QuickBooksTokenResponse> ExchangeCodeForTokensAsync(string code, string realmId, string state)
    {
        try
        {
            var clientId = _configuration["QuickBooks:ClientId"];
            var clientSecret = _configuration["QuickBooks:ClientSecret"];
            var redirectUri = _configuration["QuickBooks:RedirectUri"];

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri!)
            });

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new("Basic", authString);

            var response = await _httpClient.PostAsync(TokenEndpoint, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QuickBooks token exchange failed: {Response}", responseContent);
                throw new HttpRequestException($"Token exchange failed: {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<QuickBooksTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (tokenResponse == null)
                throw new InvalidOperationException("Failed to parse token response");

            // Store tokens in organization
            await StoreTokensAsync(state, tokenResponse, realmId);

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for tokens");
            throw;
        }
    }

    public async Task<bool> RefreshTokenAsync(string organizationId)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization?.QuickBooksRefreshToken == null)
                return false;

            var clientId = _configuration["QuickBooks:ClientId"];
            var clientSecret = _configuration["QuickBooks:ClientSecret"];

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", organization.QuickBooksRefreshToken)
            });

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new("Basic", authString);

            var response = await _httpClient.PostAsync(TokenEndpoint, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("QuickBooks token refresh failed: {Response}", responseContent);
                return false;
            }

            var tokenResponse = JsonSerializer.Deserialize<QuickBooksTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (tokenResponse == null)
                return false;

            // Update stored tokens
            organization.QuickBooksAccessToken = tokenResponse.AccessToken;
            organization.QuickBooksRefreshToken = tokenResponse.RefreshToken;
            organization.QuickBooksTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            await _unitOfWork.Organizations.UpdateAsync(organization);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing QuickBooks token for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    public async Task SyncTransactionsAsync(string organizationId)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization?.QuickBooksAccessToken == null)
                throw new InvalidOperationException("QuickBooks not connected for this organization");

            // Get unsynchronized transactions
            var transactions = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == organization.Id && !t.SyncedToQuickBooks);

            foreach (var transaction in transactions)
            {
                await SyncTransactionToQuickBooksAsync(organization, transaction);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing transactions to QuickBooks for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task CreateCustomerAsync(string organizationId, Provider provider)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization?.QuickBooksAccessToken == null)
                return;

            var customerData = new
            {
                Name = provider.DisplayName,
                CompanyName = provider.BusinessName,
                PrimaryEmailAddr = new { Address = provider.Email },
                PrimaryPhone = new { FreeFormNumber = provider.Phone },
                BillAddr = new
                {
                    Line1 = provider.Address,
                    City = provider.City,
                    Country = "US",
                    PostalCode = provider.ZipCode
                }
            };

            await SendQuickBooksApiRequestAsync(organization, "POST", $"v3/company/{organization.QuickBooksRealmId}/customer", customerData);

            _logger.LogInformation("Created QuickBooks customer for provider {ProviderId}", provider.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating QuickBooks customer for provider {ProviderId}", provider.Id);
        }
    }

    public async Task CreatePaymentAsync(string organizationId, Payout payout)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
            if (organization?.QuickBooksAccessToken == null)
                return;

            // Create payment record in QuickBooks
            var paymentData = new
            {
                TotalAmt = payout.TotalAmount,
                CustomerRef = new { value = payout.Provider.QuickBooksCustomerId },
                TxnDate = payout.PaidAt?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                PrivateNote = $"Consignment payout for period {payout.PeriodStart:yyyy-MM-dd} to {payout.PeriodEnd:yyyy-MM-dd}"
            };

            var response = await SendQuickBooksApiRequestAsync(organization, "POST",
                $"v3/company/{organization.QuickBooksRealmId}/payment", paymentData);

            // Update payout with QuickBooks sync info
            payout.SyncedToQuickBooks = true;
            payout.QuickBooksBillId = ExtractQuickBooksId(response);

            await _unitOfWork.Payouts.UpdateAsync(payout);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating QuickBooks payment for payout {PayoutId}", payout.Id);
        }
    }

    private async Task StoreTokensAsync(string organizationId, QuickBooksTokenResponse tokenResponse, string realmId)
    {
        var organization = await _unitOfWork.Organizations.GetByIdAsync(Guid.Parse(organizationId));
        if (organization == null)
            throw new InvalidOperationException("Organization not found");

        organization.QuickBooksConnected = true;
        organization.QuickBooksRealmId = realmId;
        organization.QuickBooksAccessToken = tokenResponse.AccessToken;
        organization.QuickBooksRefreshToken = tokenResponse.RefreshToken;
        organization.QuickBooksTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        await _unitOfWork.Organizations.UpdateAsync(organization);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task SyncTransactionToQuickBooksAsync(Organization organization, Transaction transaction)
    {
        try
        {
            // Create sales receipt in QuickBooks for consignment sales
            var salesReceiptData = new
            {
                TotalAmt = transaction.SalePrice,
                TxnDate = transaction.SaleDate.ToString("yyyy-MM-dd"),
                CustomerRef = new { value = "1" }, // Default customer for retail sales
                Line = new[]
                {
                    new
                    {
                        Amount = transaction.ShopAmount,
                        DetailType = "SalesItemLineDetail",
                        SalesItemLineDetail = new
                        {
                            ItemRef = new { value = "1" }, // Default item
                            Qty = 1,
                            UnitPrice = transaction.ShopAmount
                        }
                    }
                }
            };

            await SendQuickBooksApiRequestAsync(organization, "POST",
                $"v3/company/{organization.QuickBooksRealmId}/salesreceipt", salesReceiptData);

            transaction.SyncedToQuickBooks = true;
            await _unitOfWork.Transactions.UpdateAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing transaction {TransactionId} to QuickBooks", transaction.Id);
        }
    }

    private async Task<string> SendQuickBooksApiRequestAsync(Organization organization, string method, string endpoint, object? data = null)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", organization.QuickBooksAccessToken);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var url = $"{BaseApiUrl}/{endpoint}";

        HttpResponseMessage response;
        if (method == "POST" && data != null)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(url, content);
        }
        else
        {
            response = await _httpClient.GetAsync(url);
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("QuickBooks API request failed: {Response}", responseContent);
            throw new HttpRequestException($"QuickBooks API error: {response.StatusCode}");
        }

        return responseContent;
    }

    private static string? ExtractQuickBooksId(string response)
    {
        try
        {
            var doc = JsonDocument.Parse(response);
            return doc.RootElement
                .GetProperty("QueryResponse")
                .GetProperty("Payment")[0]
                .GetProperty("Id")
                .GetString();
        }
        catch
        {
            return null;
        }
    }
}