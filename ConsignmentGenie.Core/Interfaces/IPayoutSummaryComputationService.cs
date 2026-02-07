namespace ConsignmentGenie.Core.Services;

public interface IPayoutSummaryComputationService
{
    Task ComputeAllSummariesAsync(CancellationToken cancellationToken = default);
    Task ComputeOrganizationSummariesAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task ComputeConsignorSummaryAsync(Guid organizationId, Guid consignorId, CancellationToken cancellationToken = default);
}