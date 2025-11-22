using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Items;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class ProvidersController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(ConsignmentGenieContext context, ILogger<ProvidersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // LIST - Get providers with filtering/pagination
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProviderListDto>>> GetProviders([FromQuery] ProviderQueryParams queryParams)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var query = _context.Providers
                .Include(p => p.Items)
                .Include(p => p.Transactions)
                .Where(p => p.OrganizationId == organizationId);

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(p =>
                    (p.FirstName + " " + p.LastName).Contains(queryParams.Search) ||
                    p.ProviderNumber.Contains(queryParams.Search) ||
                    (p.Email != null && p.Email.Contains(queryParams.Search)));
            }

            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                if (Enum.TryParse<ProviderStatus>(queryParams.Status, out var status))
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
                var totalEarnings = provider.Transactions.Sum(t => t.ProviderAmount);

                providersWithMetrics.Add(new
                {
                    Provider = provider,
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
                ProviderId = ((Provider)p.Provider).Id,
                ProviderNumber = ((Provider)p.Provider).ProviderNumber,
                FullName = $"{((Provider)p.Provider).FirstName} {((Provider)p.Provider).LastName}",
                Email = ((Provider)p.Provider).Email,
                Phone = ((Provider)p.Provider).Phone,
                CommissionRate = ((Provider)p.Provider).CommissionRate,
                Status = ((Provider)p.Provider).Status.ToString(),
                ActiveItemCount = (int)p.ActiveItemCount,
                TotalItemCount = (int)p.TotalItemCount,
                PendingBalance = (decimal)p.PendingBalance,
                TotalEarnings = (decimal)p.TotalEarnings,
                HasPortalAccess = ((Provider)p.Provider).UserId != null,
                CreatedAt = ((Provider)p.Provider).CreatedAt
            }).ToList();

            var result = new PagedResult<ProviderListDto>
            {
                Items = providerDtos,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasNextPage = queryParams.Page < (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.Page > 1,
                OrganizationId = organizationId.ToString()
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
            var provider = await _context.Providers
                .Include(p => p.ApprovedByUser)
                .Include(p => p.User)
                .Where(p => p.Id == id && p.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Provider not found"));
            }

            var metrics = await CalculateProviderMetrics(provider.Id, organizationId);

            var providerDto = new ProviderDetailDto
            {
                ProviderId = provider.Id,
                UserId = provider.UserId,
                ProviderNumber = provider.ProviderNumber,
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
            _logger.LogError(ex, "Error getting provider {ProviderId}", id);
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
                var existingProvider = await _context.Providers
                    .AnyAsync(p => p.OrganizationId == organizationId &&
                                  p.Email == request.Email &&
                                  p.Status != ProviderStatus.Rejected);

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

            var provider = new Provider
            {
                OrganizationId = organizationId,
                ProviderNumber = providerNumber,
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
                Status = ProviderStatus.Active,
                CreatedBy = userId
            };

            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Provider created successfully";
                return CreatedAtAction(nameof(GetProvider), new { id = provider.Id }, apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Provider created but failed to retrieve details"));
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

            var provider = await _context.Providers
                .Where(p => p.Id == id && p.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Provider not found"));
            }

            // Check email uniqueness
            if (!string.IsNullOrEmpty(request.Email) && request.Email != provider.Email)
            {
                var existingProvider = await _context.Providers
                    .AnyAsync(p => p.OrganizationId == organizationId &&
                                  p.Id != id &&
                                  p.Email == request.Email &&
                                  p.Status != ProviderStatus.Rejected);

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
                apiResponse.Message = "Provider updated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Provider updated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider {ProviderId}", id);
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

            var provider = await _context.Providers
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ProviderStatus.Active)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Active provider not found"));
            }

            provider.Status = ProviderStatus.Deactivated;
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = request.Reason?.Trim();
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Provider deactivated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Provider deactivated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating provider {ProviderId}", id);
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

            var provider = await _context.Providers
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ProviderStatus.Deactivated)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Deactivated provider not found"));
            }

            provider.Status = ProviderStatus.Active;
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = "Reactivated";
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Provider reactivated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Provider reactivated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating provider {ProviderId}", id);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to reactivate provider"));
        }
    }

    // APPROVE - Approve pending self-registration
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> ApproveProvider(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var provider = await _context.Providers
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ProviderStatus.Pending)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Pending provider not found"));
            }

            provider.Status = ProviderStatus.Active;
            provider.ApprovalStatus = "Approved";
            provider.ApprovedAt = DateTime.UtcNow;
            provider.ApprovedBy = userId;
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = "Approved registration";
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Provider approved successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Provider approved but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving provider {ProviderId}", id);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to approve provider"));
        }
    }

    // REJECT - Reject pending self-registration
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> RejectProvider(Guid id, [FromBody] RejectProviderRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var provider = await _context.Providers
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ProviderStatus.Pending)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Pending provider not found"));
            }

            provider.Status = ProviderStatus.Rejected;
            provider.ApprovalStatus = "Rejected";
            provider.RejectedReason = request.Reason.Trim();
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = $"Rejected: {request.Reason}";
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProvider(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Provider rejected successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Provider rejected but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting provider {ProviderId}", id);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to reject provider"));
        }
    }

    // PENDING COUNT - For badge/notification
    [HttpGet("pending/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetPendingApprovalCount()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var count = await _context.Providers
                .Where(p => p.OrganizationId == organizationId && p.Status == ProviderStatus.Pending)
                .CountAsync();

            return Ok(ApiResponse<int>.SuccessResult(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approval count");
            return StatusCode(500, ApiResponse<int>.ErrorResult("Failed to retrieve pending count"));
        }
    }

    // GENERATE NUMBER - Get next available provider number
    [HttpGet("generate-number")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateProviderNumber()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var providerNumber = await GenerateProviderNumber(organizationId);
            return Ok(ApiResponse<string>.SuccessResult(providerNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating provider number");
            return StatusCode(500, ApiResponse<string>.ErrorResult("Failed to generate provider number"));
        }
    }

    private async Task<ProviderMetricsDto> CalculateProviderMetrics(Guid providerId, Guid organizationId)
    {
        var items = await _context.Items
            .Where(i => i.ProviderId == providerId && i.OrganizationId == organizationId)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.ProviderId == providerId && t.OrganizationId == organizationId)
            .ToListAsync();

        var payouts = await _context.Payouts
            .Where(p => p.ProviderId == providerId && p.OrganizationId == organizationId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var totalEarnings = transactions.Sum(t => t.ProviderAmount);
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
            EarningsThisMonth = thisMonthTransactions.Sum(t => t.ProviderAmount),
            EarningsLastMonth = lastMonthTransactions.Sum(t => t.ProviderAmount),
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
            .Where(t => t.ProviderId == providerId && t.OrganizationId == organizationId)
            .SumAsync(t => t.ProviderAmount);

        var totalPaid = await _context.Payouts
            .Where(p => p.ProviderId == providerId && p.OrganizationId == organizationId)
            .SumAsync(p => p.Amount);

        return totalEarnings - totalPaid;
    }

    private static List<dynamic> ApplySorting(List<dynamic> providers, ProviderQueryParams queryParams)
    {
        return queryParams.SortBy?.ToLower() switch
        {
            "name" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => ((Provider)p.Provider).FirstName + " " + ((Provider)p.Provider).LastName).ToList()
                : providers.OrderBy(p => ((Provider)p.Provider).FirstName + " " + ((Provider)p.Provider).LastName).ToList(),
            "createdat" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => ((Provider)p.Provider).CreatedAt).ToList()
                : providers.OrderBy(p => ((Provider)p.Provider).CreatedAt).ToList(),
            "itemcount" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => (int)p.TotalItemCount).ToList()
                : providers.OrderBy(p => (int)p.TotalItemCount).ToList(),
            "balance" => queryParams.SortDirection?.ToLower() == "desc"
                ? providers.OrderByDescending(p => (decimal)p.PendingBalance).ToList()
                : providers.OrderBy(p => (decimal)p.PendingBalance).ToList(),
            _ => providers.OrderBy(p => ((Provider)p.Provider).FirstName + " " + ((Provider)p.Provider).LastName).ToList()
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

    private static string? FormatAddress(Provider provider)
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
        var lastNumber = await _context.Providers
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.ProviderNumber)
            .Select(p => p.ProviderNumber)
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

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        return orgIdClaim != null ? Guid.Parse(orgIdClaim) : Guid.Empty;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }
}