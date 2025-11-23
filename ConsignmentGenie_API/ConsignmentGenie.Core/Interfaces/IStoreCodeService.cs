using ConsignmentGenie.Core.DTOs.Registration;

namespace ConsignmentGenie.Core.Interfaces;

public interface IStoreCodeService
{
    Task<StoreCodeDto> GetStoreCodeAsync(Guid organizationId);
    Task<StoreCodeDto> RegenerateStoreCodeAsync(Guid organizationId);
    Task ToggleStoreCodeAsync(Guid organizationId, bool enabled);
    string GenerateStoreCode();
}