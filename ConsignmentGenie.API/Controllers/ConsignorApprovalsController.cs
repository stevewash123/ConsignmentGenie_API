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
public class ConsignorApprovalsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ConsignorApprovalsController> _logger;

    public ConsignorApprovalsController(ConsignmentGenieContext context, ILogger<ConsignorApprovalsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // APPROVE - Approve pending self-registration
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> ApproveConsignor(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var consignor = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .FirstOrDefaultAsync();

            if (consignor == null)
            {
                return NotFound(ApiResponse<ConsignorDetailDto>.ErrorResult("Pending consignor not found"));
            }

            consignor.Status = ConsignorStatus.Active;
            consignor.ApprovalStatus = "Approved";
            consignor.ApprovedAt = DateTime.UtcNow;
            consignor.ApprovedBy = userId;
            consignor.StatusChangedAt = DateTime.UtcNow;
            consignor.StatusChangedReason = "Approved registration";
            consignor.UpdatedAt = DateTime.UtcNow;
            consignor.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetConsignorDetails(consignor.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ConsignorDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor approved successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor approved but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving consignor {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to approve consignor"));
        }
    }

    // REJECT - Reject pending self-registration
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> RejectConsignor(Guid id, [FromBody] RejectConsignorRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var consignor = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .FirstOrDefaultAsync();

            if (consignor == null)
            {
                return NotFound(ApiResponse<ConsignorDetailDto>.ErrorResult("Pending consignor not found"));
            }

            consignor.Status = ConsignorStatus.Rejected;
            consignor.ApprovalStatus = "Rejected";
            consignor.RejectedReason = request.Reason.Trim();
            consignor.StatusChangedAt = DateTime.UtcNow;
            consignor.StatusChangedReason = $"Rejected: {request.Reason}";
            consignor.UpdatedAt = DateTime.UtcNow;
            consignor.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetConsignorDetails(consignor.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ConsignorDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor rejected successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor rejected but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting consignor {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to reject consignor"));
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

    // GET PENDING - Get all consignors pending approval
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<ConsignorApprovalSummaryDto>>>> GetPendingConsignors()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var consignors = await _context.Consignors
                .Where(p => p.OrganizationId == organizationId && p.Status == ConsignorStatus.Pending)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            var consignorDtos = consignors.Select(p => new ConsignorApprovalSummaryDto
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

            return Ok(ApiResponse<List<ConsignorApprovalSummaryDto>>.SuccessResult(consignorDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending consignors for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<List<ConsignorApprovalSummaryDto>>.ErrorResult("Failed to retrieve pending consignors"));
        }
    }

    // GET APPROVAL HISTORY - Get approval/rejection history
    [HttpGet("approval-history")]
    public async Task<ActionResult<ApiResponse<List<ConsignorApprovalHistoryDto>>>> GetApprovalHistory(
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
            var consignors = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var historyDtos = consignors.Select(p => new ConsignorApprovalHistoryDto
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

            var result = new PagedResult<ConsignorApprovalHistoryDto>
            {
                Items = historyDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                OrganizationId = organizationId
            };

            return Ok(ApiResponse<PagedResult<ConsignorApprovalHistoryDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approval history for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<PagedResult<ConsignorApprovalHistoryDto>>.ErrorResult("Failed to retrieve approval history"));
        }
    }

    #region Private Helper Methods

    private async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> GetConsignorDetails(Guid consignorId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var consignor = await _context.Consignors
                .Include(p => p.ApprovedByUser)
                .Include(p => p.User)
                .Where(p => p.Id == consignorId && p.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (consignor == null)
            {
                return NotFound(ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor not found"));
            }

            var metrics = await CalculateProviderMetrics(consignor.Id, organizationId);

            var consignorDto = new ConsignorDetailDto
            {
                ConsignorId = consignor.Id,
                UserId = consignor.UserId,
                ConsignorNumber = consignor.ConsignorNumber,
                FirstName = consignor.FirstName,
                LastName = consignor.LastName,
                FullName = $"{consignor.FirstName} {consignor.LastName}",
                Email = consignor.Email,
                Phone = consignor.Phone,
                AddressLine1 = consignor.AddressLine1,
                AddressLine2 = consignor.AddressLine2,
                City = consignor.City,
                State = consignor.State,
                PostalCode = consignor.PostalCode,
                FullAddress = FormatAddress(consignor),
                CommissionRate = consignor.CommissionRate,
                ContractStartDate = consignor.ContractStartDate,
                ContractEndDate = consignor.ContractEndDate,
                IsContractExpired = consignor.ContractEndDate.HasValue && consignor.ContractEndDate.Value < DateTime.UtcNow,
                PreferredPaymentMethod = consignor.PreferredPaymentMethod,
                PaymentDetails = consignor.PaymentDetails,
                Status = consignor.Status.ToString(),
                StatusChangedAt = consignor.StatusChangedAt,
                StatusChangedReason = consignor.StatusChangedReason,
                ApprovalStatus = consignor.ApprovalStatus,
                ApprovedAt = consignor.ApprovedAt,
                ApprovedByName = consignor.ApprovedByUser?.Email,
                RejectedReason = consignor.RejectedReason,
                Notes = consignor.Notes,
                HasPortalAccess = consignor.UserId != null,
                Metrics = metrics,
                CreatedAt = consignor.CreatedAt,
                UpdatedAt = consignor.UpdatedAt ?? consignor.CreatedAt
            };

            return Ok(ApiResponse<ConsignorDetailDto>.SuccessResult(consignorDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consignor details {ConsignorId}", consignorId);
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to retrieve consignor details"));
        }
    }

    private async Task<ConsignorMetricsDto> CalculateProviderMetrics(Guid consignorId, Guid organizationId)
    {
        var items = await _context.Items
            .Where(i => i.ConsignorId == consignorId && i.OrganizationId == organizationId)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.ConsignorId == consignorId && t.OrganizationId == organizationId)
            .ToListAsync();

        var payouts = await _context.Payouts
            .Where(p => p.ConsignorId == consignorId && p.OrganizationId == organizationId)
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

        return new ConsignorMetricsDto
        {
            TotalItems = items.Count,
            AvailableItems = items.Count(i => i.Status == ItemStatus.Available),
            SoldItems = items.Count(i => i.Status == ItemStatus.Sold),
            RemovedItems = items.Count(i => i.Status == ItemStatus.Removed),
            InventoryValue = items.Where(i => i.Status == ItemStatus.Available).Sum(i => i.Price),
            PendingBalance = await CalculatePendingBalance(consignorId, organizationId),
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

    private async Task<decimal> CalculatePendingBalance(Guid consignorId, Guid organizationId)
    {
        var totalEarnings = await _context.Transactions
            .Where(t => t.ConsignorId == consignorId && t.OrganizationId == organizationId)
            .SumAsync(t => t.ConsignorAmount);

        var totalPaid = await _context.Payouts
            .Where(p => p.ConsignorId == consignorId && p.OrganizationId == organizationId)
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

    private static string? FormatAddress(Consignor consignor)
    {
        var parts = new List<string?>();

        if (!string.IsNullOrEmpty(consignor.AddressLine1))
            parts.Add(consignor.AddressLine1);

        if (!string.IsNullOrEmpty(consignor.AddressLine2))
            parts.Add(consignor.AddressLine2);

        var cityStateParts = new List<string?>();
        if (!string.IsNullOrEmpty(consignor.City)) cityStateParts.Add(consignor.City);
        if (!string.IsNullOrEmpty(consignor.State)) cityStateParts.Add(consignor.State);
        if (!string.IsNullOrEmpty(consignor.PostalCode)) cityStateParts.Add(consignor.PostalCode);

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