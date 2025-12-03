using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Items;
using ProviderDTOs = ConsignmentGenie.Application.DTOs.Consignor;
using ConsignmentGenie.Application.Services.Interfaces;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class ProvidersController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ProvidersController> _logger;
    private readonly IProviderInvitationService _invitationService;

    public ProvidersController(ConsignmentGenieContext context, ILogger<ProvidersController> logger, IProviderInvitationService invitationService)
    {
        _context = context;
        _logger = logger;
        _invitationService = invitationService;
    }

    // LIST - Get providers with filtering/pagination
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProviderListDto>>> GetProviders([FromQuery] ProviderQueryParams queryParams)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var query = _context.Consignors
                .Include(p => p.Items)
                .Include(p => p.Transactions)
                .Where(p => p.OrganizationId == organizationId);

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(p =>
                    (p.FirstName + " " + p.LastName).Contains(queryParams.Search) ||
                    p.ConsignorNumber.Contains(queryParams.Search) ||
                    (p.Email != null && p.Email.Contains(queryParams.Search)));
            }

            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                if (Enum.TryParse<ConsignorStatus>(queryParams.Status, out var status))
                {
                    query = query.Where(p => p.Status == status);
                }
            }

            // Get data and calculate metrics
            var providers = await query.ToListAsync();
            var providersWithMetrics = new List<dynamic>();

            foreach (var provider in providers)
            {
                var pendingBalance = await CalculatePendingBalance(provider.Id, organizationId);
                var totalEarnings = provider.Transactions.Sum(t => t.ConsignorAmount);

                providersWithMetrics.Add(new
                {
                    Consignor = provider,
                    PendingBalance = pendingBalance,
                    ActiveItemCount = provider.Items.Count(i => i.Status == ItemStatus.Available),
                    TotalItemCount = provider.Items.Count(),
                    TotalEarnings = totalEarnings
                });
            }

            // Apply pending balance filter
            if (queryParams.HasPendingBalance.HasValue)
            {
                providersWithMetrics = providersWithMetrics
                    .Where(p => queryParams.HasPendingBalance.Value
                        ? ((decimal)p.PendingBalance) > 0
                        : ((decimal)p.PendingBalance) == 0)
                    .ToList();
            }

            // Apply sorting
            providersWithMetrics = ApplySorting(providersWithMetrics, queryParams);

            // Apply pagination
            var totalCount = providersWithMetrics.Count;
            var pagedProviders = providersWithMetrics
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            var providerDtos = pagedProviders.Select(p => new ProviderListDto
            {
                ConsignorId = ((Consignor)p.Consignor).Id,
                ConsignorNumber = ((Consignor)p.Consignor).ConsignorNumber,
                FullName = $"{((Consignor)p.Consignor).FirstName} {((Consignor)p.Consignor).LastName}",
                Email = ((Consignor)p.Consignor).Email,
                Phone = ((Consignor)p.Consignor).Phone,
                CommissionRate = ((Consignor)p.Consignor).CommissionRate,
                Status = ((Consignor)p.Consignor).Status.ToString(),
                ActiveItemCount = (int)p.ActiveItemCount,
                TotalItemCount = (int)p.TotalItemCount,
                PendingBalance = (decimal)p.PendingBalance,
                TotalEarnings = (decimal)p.TotalEarnings,
                HasPortalAccess = ((Consignor)p.Consignor).UserId != null,
                CreatedAt = ((Consignor)p.Consignor).CreatedAt
            }).ToList();

            var result = new PagedResult<ProviderListDto>
            {
                Items = providerDtos,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                OrganizationId = organizationId
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting providers for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, "Failed to retrieve providers");
        }
    }

    // GET ONE - Get provider by ID with full details
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> GetProvider(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var provider = await _context.Consignors
                .Include(p => p.ApprovedByUser)
                .Include(p => p.User)
                .Where(p => p.Id == id && p.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Consignor not found"));
            }

            var metrics = await CalculateProviderMetrics(provider.Id, organizationId);

            var providerDto = new ProviderDetailDto
            {
                ConsignorId = provider.Id,
                UserId = provider.UserId,
                ConsignorNumber = provider.ConsignorNumber,
                FirstName = provider.FirstName,
                LastName = provider.LastName,
                FullName = $"{provider.FirstName} {provider.LastName}",
                Email = provider.Email,
                Phone = provider.Phone,
                AddressLine1 = provider.AddressLine1,
                AddressLine2 = provider.AddressLine2,
                City = provider.City,
                State = provider.State,
                PostalCode = provider.PostalCode,
                FullAddress = FormatAddress(provider),
                CommissionRate = provider.CommissionRate,
                ContractStartDate = provider.ContractStartDate,
                ContractEndDate = provider.ContractEndDate,
                IsContractExpired = provider.ContractEndDate.HasValue && provider.ContractEndDate.Value < DateTime.UtcNow,
                PreferredPaymentMethod = provider.PreferredPaymentMethod,
                PaymentDetails = provider.PaymentDetails,
                Status = provider.Status.ToString(),
                StatusChangedAt = provider.StatusChangedAt,
                StatusChangedReason = provider.StatusChangedReason,
                ApprovalStatus = provider.ApprovalStatus,
                ApprovedAt = provider.ApprovedAt,
                ApprovedByName = provider.ApprovedByUser?.Email,
                RejectedReason = provider.RejectedReason,
                Notes = provider.Notes,
                HasPortalAccess = provider.UserId != null,
                Metrics = metrics,
                CreatedAt = provider.CreatedAt,
                UpdatedAt = provider.UpdatedAt ?? provider.CreatedAt
            };

            return Ok(ApiResponse<ProviderDetailDto>.SuccessResult(providerDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to retrieve provider"));
        }
    }

    // CREATE - Add new provider manually
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> CreateProvider([FromBody] CreateProviderRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            // Check if email already exists
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingProvider = await _context.Consignors
                    .AnyAsync(p => p.OrganizationId == organizationId &&
                                  p.Email == request.Email &&
                                  p.Status != ConsignorStatus.Rejected);

                if (existingProvider)
                {
                    return BadRequest(ApiResponse<ProviderDetailDto>.ErrorResult("A provider with this email already exists"));
                }
            }

            // Validate contract dates
            if (request.ContractStartDate.HasValue && request.ContractEndDate.HasValue &&
                request.ContractEndDate.Value <= request.ContractStartDate.Value)
            {
                return BadRequest(ApiResponse<ProviderDetailDto>.ErrorResult("Contract end date must be after start date"));
            }

            var providerNumber = await GenerateProviderNumber(organizationId);

            var provider = new Consignor
            {
                OrganizationId = organizationId,
                ConsignorNumber = providerNumber,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email?.Trim(),
                Phone = request.Phone?.Trim(),
                AddressLine1 = request.AddressLine1?.Trim(),
                AddressLine2 = request.AddressLine2?.Trim(),
                City = request.City?.Trim(),
                State = request.State?.Trim(),
                PostalCode = request.PostalCode?.Trim(),
                CommissionRate = request.CommissionRate,
                ContractStartDate = request.ContractStartDate,
                ContractEndDate = request.ContractEndDate,
                PreferredPaymentMethod = request.PreferredPaymentMethod?.Trim(),
                PaymentDetails = request.PaymentDetails?.Trim(),
                Notes = request.Notes?.Trim(),
                Status = ConsignorStatus.Active,
                CreatedBy = userId
            };

            _context.Consignors.Add(provider);
            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor created successfully";
                return CreatedAtAction(nameof(GetProvider), new { id = provider.Id }, apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Consignor created but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider");
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to create provider"));
        }
    }

    // UPDATE - Edit provider
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> UpdateProvider(Guid id, [FromBody] UpdateProviderRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var provider = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Consignor not found"));
            }

            // Check email uniqueness
            if (!string.IsNullOrEmpty(request.Email) && request.Email != provider.Email)
            {
                var existingProvider = await _context.Consignors
                    .AnyAsync(p => p.OrganizationId == organizationId &&
                                  p.Id != id &&
                                  p.Email == request.Email &&
                                  p.Status != ConsignorStatus.Rejected);

                if (existingProvider)
                {
                    return BadRequest(ApiResponse<ProviderDetailDto>.ErrorResult("A provider with this email already exists"));
                }
            }

            // Validate contract dates
            if (request.ContractStartDate.HasValue && request.ContractEndDate.HasValue &&
                request.ContractEndDate.Value <= request.ContractStartDate.Value)
            {
                return BadRequest(ApiResponse<ProviderDetailDto>.ErrorResult("Contract end date must be after start date"));
            }

            provider.FirstName = request.FirstName.Trim();
            provider.LastName = request.LastName.Trim();
            provider.Email = request.Email?.Trim();
            provider.Phone = request.Phone?.Trim();
            provider.AddressLine1 = request.AddressLine1?.Trim();
            provider.AddressLine2 = request.AddressLine2?.Trim();
            provider.City = request.City?.Trim();
            provider.State = request.State?.Trim();
            provider.PostalCode = request.PostalCode?.Trim();
            provider.CommissionRate = request.CommissionRate;
            provider.ContractStartDate = request.ContractStartDate;
            provider.ContractEndDate = request.ContractEndDate;
            provider.PreferredPaymentMethod = request.PreferredPaymentMethod?.Trim();
            provider.PaymentDetails = request.PaymentDetails?.Trim();
            provider.Notes = request.Notes?.Trim();
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor updated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Consignor updated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to update provider"));
        }
    }

    // DEACTIVATE - Soft deactivate provider
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> DeactivateProvider(Guid id, [FromBody] DeactivateProviderRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var provider = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Active)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Active provider not found"));
            }

            provider.Status = ConsignorStatus.Deactivated;
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = request.Reason?.Trim();
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor deactivated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Consignor deactivated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating provider {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to deactivate provider"));
        }
    }

    // REACTIVATE - Restore deactivated provider
    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> ReactivateProvider(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var provider = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Deactivated)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Deactivated provider not found"));
            }

            provider.Status = ConsignorStatus.Active;
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = "Reactivated";
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor reactivated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Consignor reactivated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating provider {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to reactivate provider"));
        }
    }

    #region Private Helper Methods

    private async Task<ProviderMetricsDto> CalculateProviderMetrics(Guid providerId, Guid organizationId)
    {
        var items = await _context.Items
            .Where(i => i.ConsignorId == providerId && i.OrganizationId == organizationId)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.ConsignorId == providerId && t.OrganizationId == organizationId)
            .ToListAsync();

        var payouts = await _context.Payouts
            .Where(p => p.ConsignorId == providerId && p.OrganizationId == organizationId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var totalEarnings = transactions.Sum(t => t.ConsignorAmount);
        var totalPaid = payouts.Sum(p => p.Amount);

        var thisMonthTransactions = transactions.Where(t => t.SaleDate >= startOfMonth).ToList();
        var lastMonthTransactions = transactions
            .Where(t => t.SaleDate >= startOfLastMonth && t.SaleDate < startOfMonth)
            .ToList();

        return new ProviderMetricsDto
        {
            TotalItems = items.Count,
            AvailableItems = items.Count(i => i.Status == ItemStatus.Available),
            SoldItems = items.Count(i => i.Status == ItemStatus.Sold),
            RemovedItems = items.Count(i => i.Status == ItemStatus.Removed),
            InventoryValue = items.Where(i => i.Status == ItemStatus.Available).Sum(i => i.Price),
            PendingBalance = await CalculatePendingBalance(providerId, organizationId),
            TotalEarnings = totalEarnings,
            TotalPaid = totalPaid,
            EarningsThisMonth = thisMonthTransactions.Sum(t => t.ConsignorAmount),
            EarningsLastMonth = lastMonthTransactions.Sum(t => t.ConsignorAmount),
            SalesThisMonth = thisMonthTransactions.Count,
            SalesLastMonth = lastMonthTransactions.Count,
            LastSaleDate = transactions.OrderByDescending(t => t.SaleDate).FirstOrDefault()?.SaleDate,
            LastPayoutDate = payouts.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.CreatedAt,
            LastPayoutAmount = payouts.OrderByDescending(p => p.CreatedAt).FirstOrDefault()?.Amount ?? 0,
            AverageItemPrice = items.Count > 0 ? items.Average(i => i.Price) : 0,
            AverageDaysToSell = CalculateAverageDaysToSell(items, transactions)
        };
    }

    private async Task<decimal> CalculatePendingBalance(Guid providerId, Guid organizationId)
    {
        var totalEarnings = await _context.Transactions
            .Where(t => t.ConsignorId == providerId && t.OrganizationId == organizationId)
            .SumAsync(t => t.ConsignorAmount);

        var totalPaid = await _context.Payouts
            .Where(p => p.ConsignorId == providerId && p.OrganizationId == organizationId)
            .SumAsync(p => p.Amount);

        return totalEarnings - totalPaid;
    }

    private static List<dynamic> ApplySorting(List<dynamic> providers, ProviderQueryParams queryParams)
    {
        return queryParams.SortBy?.ToLower() switch
        {
            "name" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => ((Consignor)p.Consignor).FirstName + " " + ((Consignor)p.Consignor).LastName).ToList()
                : providers.OrderBy(p => ((Consignor)p.Consignor).FirstName + " " + ((Consignor)p.Consignor).LastName).ToList(),
            "createdat" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => ((Consignor)p.Consignor).CreatedAt).ToList()
                : providers.OrderBy(p => ((Consignor)p.Consignor).CreatedAt).ToList(),
            "itemcount" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => (int)p.TotalItemCount).ToList()
                : providers.OrderBy(p => (int)p.TotalItemCount).ToList(),
            "balance" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => (decimal)p.PendingBalance).ToList()
                : providers.OrderBy(p => (decimal)p.PendingBalance).ToList(),
            _ => providers.OrderBy(p => ((Consignor)p.Consignor).FirstName + " " + ((Consignor)p.Consignor).LastName).ToList()
        };
    }

    private static decimal CalculateAverageDaysToSell(List<Item> items, List<Transaction> transactions)
    {
        var soldItems = items.Where(i => i.Status == ItemStatus.Sold).ToList();
        if (!soldItems.Any()) return 0;

        var totalDays = 0m;
        var count = 0;

        foreach (var item in soldItems)
        {
            var transaction = transactions.FirstOrDefault(t => t.ItemId == item.Id);
            if (transaction != null)
            {
                var days = (decimal)(transaction.SaleDate - item.CreatedAt).TotalDays;
                if (days >= 0)
                {
                    totalDays += days;
                    count++;
                }
            }
        }

        return count > 0 ? totalDays / count : 0;
    }

    private static string? FormatAddress(Consignor provider)
    {
        var parts = new List<string?>();

        if (!string.IsNullOrEmpty(provider.AddressLine1))
            parts.Add(provider.AddressLine1);

        if (!string.IsNullOrEmpty(provider.AddressLine2))
            parts.Add(provider.AddressLine2);

        var cityStateParts = new List<string?>();
        if (!string.IsNullOrEmpty(provider.City)) cityStateParts.Add(provider.City);
        if (!string.IsNullOrEmpty(provider.State)) cityStateParts.Add(provider.State);
        if (!string.IsNullOrEmpty(provider.PostalCode)) cityStateParts.Add(provider.PostalCode);

        if (cityStateParts.Any())
            parts.Add(string.Join(", ", cityStateParts.Where(p => !string.IsNullOrEmpty(p))));

        return parts.Any(p => !string.IsNullOrEmpty(p)) ? string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p))) : null;
    }

    private async Task<string> GenerateProviderNumber(Guid organizationId)
    {
        var lastNumber = await _context.Consignors
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.ConsignorNumber)
            .Select(p => p.ConsignorNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNumber != null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length > 1 && int.TryParse(parts[1], out int num))
            {
                nextNumber = num + 1;
            }
        }

        return $"PRV-{nextNumber:D5}";
    }

    #region Consignor Invitations

    // CREATE INVITATION - Send invitation to new provider
    [HttpPost("invitations")]
    public async Task<ActionResult<ProviderDTOs.ProviderInvitationResultDto>> CreateInvitation([FromBody] ProviderDTOs.CreateProviderInvitationDto request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var invitedById = GetUserId();

            var result = await _invitationService.CreateInvitationAsync(request, organizationId, invitedById);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider invitation");
            return StatusCode(500, new ProviderDTOs.ProviderInvitationResultDto
            {
                Success = false,
                Message = "Internal server error occurred while creating invitation."
            });
        }
    }

    // GET PENDING INVITATIONS - List all pending invitations
    [HttpGet("invitations")]
    public async Task<ActionResult<IEnumerable<ProviderDTOs.ProviderInvitationDto>>> GetPendingInvitations()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var invitations = await _invitationService.GetPendingInvitationsAsync(organizationId);
            return Ok(invitations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending invitations");
            return StatusCode(500, "Internal server error occurred while retrieving invitations.");
        }
    }

    // CANCEL INVITATION - Cancel a pending invitation
    [HttpDelete("invitations/{invitationId}")]
    public async Task<ActionResult> CancelInvitation(Guid invitationId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var success = await _invitationService.CancelInvitationAsync(invitationId, organizationId);

            if (!success)
            {
                return NotFound("Invitation not found or cannot be cancelled.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invitation {InvitationId}", invitationId);
            return StatusCode(500, "Internal server error occurred while cancelling invitation.");
        }
    }

    // RESEND INVITATION - Resend a pending invitation
    [HttpPost("invitations/{invitationId}/resend")]
    public async Task<ActionResult> ResendInvitation(Guid invitationId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var success = await _invitationService.ResendInvitationAsync(invitationId, organizationId);

            if (!success)
            {
                return NotFound("Invitation not found or cannot be resent.");
            }

            return Ok(new { message = "Invitation resent successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation {InvitationId}", invitationId);
            return StatusCode(500, "Internal server error occurred while resending invitation.");
        }
    }

    #endregion

    private Guid GetOrganizationId()
    {
        // Debug: Log all available claims
        Console.WriteLine("=== JWT CLAIMS DEBUG ===");
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"Claim Type: '{claim.Type}', Value: '{claim.Value}'");
        }
        Console.WriteLine("========================");

        // Try both case variations
        var orgIdClaimLower = User.FindFirst("OrganizationId")?.Value;
        var orgIdClaimUpper = User.FindFirst("OrganizationId")?.Value;

        Console.WriteLine($"organizationId (lowercase): {orgIdClaimLower}");
        Console.WriteLine($"OrganizationId (uppercase): {orgIdClaimUpper}");

        var finalValue = orgIdClaimLower ?? orgIdClaimUpper;
        Console.WriteLine($"Final OrganizationId: {finalValue}");

        return finalValue != null ? Guid.Parse(finalValue) : Guid.Empty;
    }

    private Guid GetUserId()
    {
        // Try both standard claim types
        var userIdClaim = User.FindFirst("userId")?.Value;
        var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Console.WriteLine($"userId claim: {userIdClaim}");
        Console.WriteLine($"NameIdentifier claim: {nameIdentifierClaim}");

        var finalValue = userIdClaim ?? nameIdentifierClaim;
        Console.WriteLine($"Final UserId: {finalValue}");

        return finalValue != null ? Guid.Parse(finalValue) : Guid.Empty;
    }

    #endregion
}