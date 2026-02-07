using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ConsignmentGenie.Core.Services;

namespace ConsignmentGenie.Application.Services.Financial;

public class DwollaService : IDwollaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DwollaService> _logger;

    private string AppKey => _configuration["Dwolla:Key"]
        ?? throw new InvalidOperationException("Dwolla:Key not configured");
    private string AppSecret => _configuration["Dwolla:Secret"]
        ?? throw new InvalidOperationException("Dwolla:Secret not configured");
    private string Environment => _configuration["Dwolla:Environment"] ?? "sandbox";

    private string BaseUrl => Environment == "production"
        ? "https://api.dwolla.com"
        : "https://api-sandbox.dwolla.com";

    private string AuthUrl => Environment == "production"
        ? "https://api.dwolla.com/token"
        : "https://api-sandbox.dwolla.com/token";

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public DwollaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DwollaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DwollaCustomerResult> CreateOrGetCustomerAsync(
        Guid organizationId,
        string businessName,
        string email)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            // For sandbox, create an "unverified" business customer with minimal required fields
            var request = new
            {
                firstName = businessName.Split(' ').FirstOrDefault() ?? "Business",
                lastName = businessName.Split(' ').LastOrDefault() ?? "Owner",
                email = email,
                type = "personal",
                ipAddress = "127.0.0.1",
                // Add minimal required fields for Dwolla
                dateOfBirth = "1990-01-01",  // Placeholder for sandbox
                address1 = "123 Business St",  // Placeholder for sandbox
                city = "San Francisco",        // Placeholder for sandbox
                state = "CA",                  // Placeholder for sandbox
                postalCode = "94102",          // Placeholder for sandbox
                ssn = "1234"                   // Last 4 digits placeholder for sandbox
            };

            var response = await PostAsync("/customers", request);

            // Dwolla returns location header with customer URL
            var customerUrl = response.Headers.Location?.ToString();

            if (string.IsNullOrEmpty(customerUrl))
            {
                return new DwollaCustomerResult
                {
                    Success = false,
                    ErrorMessage = "No customer URL returned from Dwolla"
                };
            }

            // Extract customer ID from URL
            var customerId = customerUrl.Split('/').Last();

            return new DwollaCustomerResult
            {
                Success = true,
                CustomerId = customerId,
                CustomerUrl = customerUrl,
                Status = "unverified"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Dwolla customer for organization {OrganizationId}", organizationId);
            return new DwollaCustomerResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<DwollaFundingSourceResult> CreateFundingSourceAsync(
        string dwollaCustomerUrl,
        string plaidProcessorToken,
        string accountName)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var request = new
            {
                plaidToken = plaidProcessorToken,
                name = accountName
            };

            var endpoint = $"{dwollaCustomerUrl}/funding-sources";
            var response = await PostAsync(endpoint, request);

            var fundingSourceUrl = response.Headers.Location?.ToString();

            if (string.IsNullOrEmpty(fundingSourceUrl))
            {
                return new DwollaFundingSourceResult
                {
                    Success = false,
                    ErrorMessage = "No funding source URL returned from Dwolla"
                };
            }

            var fundingSourceId = fundingSourceUrl.Split('/').Last();

            return new DwollaFundingSourceResult
            {
                Success = true,
                FundingSourceId = fundingSourceId,
                FundingSourceUrl = fundingSourceUrl,
                Status = "verified"  // Plaid-linked accounts are auto-verified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Dwolla funding source for customer {CustomerUrl}", dwollaCustomerUrl);
            return new DwollaFundingSourceResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> RemoveFundingSourceAsync(string fundingSourceUrl)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var request = new { removed = true };
            await PostAsync(fundingSourceUrl, request, isPatch: true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Dwolla funding source {FundingSourceUrl}", fundingSourceUrl);
            return false;
        }
    }

    public async Task<string> GetFundingSourceStatusAsync(string fundingSourceUrl)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.dwolla.v1.hal+json"));

            var response = await _httpClient.GetAsync(fundingSourceUrl);
            var content = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(content);
            return json.RootElement.GetProperty("status").GetString() ?? "unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Dwolla funding source status for {FundingSourceUrl}", fundingSourceUrl);
            return "error";
        }
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            return;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AppKey}:{AppSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Dwolla auth failed: {content}");
        }

        var json = JsonDocument.Parse(content);
        _accessToken = json.RootElement.GetProperty("access_token").GetString();
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Buffer
    }

    private async Task<HttpResponseMessage> PostAsync(string url, object request, bool isPatch = false)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.dwolla.v1.hal+json"));

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, "application/vnd.dwolla.v1.hal+json");

        var fullUrl = url.StartsWith("http") ? url : $"{BaseUrl}{url}";

        HttpResponseMessage response;
        if (isPatch)
        {
            var patchRequest = new HttpRequestMessage(HttpMethod.Patch, fullUrl) { Content = content };
            response = await _httpClient.SendAsync(patchRequest);
        }
        else
        {
            response = await _httpClient.PostAsync(fullUrl, content);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Dwolla API error: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            throw new Exception($"Dwolla API error: {response.StatusCode}");
        }

        return response;
    }
}