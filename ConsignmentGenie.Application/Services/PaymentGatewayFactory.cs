using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentGatewayFactory> _logger;
    private readonly IConfiguration _configuration;

    public PaymentGatewayFactory(
        IServiceProvider serviceProvider,
        IUnitOfWork unitOfWork,
        ILogger<PaymentGatewayFactory> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
    }

    public IPaymentGatewayService CreateService(string provider, Dictionary<string, object> config)
    {
        return provider.ToLowerInvariant() switch
        {
            "stripe" => CreateStripeService(config),
            "square" => throw new NotImplementedException("Square payment gateway not yet implemented"),
            "quickbooks" => throw new NotImplementedException("QuickBooks payment gateway not yet implemented"),
            "clover" => throw new NotImplementedException("Clover payment gateway not yet implemented"),
            "shopify" => throw new NotImplementedException("Shopify payment gateway not yet implemented"),
            _ => throw new NotSupportedException($"Payment gateway provider '{provider}' is not supported")
        };
    }

    public IPaymentGatewayService GetDefaultService(Guid organizationId)
    {
        var defaultConnection = _unitOfWork.PaymentGatewayConnections
            .GetAllAsync(c => c.OrganizationId == organizationId && c.IsDefault && c.IsActive)
            .Result
            .FirstOrDefault();

        if (defaultConnection == null)
        {
            throw new InvalidOperationException($"No default payment gateway configured for organization {organizationId}");
        }

        var config = DecryptConfig(defaultConnection.EncryptedConfig);
        return CreateService(defaultConnection.Provider, config);
    }

    public IPaymentGatewayService GetServiceByConnection(Guid connectionId)
    {
        var connection = _unitOfWork.PaymentGatewayConnections
            .GetAllAsync(c => c.Id == connectionId && c.IsActive)
            .Result
            .FirstOrDefault();

        if (connection == null)
        {
            throw new InvalidOperationException($"Payment gateway connection {connectionId} not found or inactive");
        }

        var config = DecryptConfig(connection.EncryptedConfig);
        return CreateService(connection.Provider, config);
    }

    public List<string> GetSupportedProviders()
    {
        return new List<string>
        {
            "stripe",
            "square",
            "quickbooks",
            "clover",
            "shopify"
        };
    }

    public async Task<bool> TestConnectionAsync(string provider, Dictionary<string, object> config)
    {
        try
        {
            var service = CreateService(provider, config);
            // Test by checking if the service supports a basic functionality
            return await service.SupportsFunctionality("basic_payment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test connection for provider {Provider}", provider);
            return false;
        }
    }

    private IPaymentGatewayService CreateStripeService(Dictionary<string, object> config)
    {
        // Get the registered StripePaymentGatewayService and configure it with the provided config
        var stripeService = _serviceProvider.GetRequiredService<StripePaymentGatewayService>();

        // In a real implementation, you'd inject the config into the service
        // For now, we'll use a simple approach
        return stripeService;
    }

    private Dictionary<string, object> DecryptConfig(string encryptedConfig)
    {
        try
        {
            // In a real implementation, this should properly decrypt the config
            // For now, we'll assume it's just JSON encoded (not actually encrypted)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(encryptedConfig) ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt payment gateway configuration");
            throw new InvalidOperationException("Invalid payment gateway configuration");
        }
    }
}