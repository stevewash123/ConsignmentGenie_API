using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ConsignmentGenie.Application.Services;

public class SquareService : ISquareService
{
    private readonly IConfiguration _configuration;

    public SquareService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> GetItemQuantityAsync(string accessToken, string variationId, CancellationToken ct = default)
    {
        // Mock implementation for now - would need Square SDK
        await Task.Delay(100, ct); // Simulate API call
        return 1; // Mock quantity
    }

    public async Task UpdateItemQuantityAsync(string accessToken, string variationId, int quantity, CancellationToken ct = default)
    {
        // Mock implementation for now - would need Square SDK
        await Task.Delay(200, ct); // Simulate API call

        // Simulate potential API errors
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new SquareApiException("Invalid access token");
        }
    }
}