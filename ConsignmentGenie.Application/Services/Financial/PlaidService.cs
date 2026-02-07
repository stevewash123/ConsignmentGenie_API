using ConsignmentGenie.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services.Financial;

public class PlaidService : IPlaidService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PlaidService> _logger;
    private readonly string _clientId;
    private readonly string _secret;
    private readonly string _environment;

    public PlaidService(
        HttpClient httpClient,
        ILogger<PlaidService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clientId = configuration["Plaid:ClientId"] ?? throw new InvalidOperationException("Plaid ClientId not configured");
        _secret = configuration["Plaid:Secret"] ?? configuration["Plaid:SandboxSecret"] ?? throw new InvalidOperationException("Plaid Secret not configured");
        _environment = configuration["Plaid:Environment"] ?? "sandbox";

        // Set base URL based on environment
        _httpClient.BaseAddress = _environment.ToLower() switch
        {
            "production" => new Uri("https://production.plaid.com/"),
            "development" => new Uri("https://development.plaid.com/"),
            _ => new Uri("https://sandbox.plaid.com/")
        };
    }

    public async Task<decimal> GetAccountBalance(string accessToken, string accountId)
    {
        _logger.LogInformation("Retrieving account balance for account {AccountId}", accountId);

        try
        {
            var request = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken,
                account_ids = new[] { accountId }
            };

            var response = await _httpClient.PostAsJsonAsync("/accounts/balance/get", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var balanceResponse = JsonSerializer.Deserialize<PlaidBalanceResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (balanceResponse?.Accounts?.FirstOrDefault() is var account && account != null)
            {
                var balance = account.Balances.Available ?? account.Balances.Current ?? 0;
                _logger.LogInformation("Retrieved balance {Balance} for account {AccountId}", balance, accountId);
                return balance;
            }

            _logger.LogWarning("No account found with ID {AccountId}", accountId);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error retrieving balance for account {AccountId}", accountId);
            throw new InvalidOperationException("Failed to retrieve account balance from Plaid", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Plaid response for account {AccountId}", accountId);
            throw new InvalidOperationException("Invalid response from Plaid", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving balance for account {AccountId}", accountId);
            throw;
        }
    }

    public async Task<bool> ValidateConnection(string accessToken)
    {
        _logger.LogInformation("Validating Plaid connection");

        try
        {
            var request = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken
            };

            var response = await _httpClient.PostAsJsonAsync("/accounts/get", request);
            var success = response.IsSuccessStatusCode;

            _logger.LogInformation("Plaid connection validation {Result}", success ? "successful" : "failed");
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plaid connection validation failed");
            return false;
        }
    }

    public async Task<PlaidAccountInfo[]> GetAccounts(string accessToken)
    {
        _logger.LogInformation("Retrieving Plaid accounts");

        try
        {
            var request = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken
            };

            var response = await _httpClient.PostAsJsonAsync("/accounts/get", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var accountResponse = JsonSerializer.Deserialize<PlaidAccountsResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var accounts = accountResponse?.Accounts?.Select(a => new PlaidAccountInfo
            {
                AccountId = a.AccountId,
                Name = a.Name,
                Type = a.Type,
                Subtype = a.Subtype,
                Balance = a.Balances.Available ?? a.Balances.Current ?? 0,
                Last4 = a.Mask ?? ""
            }).ToArray() ?? Array.Empty<PlaidAccountInfo>();

            _logger.LogInformation("Retrieved {Count} accounts from Plaid", accounts.Length);
            return accounts;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error retrieving Plaid accounts");
            throw new InvalidOperationException("Failed to retrieve accounts from Plaid", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Plaid accounts response");
            throw new InvalidOperationException("Invalid response from Plaid", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving Plaid accounts");
            throw;
        }
    }

    public async Task<string> CreateLinkToken(Guid userId, Guid organizationId)
    {
        _logger.LogInformation("Creating Plaid Link token for user {UserId} in organization {OrganizationId}",
            userId, organizationId);

        try
        {
            var request = new
            {
                client_id = _clientId,
                secret = _secret,
                client_name = "ConsignmentGenie",
                country_codes = new[] { "US" },
                language = "en",
                user = new
                {
                    client_user_id = userId.ToString()
                },
                products = new[] { "auth", "transactions" },
                account_filters = new
                {
                    depository = new
                    {
                        account_subtypes = new[] { "checking", "savings" }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/link/token/create", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var linkTokenResponse = JsonSerializer.Deserialize<PlaidLinkTokenCreateResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (linkTokenResponse?.LinkToken == null)
            {
                throw new InvalidOperationException("Failed to create Plaid Link token");
            }

            _logger.LogInformation("Successfully created Plaid Link token for user {UserId}", userId);
            return linkTokenResponse.LinkToken;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating Plaid Link token");
            throw new InvalidOperationException("Failed to create Plaid Link token", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Plaid Link token response");
            throw new InvalidOperationException("Invalid response from Plaid", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating Plaid Link token");
            throw;
        }
    }


    public async Task<PlaidLinkTokenResponse> CreateLinkTokenAsync(Guid organizationId, Guid? userId = null)
    {
        var clientUserId = userId?.ToString() ?? organizationId.ToString();

        var request = new
        {
            client_id = _clientId,
            secret = _secret,
            user = new { client_user_id = clientUserId },
            client_name = "ConsignmentGenie",
            products = new[] { "auth" },
            country_codes = new[] { "US" },
            language = "en"
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/link/token/create", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var linkTokenResponse = JsonSerializer.Deserialize<PlaidLinkTokenApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (linkTokenResponse?.LinkToken == null)
            {
                throw new InvalidOperationException("Failed to create Plaid Link token");
            }

            return new PlaidLinkTokenResponse
            {
                LinkToken = linkTokenResponse.LinkToken,
                Expiration = linkTokenResponse.Expiration
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating Plaid Link token");
            throw new InvalidOperationException("Failed to create Plaid Link token", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Plaid Link token response");
            throw new InvalidOperationException("Invalid response from Plaid", ex);
        }
    }

    public async Task<PlaidExchangeResult> ExchangePublicTokenAsync(string publicToken, string accountId)
    {
        try
        {
            // Step 1: Exchange public token for access token
            var exchangeRequest = new
            {
                client_id = _clientId,
                secret = _secret,
                public_token = publicToken
            };

            _logger.LogInformation("Sending Plaid public token exchange request with client_id: {ClientId}, secret: {Secret}",
                _clientId, _secret?.Substring(0, Math.Min(4, _secret.Length)) + "***");

            _logger.LogInformation("Request payload: {RequestPayload}", System.Text.Json.JsonSerializer.Serialize(exchangeRequest));

            var exchangeResponse = await _httpClient.PostAsJsonAsync("/item/public_token/exchange", exchangeRequest);

            _logger.LogInformation("Plaid exchange response status: {StatusCode}", exchangeResponse.StatusCode);
            if (!exchangeResponse.IsSuccessStatusCode)
            {
                var errorContent = await exchangeResponse.Content.ReadAsStringAsync();
                _logger.LogError("Plaid exchange failed with status {StatusCode}. Response: {ErrorResponse}",
                    exchangeResponse.StatusCode, errorContent);
            }

            exchangeResponse.EnsureSuccessStatusCode();

            var exchangeContent = await exchangeResponse.Content.ReadAsStringAsync();
            var exchangeResult = JsonSerializer.Deserialize<PlaidAccessTokenResponse>(exchangeContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (exchangeResult?.AccessToken == null)
            {
                throw new InvalidOperationException("Failed to exchange public token");
            }

            // Step 2: Create processor token for Dwolla
            var processorRequest = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = exchangeResult.AccessToken,
                account_id = accountId,
                processor = "dwolla"
            };

            _logger.LogInformation("Creating Dwolla processor token with account_id: {AccountId}", accountId);
            _logger.LogInformation("Processor request payload: {ProcessorRequestPayload}", System.Text.Json.JsonSerializer.Serialize(processorRequest));

            var processorResponse = await _httpClient.PostAsJsonAsync("/processor/token/create", processorRequest);

            _logger.LogInformation("Processor token response status: {StatusCode}", processorResponse.StatusCode);
            if (!processorResponse.IsSuccessStatusCode)
            {
                var processorErrorContent = await processorResponse.Content.ReadAsStringAsync();
                _logger.LogError("Processor token creation failed with status {StatusCode}. Response: {ErrorResponse}",
                    processorResponse.StatusCode, processorErrorContent);
            }

            processorResponse.EnsureSuccessStatusCode();

            var processorContent = await processorResponse.Content.ReadAsStringAsync();
            var processorResult = JsonSerializer.Deserialize<PlaidProcessorTokenResponse>(processorContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return new PlaidExchangeResult
            {
                Success = true,
                AccessToken = exchangeResult.AccessToken,
                ProcessorToken = processorResult?.ProcessorToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange Plaid token");
            return new PlaidExchangeResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

// Internal response models for Plaid API
internal record PlaidBalanceResponse
{
    public PlaidAccount[]? Accounts { get; init; }
}

internal record PlaidAccountsResponse
{
    public PlaidAccount[]? Accounts { get; init; }
}

internal record PlaidAccount
{
    public string AccountId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Subtype { get; init; } = "";
    public string? Mask { get; init; }
    public PlaidBalance Balances { get; init; } = new();
}

internal record PlaidBalance
{
    public decimal? Available { get; init; }
    public decimal? Current { get; init; }
    public decimal? Limit { get; init; }
}

internal record PlaidLinkTokenCreateResponse
{
    public string? LinkToken { get; init; }
}


internal record PlaidLinkTokenApiResponse
{
    public string LinkToken { get; set; } = null!;
    public DateTime Expiration { get; set; }
}

internal record PlaidAccessTokenResponse
{
    public string AccessToken { get; set; } = null!;
    public string ItemId { get; set; } = null!;
}

internal record PlaidProcessorTokenResponse
{
    public string ProcessorToken { get; set; } = null!;
}