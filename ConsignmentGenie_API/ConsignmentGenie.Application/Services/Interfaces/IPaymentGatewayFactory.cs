namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IPaymentGatewayFactory
{
    IPaymentGatewayService CreateService(string provider, Dictionary<string, object> config);
    IPaymentGatewayService GetDefaultService(Guid organizationId);
    IPaymentGatewayService GetServiceByConnection(Guid connectionId);
    List<string> GetSupportedProviders();
    Task<bool> TestConnectionAsync(string provider, Dictionary<string, object> config);
}