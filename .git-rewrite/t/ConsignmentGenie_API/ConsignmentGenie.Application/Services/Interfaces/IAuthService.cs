using ConsignmentGenie.Application.DTOs.Auth;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RegisterAsync(RegisterRequest request);
    string GenerateJwtToken(Guid userId, string email, string role, Guid organizationId);
}