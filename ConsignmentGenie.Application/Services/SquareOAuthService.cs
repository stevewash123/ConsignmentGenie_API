using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public interface ISquareOAuthService
{
    Task<string> GenerateAuthUrlAsync(Guid organizationId);
    Task<SquareConnectionResult> ProcessCallbackAsync(Guid organizationId, string code, string state);
    Task<SquareConnectionStatus> GetConnectionStatusAsync(Guid organizationId);
    Task<bool> DisconnectAsync(Guid organizationId);
    Task<bool> RefreshTokenAsync(Guid organizationId);
}

public class SquareOAuthService : ISquareOAuthService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SquareOAuthService> _logger;
    private readonly HttpClient _httpClient;

    public SquareOAuthService(
        ConsignmentGenieContext context,
        IConfiguration configuration,
        ILogger<SquareOAuthService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> GenerateAuthUrlAsync(Guid organizationId)
    {
        var applicationId = _configuration["Square:ApplicationId"];
        var scopes = _configuration["Square:Scopes"] ?? "ITEMS_READ ITEMS_WRITE ORDERS_READ PAYMENTS_READ MERCHANT_PROFILE_READ";
        var redirectUri = _configuration["Square:RedirectUri"];

        _logger.LogInformation("Square configuration check - ApplicationId: {HasAppId}, RedirectUri: {HasRedirectUri}",
            !string.IsNullOrEmpty(applicationId), !string.IsNullOrEmpty(redirectUri));

        if (string.IsNullOrEmpty(applicationId))
        {
            _logger.LogError("Square ApplicationId is not configured");
            throw new InvalidOperationException("Square ApplicationId is not configured");
        }

        if (string.IsNullOrEmpty(redirectUri))
        {
            _logger.LogError("Square RedirectUri is not configured");
            throw new InvalidOperationException("Square RedirectUri is not configured");
        }

        var state = GenerateStateToken(organizationId);

        var authUrl = $"https://squareupsandbox.com/oauth2/authorize" +
                     $"?client_id={applicationId}" +
                     $"&scope={Uri.EscapeDataString(scopes)}" +
                     $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                     $"&state={state}";

        _logger.LogInformation("Generated Square OAuth URL for organization {OrganizationId}", organizationId);
        return authUrl;
    }

    public async Task<SquareConnectionResult> ProcessCallbackAsync(Guid organizationId, string code, string state)
    {
        try
        {
            // Validate state token
            if (!ValidateStateToken(state, organizationId))
            {
                _logger.LogWarning("Invalid state token for organization {OrganizationId}", organizationId);
                return new SquareConnectionResult { Success = false, ErrorCode = "invalid_state" };
            }

            // Exchange code for tokens
            var tokenResponse = await ExchangeCodeForTokensAsync(code);
            if (!tokenResponse.Success)
            {
                _logger.LogError("Token exchange failed for organization {OrganizationId}", organizationId);
                return new SquareConnectionResult { Success = false, ErrorCode = "token_exchange_failed" };
            }

            // Fetch merchant info (if we have the scope)
            var merchantInfo = await FetchMerchantInfoAsync(tokenResponse.AccessToken);
            if (merchantInfo == null)
            {
                _logger.LogWarning("Could not fetch merchant info for organization {OrganizationId}, using fallback", organizationId);
                // Use fallback merchant info if we can't fetch it (missing scope)
                merchantInfo = new MerchantInfo
                {
                    MerchantId = "unknown",
                    BusinessName = "Square Connected Business"
                };
            }

            // Update or create Square connection
            var connection = await _context.SquareConnections
                .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId);

            if (connection == null)
            {
                connection = new SquareConnection
                {
                    OrganizationId = organizationId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.SquareConnections.Add(connection);
            }

            // Update connection details
            connection.IsConnected = true;
            connection.MerchantId = merchantInfo.MerchantId;
            connection.LocationId = merchantInfo.LocationId;
            connection.AccessToken = tokenResponse.AccessToken; // TODO: Encrypt
            connection.RefreshToken = tokenResponse.RefreshToken; // TODO: Encrypt
            connection.TokenExpiry = DateTime.UtcNow.AddDays(30); // Square tokens expire in 30 days
            connection.LastSyncAt = DateTime.UtcNow;
            connection.UpdatedAt = DateTime.UtcNow;

            // Update organization integration mode
            var organization = await _context.Organizations.FindAsync(organizationId);
            if (organization != null)
            {
                organization.IntegrationMode = IntegrationMode.Square;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Square connection completed for organization {OrganizationId}, merchant {MerchantId}, location {LocationId}",
                organizationId, merchantInfo.MerchantId, merchantInfo.LocationId);

            return new SquareConnectionResult
            {
                Success = true,
                MerchantId = merchantInfo.MerchantId,
                MerchantName = merchantInfo.BusinessName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Square OAuth callback for organization {OrganizationId}", organizationId);
            return new SquareConnectionResult { Success = false, ErrorCode = "internal_error" };
        }
    }

    public async Task<SquareConnectionStatus> GetConnectionStatusAsync(Guid organizationId)
    {
        var connection = await _context.SquareConnections
            .Where(sc => sc.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        var organization = await _context.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => o.IntegrationMode)
            .FirstOrDefaultAsync();

        if (connection == null || !connection.IsConnected || string.IsNullOrEmpty(connection.MerchantId))
        {
            return new SquareConnectionStatus
            {
                Connected = false,
                IntegrationMode = organization.ToString()
            };
        }

        // In production, we might want to fetch fresh merchant name or cache it
        var merchantName = $"Merchant {connection.MerchantId}";

        return new SquareConnectionStatus
        {
            Connected = true,
            MerchantId = connection.MerchantId,
            MerchantName = merchantName,
            LastSyncAt = connection.LastSyncAt,
            IntegrationMode = organization.ToString()
        };
    }

    public async Task<bool> DisconnectAsync(Guid organizationId)
    {
        try
        {
            var connection = await _context.SquareConnections
                .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId);

            var organization = await _context.Organizations.FindAsync(organizationId);

            if (connection != null)
            {
                // Completely remove the connection record to ensure fresh OAuth on reconnect
                _context.SquareConnections.Remove(connection);
                _logger.LogInformation("Removed Square connection record for organization {OrganizationId}", organizationId);
            }

            if (organization != null)
            {
                organization.IntegrationMode = IntegrationMode.CgNative;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Square disconnected for organization {OrganizationId}", organizationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting Square for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync(Guid organizationId)
    {
        try
        {
            var connection = await _context.SquareConnections
                .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId);

            if (connection == null || string.IsNullOrEmpty(connection.RefreshToken))
            {
                return false;
            }

            var refreshResponse = await RefreshSquareTokenAsync(connection.RefreshToken);
            if (!refreshResponse.Success)
            {
                _logger.LogError("Token refresh failed for organization {OrganizationId}", organizationId);
                return false;
            }

            connection.AccessToken = refreshResponse.AccessToken;
            connection.RefreshToken = refreshResponse.RefreshToken;
            connection.TokenExpiry = DateTime.UtcNow.AddDays(30);
            connection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Square token refreshed for organization {OrganizationId}", organizationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Square token for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    #region Private Helper Methods

    private string GenerateStateToken(Guid organizationId)
    {
        // In production, this should be more secure (cryptographically signed)
        var data = $"{organizationId}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
    }

    private bool ValidateStateToken(string state, Guid organizationId)
    {
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var parts = decoded.Split(':');
            if (parts.Length != 2) return false;

            var tokenOrgId = Guid.Parse(parts[0]);
            var tokenTime = DateTime.ParseExact(parts[1], "yyyyMMddHHmmss", null);

            // Token is valid if it's for this org and within 1 hour
            return tokenOrgId == organizationId &&
                   DateTime.UtcNow.Subtract(tokenTime).TotalHours < 1;
        }
        catch
        {
            return false;
        }
    }

    private async Task<TokenExchangeResult> ExchangeCodeForTokensAsync(string code)
    {
        try
        {
            var applicationId = _configuration["Square:ApplicationId"];
            var applicationSecret = _configuration["Square:ApplicationSecret"];
            var redirectUri = _configuration["Square:RedirectUri"];

            var requestData = new
            {
                client_id = applicationId,
                client_secret = applicationSecret,
                code = code,
                grant_type = "authorization_code",
                redirect_uri = redirectUri
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://connect.squareupsandbox.com/oauth2/token", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Square token exchange failed: {Response}", responseJson);
                return new TokenExchangeResult { Success = false };
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return new TokenExchangeResult
            {
                Success = true,
                AccessToken = tokenData.GetProperty("access_token").GetString()!,
                RefreshToken = tokenData.GetProperty("refresh_token").GetString()!
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Square token exchange");
            return new TokenExchangeResult { Success = false };
        }
    }

    private async Task<MerchantInfo?> FetchMerchantInfoAsync(string accessToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://connect.squareupsandbox.com/v2/merchants");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Square merchant API response - Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseJson);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch Square merchant info - Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseJson);
                return null;
            }

            var merchantData = JsonSerializer.Deserialize<JsonElement>(responseJson);

            // Square API returns "merchant" (singular), not "merchants" (plural)
            if (!merchantData.TryGetProperty("merchant", out var merchantsProperty))
            {
                _logger.LogError("No 'merchant' property in Square response: {Response}", responseJson);
                return null;
            }

            if (merchantsProperty.GetArrayLength() == 0)
            {
                _logger.LogError("Empty merchant array in Square response: {Response}", responseJson);
                return null;
            }

            var merchant = merchantsProperty[0];
            var merchantId = merchant.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var businessName = merchant.TryGetProperty("business_name", out var nameProp) ? nameProp.GetString() : "Unknown Business";
            var locationId = merchant.TryGetProperty("main_location_id", out var locationProp) ? locationProp.GetString() : null;

            if (string.IsNullOrEmpty(merchantId))
            {
                _logger.LogError("No merchant ID found in Square response: {Response}", responseJson);
                return null;
            }

            return new MerchantInfo
            {
                MerchantId = merchantId,
                BusinessName = businessName ?? "Unknown Business",
                LocationId = locationId ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Square merchant info");
            return null;
        }
    }

    private async Task<TokenExchangeResult> RefreshSquareTokenAsync(string refreshToken)
    {
        try
        {
            var applicationId = _configuration["Square:ApplicationId"];
            var applicationSecret = _configuration["Square:ApplicationSecret"];

            var requestData = new
            {
                client_id = applicationId,
                client_secret = applicationSecret,
                refresh_token = refreshToken,
                grant_type = "refresh_token"
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://connect.squareupsandbox.com/oauth2/token", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Square token refresh failed: {Response}", responseJson);
                return new TokenExchangeResult { Success = false };
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return new TokenExchangeResult
            {
                Success = true,
                AccessToken = tokenData.GetProperty("access_token").GetString()!,
                RefreshToken = tokenData.GetProperty("refresh_token").GetString()!
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Square token refresh");
            return new TokenExchangeResult { Success = false };
        }
    }

    #endregion
}

#region DTOs and Result Classes

public class SquareConnectionResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? MerchantId { get; set; }
    public string? MerchantName { get; set; }
}

public class SquareConnectionStatus
{
    public bool Connected { get; set; }
    public string? MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? IntegrationMode { get; set; }
}

public class TokenExchangeResult
{
    public bool Success { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class MerchantInfo
{
    public string MerchantId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
}

#endregion