using ConsignmentGenie.Application.DTOs.Account;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IAccountService
{
    Task<AccountInfoDto> GetAccountInfoAsync(Guid organizationId);
}