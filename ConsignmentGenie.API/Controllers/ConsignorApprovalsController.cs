using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Application.DTOs;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/consignors")]
[Authorize(Roles = "Owner")]
public class ProviderApprovalsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ProviderApprovalsController> _logger;

    public ProviderApprovalsController(ConsignmentGenieContext context, ILogger<ProviderApprovalsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // APPROVE - Approve pending self-registration
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ProviderDetailDto>>> ApproveProvider(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var provider = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Pending provider not found"));
            }

            provider.Status = ConsignorStatus.Active;
            provider.ApprovalStatus = "Approved";
            provider.ApprovedAt = DateTime.UtcNow;
            provider.ApprovedBy = userId;
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = "Approved registration";
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProviderDetails(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor approved successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Consignor approved but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving provider {ConsignorId}", id);
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

            var provider = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDetailDto>.ErrorResult("Pending provider not found"));
            }

            provider.Status = ConsignorStatus.Rejected;
            provider.ApprovalStatus = "Rejected";
            provider.RejectedReason = request.Reason.Trim();
            provider.StatusChangedAt = DateTime.UtcNow;
            provider.StatusChangedReason = $"Rejected: {request.Reason}";
            provider.UpdatedAt = DateTime.UtcNow;
            provider.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetProviderDetails(provider.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ProviderDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor rejected successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Consignor rejected but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting provider {ConsignorId}", id);
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
            var count = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .CountAsync();

            return Ok(ApiResponse<int>.SuccessResult(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approval count");
            return StatusCode(500, ApiResponse<int>.ErrorResult("Failed to retrieve pending count"));
        }
    }

    // GET PENDING - Get all providers pending approval
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<ProviderApprovalSummaryDto>>>> GetPendingConsignors()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var providers = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            var providerDtos = providers.Select(p => new ProviderApprovalSummaryDto
            {
                ConsignorId = p.Id,
                ConsignorNumber = p.ConsignorNumber,
                FullName = $"{p.FirstName} {p.LastName}",
                Email = p.Email,
                Phone = p.Phone,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                CommissionRate = p.CommissionRate
            }).ToList();

            return Ok(ApiResponse<List<ProviderApprovalSummaryDto>>.SuccessResult(providerDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending providers for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<List<ProviderApprovalSummaryDto>>.ErrorResult("Failed to retrieve pending providers"));
        }
    }

    // GET APPROVAL HISTORY - Get approval/rejection history
    [HttpGet("approval-history")]
    public async Task<ActionResult<ApiResponse<List<ProviderApprovalHistoryDto>>>> GetApprovalHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var query = _context.Consignors
                .Include(p => p.ApprovedByUser)
                .Where(p => p.OrganizationId == organizationId &&
                           (p.Status == ConsignorStatus.Active || p.Status == ConsignorStatus.Rejected) &&
                           (p.ApprovedAt.HasValue || !string.IsNullOrEmpty(p.RejectedReason)))
                .OrderByDescending(p => p.ApprovedAt ?? p.StatusChangedAt);

            var totalCount = await query.CountAsync();
            var providers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var historyDtos = providers.Select(p => new ProviderApprovalHistoryDto
            {
                ConsignorId = p.Id,
                ConsignorNumber = p.ConsignorNumber,
                ConsignorName = $"{p.FirstName} {p.LastName}",
                Email = p.Email,
                ApprovalStatus = p.ApprovalStatus,
                ApprovedAt = p.ApprovedAt,
                ApprovedByName = p.ApprovedByUser?.Email,
                RejectedReason = p.RejectedReason,
                CreatedAt = p.CreatedAt
            }).ToList();

            var result = new PagedResult<ProviderApprovalHistoryDto>
            {
                Items = historyDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                OrganizationId = organizationId
            };

            return Ok(ApiResponse<PagedResult<ProviderApprovalHistoryDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approval history for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<PagedResult<ProviderApprovalHistoryDto>>.ErrorResult("Failed to retrieve approval history"));
        }
    }

    #region Private Helper Methods

    private async Task<ActionResult<ApiResponse<ProviderDetailDto>>> GetProviderDetails(Guid providerId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var provider = await _context.Consignors
                .Include(p => p.ApprovedByUser)
                .Include(p => p.User)
                .Where(p => p.Id == providerId && p.OrganizationId == organizationId)
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
            _logger.LogError(ex, "Error getting provider details {ConsignorId}", providerId);
            return StatusCode(500, ApiResponse<ProviderDetailDto>.ErrorResult("Failed to retrieve provider details"));
        }
    }

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

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
        return orgIdClaim != null ? Guid.Parse(orgIdClaim) : Guid.Empty;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
    }

    #endregion
}