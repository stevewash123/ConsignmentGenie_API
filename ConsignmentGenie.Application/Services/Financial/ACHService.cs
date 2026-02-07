using ConsignmentGenie.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services.Financial;

public class ACHService : IACHService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ACHService> _logger;
    private readonly string _apiKey;
    private readonly string _environment;

    public ACHService(
        HttpClient httpClient,
        ILogger<ACHService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["ACH:ApiKey"] ?? throw new InvalidOperationException("ACH API Key not configured");
        _environment = configuration["ACH:Environment"] ?? "sandbox";

        // Set base URL based on environment - using placeholder for now
        // In real implementation, this would be your ACH provider (Stripe, Dwolla, etc.)
        _httpClient.BaseAddress = _environment.ToLower() switch
        {
            "production" => new Uri("https://api.achprovider.com/"),
            _ => new Uri("https://sandbox-api.achprovider.com/")
        };

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<ACHTransferResult> InitiateTransfer(ACHTransferRequest request)
    {
        _logger.LogInformation("Initiating ACH transfer of {Amount} for payout {PayoutId}",
            request.Amount, request.PayoutId);

        try
        {
            // Validate request
            if (request.Amount <= 0)
            {
                return new ACHTransferResult
                {
                    Success = false,
                    ErrorMessage = "Transfer amount must be greater than zero"
                };
            }

            if (string.IsNullOrEmpty(request.ToAccountId))
            {
                return new ACHTransferResult
                {
                    Success = false,
                    ErrorMessage = "Recipient account ID is required"
                };
            }

            // Build ACH transfer payload
            var transferPayload = new
            {
                amount = request.Amount,
                currency = "USD",
                source_account = request.FromAccountToken,
                destination_account = request.ToAccountId,
                description = request.Description,
                reference = request.Reference,
                metadata = new
                {
                    payout_id = request.PayoutId.ToString(),
                    created_by = "auto_payout_job"
                }
            };

            // For development/testing - simulate ACH provider response
            if (_environment.ToLower() == "sandbox" || _environment.ToLower() == "development")
            {
                return await SimulateACHTransfer(request);
            }

            // Real ACH provider integration
            var response = await _httpClient.PostAsJsonAsync("/transfers", transferPayload);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var transferResponse = JsonSerializer.Deserialize<ACHProviderResponse>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var result = new ACHTransferResult
                {
                    Success = true,
                    TransferId = transferResponse?.Id,
                    InitiatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("ACH transfer initiated successfully. TransferId: {TransferId}", result.TransferId);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ACH transfer failed with status {StatusCode}: {Error}",
                    response.StatusCode, errorContent);

                return new ACHTransferResult
                {
                    Success = false,
                    ErrorMessage = $"ACH provider error: {response.StatusCode}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during ACH transfer for payout {PayoutId}", request.PayoutId);
            return new ACHTransferResult
            {
                Success = false,
                ErrorMessage = "Network error during transfer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during ACH transfer for payout {PayoutId}", request.PayoutId);
            return new ACHTransferResult
            {
                Success = false,
                ErrorMessage = "Unexpected error during transfer"
            };
        }
    }

    public async Task<ACHTransferStatus> GetTransferStatus(string transferId)
    {
        _logger.LogInformation("Getting ACH transfer status for {TransferId}", transferId);

        try
        {
            // For development/testing - simulate status check
            if (_environment.ToLower() == "sandbox" || _environment.ToLower() == "development")
            {
                return await SimulateTransferStatus(transferId);
            }

            var response = await _httpClient.GetAsync($"/transfers/{transferId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var statusResponse = JsonSerializer.Deserialize<ACHProviderStatusResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var result = new ACHTransferStatus
            {
                TransferId = transferId,
                Status = statusResponse?.Status ?? "unknown",
                ErrorMessage = statusResponse?.ErrorMessage,
                StatusUpdatedAt = statusResponse?.UpdatedAt ?? DateTime.UtcNow
            };

            _logger.LogInformation("Retrieved transfer status {Status} for {TransferId}", result.Status, transferId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfer status for {TransferId}", transferId);
            return new ACHTransferStatus
            {
                TransferId = transferId,
                Status = "unknown",
                ErrorMessage = ex.Message,
                StatusUpdatedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ACHTransferResult> SimulateACHTransfer(ACHTransferRequest request)
    {
        // Simulate processing time
        await Task.Delay(100);

        // Simulate 95% success rate
        var random = new Random();
        var success = random.NextDouble() > 0.05;

        if (success)
        {
            var transferId = $"sim_{Guid.NewGuid():N}";
            _logger.LogInformation("Simulated ACH transfer successful. TransferId: {TransferId}", transferId);

            return new ACHTransferResult
            {
                Success = true,
                TransferId = transferId,
                InitiatedAt = DateTime.UtcNow
            };
        }
        else
        {
            _logger.LogWarning("Simulated ACH transfer failed for payout {PayoutId}", request.PayoutId);

            return new ACHTransferResult
            {
                Success = false,
                ErrorMessage = "Simulated transfer failure - insufficient funds",
                InitiatedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ACHTransferStatus> SimulateTransferStatus(string transferId)
    {
        await Task.Delay(50);

        // Simulate different statuses based on transfer ID
        var status = transferId.Contains("fail") ? "failed" : "completed";

        return new ACHTransferStatus
        {
            TransferId = transferId,
            Status = status,
            ErrorMessage = status == "failed" ? "Simulated failure" : null,
            StatusUpdatedAt = DateTime.UtcNow
        };
    }
}

// Internal response models for ACH provider API
internal record ACHProviderResponse
{
    public string? Id { get; init; }
    public string Status { get; init; } = "";
    public string? ErrorMessage { get; init; }
}

internal record ACHProviderStatusResponse
{
    public string Status { get; init; } = "";
    public string? ErrorMessage { get; init; }
    public DateTime UpdatedAt { get; init; }
}