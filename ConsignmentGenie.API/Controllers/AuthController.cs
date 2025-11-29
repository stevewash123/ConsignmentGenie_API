using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IUnitOfWork unitOfWork, ILogger<AuthController> logger)
    {
        _authService = authService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));
            }

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult($"Login failed: {ex.Message}"));
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);

            if (response == null)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Email already exists"));
            }

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Registration successful"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult($"Registration failed: {ex.Message}"));
        }
    }

    // Provider Registration endpoints
    [HttpGet("validate-store-code/{code}")]
    public async Task<ActionResult<ApiResponse<StoreCodeValidationResponseDto>>> ValidateStoreCode(string code)
    {
        try
        {
            var organization = await _unitOfWork.Organizations
                .GetAsync(o => o.StoreCode == code && o.StoreCodeEnabled);

            if (organization == null)
            {
                return BadRequest(ApiResponse<StoreCodeValidationResponseDto>.ErrorResult("Invalid store code"));
            }

            var response = new StoreCodeValidationResponseDto
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name
            };
            return Ok(ApiResponse<StoreCodeValidationResponseDto>.SuccessResult(response, "Store code is valid"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating store code");
            return StatusCode(500, ApiResponse<StoreCodeValidationResponseDto>.ErrorResult("Internal server error"));
        }
    }

    [HttpPost("register/provider")]
    public async Task<ActionResult<ApiResponse<ProviderRegistrationResponseDto>>> RegisterProvider([FromBody] RegisterProviderRequest request)
    {
        try
        {
            // Validate store code first
            var organization = await _unitOfWork.Organizations
                .GetAsync(o => o.StoreCode == request.StoreCode && o.StoreCodeEnabled);

            if (organization == null)
            {
                return BadRequest(ApiResponse<ProviderRegistrationResponseDto>.ErrorResult("Invalid store code"));
            }

            // Check if provider already exists
            var existingProvider = await _unitOfWork.Providers
                .GetAsync(p => p.Email == request.Email && p.OrganizationId == organization.Id);

            if (existingProvider != null)
            {
                return BadRequest(ApiResponse<ProviderRegistrationResponseDto>.ErrorResult("Provider with this email already exists"));
            }

            // Create provider registration request (pending approval)
            var provider = new ConsignmentGenie.Core.Entities.Provider
            {
                OrganizationId = organization.Id,
                Email = request.Email,
                DisplayName = request.FullName,
                Phone = request.Phone,
                CommissionRate = 50.00m, // Default rate, owner can change
                Status = ConsignmentGenie.Core.Enums.ProviderStatus.Pending,
                PortalAccess = false
            };

            await _unitOfWork.Providers.AddAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Send email notification to shop owner about pending approval
            // TODO: Send confirmation email to provider

            var response = new ProviderRegistrationResponseDto
            {
                ProviderId = provider.Id,
                Message = "Registration submitted successfully. You will receive an email when approved."
            };
            return Ok(ApiResponse<ProviderRegistrationResponseDto>.SuccessResult(response, "Provider registration submitted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering provider");
            return StatusCode(500, ApiResponse<ProviderRegistrationResponseDto>.ErrorResult("Registration failed"));
        }
    }

    [HttpPost("provider/setup")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> SetupProviderAccount([FromBody] ProviderPortalSetupRequest request)
    {
        try
        {
            // Find provider by email and invite code
            var provider = await _unitOfWork.Providers
                .GetAsync(p => p.Email == request.Email && p.InviteCode == request.InviteCode,
                    includeProperties: "Organization");

            if (provider == null)
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or invite code"));

            if (provider.UserId != null)
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Provider account already set up"));

            if (provider.InviteExpiry.HasValue && provider.InviteExpiry < DateTime.UtcNow)
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invite code has expired"));

            // Create user account for provider
            var user = new ConsignmentGenie.Core.Entities.User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = ConsignmentGenie.Core.Enums.UserRole.Provider,
                OrganizationId = provider.OrganizationId,
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Link provider to user
            provider.UserId = user.Id;
            provider.PortalAccess = true;
            provider.InviteCode = null; // Clear invite code
            provider.InviteExpiry = null;
            provider.Status = ConsignmentGenie.Core.Enums.ProviderStatus.Active;

            await _unitOfWork.Providers.UpdateAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            // Generate JWT token
            var token = _authService.GenerateJwtToken(user.Id, user.Email, user.Role.ToString(), provider.OrganizationId);

            var response = new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                OrganizationId = provider.OrganizationId,
                OrganizationName = provider.Organization.Name
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Provider account setup successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up provider account");
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("Account setup failed"));
        }
    }

    [HttpPost("provider/login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> LoginProvider([FromBody] ProviderLoginRequest request)
    {
        try
        {
            // Find provider by email
            var provider = await _unitOfWork.Providers
                .GetAsync(p => p.Email == request.Email,
                    includeProperties: "Organization,User");

            if (provider?.User == null)
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));

            if (!BCrypt.Net.BCrypt.Verify(request.Password, provider.User.PasswordHash))
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));

            if (!provider.PortalAccess)
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Portal access not granted"));

            // Generate JWT token
            var token = _authService.GenerateJwtToken(provider.User.Id, provider.User.Email, provider.User.Role.ToString(), provider.OrganizationId);

            var response = new LoginResponse
            {
                Token = token,
                UserId = provider.User.Id,
                Email = provider.User.Email,
                Role = provider.User.Role,
                OrganizationId = provider.OrganizationId,
                OrganizationName = provider.Organization.Name
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during provider login");
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("Login failed"));
        }
    }

    [HttpGet("validate-subdomain/{subdomain}")]
    public async Task<ActionResult<ApiResponse<SubdomainValidationResponse>>> ValidateSubdomain(string subdomain)
    {
        try
        {
            // Check if subdomain is already taken
            var existingOrg = await _unitOfWork.Organizations
                .GetAsync(o => o.Subdomain == subdomain.ToLower());

            var response = new SubdomainValidationResponse
            {
                IsAvailable = existingOrg == null,
                Subdomain = subdomain.ToLower()
            };

            return Ok(ApiResponse<SubdomainValidationResponse>.SuccessResult(response,
                response.IsAvailable ? "Subdomain is available" : "Subdomain is already taken"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subdomain: {Subdomain}", subdomain);
            return StatusCode(500, ApiResponse<SubdomainValidationResponse>.ErrorResult("Validation failed"));
        }
    }
}

// Request DTOs
public class ProviderPortalSetupRequest
{
    public string Email { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ProviderLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SubdomainValidationResponse
{
    public bool IsAvailable { get; set; }
    public string Subdomain { get; set; } = string.Empty;
}