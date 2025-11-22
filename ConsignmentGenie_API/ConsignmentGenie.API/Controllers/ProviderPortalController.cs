using ConsignmentGenie.Application.DTOs.Provider;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/provider-portal")]
public class ProviderPortalController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly ILogger<ProviderPortalController> _logger;

    public ProviderPortalController(
        IUnitOfWork unitOfWork,
        IAuthService authService,
        ILogger<ProviderPortalController> logger)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> SetupProviderAccount([FromBody] ProviderPortalSetupRequest request)
    {
        try
        {
            // Find provider by email and invite code
            var provider = await _unitOfWork.Providers
                .GetAsync(p => p.Email == request.Email && p.InviteCode == request.InviteCode);

            if (provider == null)
                return BadRequest("Invalid email or invite code");

            if (provider.UserId != null)
                return BadRequest("Provider account already set up");

            // Create user account for provider
            var user = new ConsignmentGenie.Core.Entities.User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Provider,
                OrganizationId = provider.OrganizationId,
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Link provider to user
            provider.UserId = user.Id;
            provider.PortalAccess = true;
            provider.InviteCode = null; // Clear invite code

            await _unitOfWork.Providers.UpdateAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            // Generate JWT token
            var token = _authService.GenerateJwtToken(user.Id, user.Email, user.Role.ToString(), provider.OrganizationId);

            return Ok(new
            {
                success = true,
                message = "Provider account setup successful",
                data = new
                {
                    token,
                    userId = user.Id,
                    email = user.Email,
                    role = (int)user.Role,
                    providerId = provider.Id,
                    providerName = provider.DisplayName,
                    organizationId = provider.OrganizationId,
                    organizationName = provider.Organization.Name
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up provider account");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginProvider([FromBody] ProviderPortalLoginRequest request)
    {
        try
        {
            // Find provider by email and invite code (for initial login)
            var provider = await _unitOfWork.Providers
                .GetAsync(p => p.Email == request.Email,
                    includeProperties: "Organization,User");

            if (provider?.User == null)
                return BadRequest("Invalid credentials");

            if (!BCrypt.Net.BCrypt.Verify(request.InviteCode, provider.User.PasswordHash))
                return BadRequest("Invalid credentials");

            // Generate JWT token
            var token = _authService.GenerateJwtToken(provider.User.Id, provider.User.Email, provider.User.Role.ToString(), provider.OrganizationId);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = new
                {
                    token,
                    userId = provider.User.Id,
                    email = provider.User.Email,
                    role = (int)provider.User.Role,
                    providerId = provider.Id,
                    providerName = provider.DisplayName,
                    organizationId = provider.OrganizationId,
                    organizationName = provider.Organization.Name
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during provider login");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> GetProviderDashboard()
    {
        try
        {
            var providerId = User.FindFirst("ProviderId")?.Value;
            if (string.IsNullOrEmpty(providerId))
                return BadRequest("Provider not found");

            var provider = await _unitOfWork.Providers
                .GetAsync(p => p.Id == Guid.Parse(providerId), includeProperties: "Items,Payouts");

            if (provider == null)
                return NotFound("Provider not found");

            // Calculate dashboard metrics
            var totalEarnings = provider.Payouts
                .Where(p => p.PaidAt.HasValue)
                .Sum(p => p.TotalAmount);

            var pendingPayouts = provider.Payouts
                .Where(p => !p.PaidAt.HasValue)
                .Sum(p => p.TotalAmount);

            var activeItems = provider.Items
                .Count(i => i.Status == ItemStatus.Available);

            var soldItems = provider.Items
                .Count(i => i.Status == ItemStatus.Sold);

            var lastPayoutDate = provider.Payouts
                .Where(p => p.PaidAt.HasValue)
                .OrderByDescending(p => p.PaidAt)
                .FirstOrDefault()?.PaidAt;

            var dashboard = new ProviderDashboardResponse
            {
                ProviderName = provider.DisplayName,
                TotalEarnings = totalEarnings,
                PendingPayouts = pendingPayouts,
                ActiveItems = activeItems,
                SoldItems = soldItems,
                LastPayoutDate = lastPayoutDate,
                CommissionRate = provider.DefaultSplitPercentage
            };

            return Ok(new { success = true, data = dashboard });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider dashboard");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("items")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> GetProviderItems()
    {
        try
        {
            var providerId = User.FindFirst("ProviderId")?.Value;
            if (string.IsNullOrEmpty(providerId))
                return BadRequest("Provider not found");

            var items = await _unitOfWork.Items
                .GetAllAsync(i => i.ProviderId == Guid.Parse(providerId), includeProperties: "Photos");

            var itemResponses = items.Select(item => new ProviderItemResponse
            {
                Id = item.Id,
                Name = item.Title,
                Description = item.Description,
                Price = item.Price,
                Status = item.Status.ToString(),
                DateAdded = item.CreatedAt,
                DateSold = item.Status == ItemStatus.Sold ? item.UpdatedAt : null,
                ProviderAmount = item.Status == ItemStatus.Sold
                    ? item.Price * (item.Provider.DefaultSplitPercentage / 100)
                    : null,
                PhotoUrls = new List<string>() // TODO: Parse Photos JSON field
            }).ToList();

            return Ok(new { success = true, data = itemResponses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider items");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("payouts")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> GetProviderPayouts()
    {
        try
        {
            var providerId = User.FindFirst("ProviderId")?.Value;
            if (string.IsNullOrEmpty(providerId))
                return BadRequest("Provider not found");

            var payouts = await _unitOfWork.Payouts
                .GetAllAsync(p => p.ProviderId == Guid.Parse(providerId),
                    includeProperties: "Provider");

            var payoutResponses = payouts.Select(payout => new ProviderPayoutResponse
            {
                Id = payout.Id,
                Amount = payout.TotalAmount,
                PeriodStart = payout.PeriodStart,
                PeriodEnd = payout.PeriodEnd,
                PayoutDate = payout.PaidAt,
                Status = payout.PaidAt.HasValue ? "Paid" : "Pending",
                ItemCount = 0, // Would need to query transactions for this
                Items = new() // Would need to populate from transaction items
            }).OrderByDescending(p => p.PeriodEnd).ToList();

            return Ok(new { success = true, data = payoutResponses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider payouts");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("send-invite")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> SendProviderInvite([FromBody] SendProviderInviteRequest request)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var provider = await _unitOfWork.Providers
                .GetByIdAsync(request.ProviderId);

            if (provider == null || provider.OrganizationId != Guid.Parse(organizationId))
                return NotFound("Provider not found");

            if (provider.PortalAccess)
                return BadRequest("Provider already has portal access");

            // Generate invite code
            provider.InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
            provider.InviteExpiry = DateTime.UtcNow.AddDays(7);

            await _unitOfWork.Providers.UpdateAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Send email with invite link
            // var inviteLink = $"{_configuration["Frontend:BaseUrl"]}/provider-portal/setup?code={provider.InviteCode}&email={provider.Email}";

            return Ok(new
            {
                success = true,
                message = "Invite sent successfully",
                data = new { inviteCode = provider.InviteCode }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending provider invite");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class SendProviderInviteRequest
{
    [Required]
    public Guid ProviderId { get; set; }
}