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
[Route("api/consignors")]
[Authorize(Roles = "Owner")]
public class ConsignorsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ConsignorsController> _logger;
    private readonly IConsignorInvitationService _invitationService;

    public ConsignorsController(ConsignmentGenieContext context, ILogger<ConsignorsController> logger, IConsignorInvitationService invitationService)
    {
        _context = context;
        _logger = logger;
        _invitationService = invitationService;
    }

    // LIST - Get consignors with filtering/pagination
    [HttpGet]
    public async Task<ActionResult<PagedResult<ConsignorListDto>>> GetProviders([FromQuery] ConsignorQueryParams queryParams)
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
            var consignors = await query.ToListAsync();
            var consignorsWithMetrics = new List<dynamic>();

            foreach (var consignor in consignors)
            {
                var pendingBalance = await CalculatePendingBalance(consignor.Id, organizationId);
                var totalEarnings = consignor.Transactions.Sum(t => t.ConsignorAmount);

                consignorsWithMetrics.Add(new
                {
                    Consignor = consignor,
                    PendingBalance = pendingBalance,
                    ActiveItemCount = consignor.Items.Count(i => i.Status == ItemStatus.Available),
                    TotalItemCount = consignor.Items.Count(),
                    TotalEarnings = totalEarnings
                });
            }

            // Apply pending balance filter
            if (queryParams.HasPendingBalance.HasValue)
            {
                consignorsWithMetrics = consignorsWithMetrics
                    .Where(p => queryParams.HasPendingBalance.Value
                        ? ((decimal)p.PendingBalance) > 0
                        : ((decimal)p.PendingBalance) == 0)
                    .ToList();
            }

            // Apply sorting
            consignorsWithMetrics = ApplySorting(consignorsWithMetrics, queryParams);

            // Apply pagination
            var totalCount = consignorsWithMetrics.Count;
            var pagedProviders = consignorsWithMetrics
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            var consignorDtos = pagedProviders.Select(p => new ConsignorListDto
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

            var result = new PagedResult<ConsignorListDto>
            {
                Items = consignorDtos,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                OrganizationId = organizationId
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consignors for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, "Failed to retrieve consignors");
        }
    }

    // GET ONE - Get consignor by ID with full details
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> GetConsignor(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var consignor = await _context.Consignors
                .Include(p => p.ApprovedByUser)
                .Include(p => p.User)
                .Where(p => p.Id == id && p.OrganizationId == organizationId)
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
            _logger.LogError(ex, "Error getting consignor {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to retrieve consignor"));
        }
    }

    // CREATE - Add new consignor manually
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> CreateConsignor([FromBody] CreateConsignorRequest request)
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
                    return BadRequest(ApiResponse<ConsignorDetailDto>.ErrorResult("A consignor with this email already exists"));
                }
            }

            // Validate contract dates
            if (request.ContractStartDate.HasValue && request.ContractEndDate.HasValue &&
                request.ContractEndDate.Value <= request.ContractStartDate.Value)
            {
                return BadRequest(ApiResponse<ConsignorDetailDto>.ErrorResult("Contract end date must be after start date"));
            }

            var consignorNumber = await GenerateProviderNumber(organizationId);

            var consignor = new Consignor
            {
                OrganizationId = organizationId,
                ConsignorNumber = consignorNumber,
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

            _context.Consignors.Add(consignor);
            await _context.SaveChangesAsync();

            var result = await GetConsignor(consignor.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ConsignorDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor created successfully";
                return CreatedAtAction(nameof(GetConsignor), new { id = consignor.Id }, apiResponse);
            }

            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor created but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating consignor");
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to create consignor"));
        }
    }

    // UPDATE - Edit consignor
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> UpdateConsignor(Guid id, [FromBody] UpdateConsignorRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var consignor = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (consignor == null)
            {
                return NotFound(ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor not found"));
            }

            // Check email uniqueness
            if (!string.IsNullOrEmpty(request.Email) && request.Email != consignor.Email)
            {
                var existingProvider = await _context.Consignors
                    .AnyAsync(p => p.OrganizationId == organizationId &&
                                  p.Id != id &&
                                  p.Email == request.Email &&
                                  p.Status != ConsignorStatus.Rejected);

                if (existingProvider)
                {
                    return BadRequest(ApiResponse<ConsignorDetailDto>.ErrorResult("A consignor with this email already exists"));
                }
            }

            // Validate contract dates
            if (request.ContractStartDate.HasValue && request.ContractEndDate.HasValue &&
                request.ContractEndDate.Value <= request.ContractStartDate.Value)
            {
                return BadRequest(ApiResponse<ConsignorDetailDto>.ErrorResult("Contract end date must be after start date"));
            }

            consignor.FirstName = request.FirstName.Trim();
            consignor.LastName = request.LastName.Trim();
            consignor.Email = request.Email?.Trim();
            consignor.Phone = request.Phone?.Trim();
            consignor.AddressLine1 = request.AddressLine1?.Trim();
            consignor.AddressLine2 = request.AddressLine2?.Trim();
            consignor.City = request.City?.Trim();
            consignor.State = request.State?.Trim();
            consignor.PostalCode = request.PostalCode?.Trim();
            consignor.CommissionRate = request.CommissionRate;
            consignor.ContractStartDate = request.ContractStartDate;
            consignor.ContractEndDate = request.ContractEndDate;
            consignor.PreferredPaymentMethod = request.PreferredPaymentMethod?.Trim();
            consignor.PaymentDetails = request.PaymentDetails?.Trim();
            consignor.Notes = request.Notes?.Trim();
            consignor.UpdatedAt = DateTime.UtcNow;
            consignor.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetConsignor(consignor.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ConsignorDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor updated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor updated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating consignor {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to update consignor"));
        }
    }

    // DEACTIVATE - Soft deactivate consignor
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> DeactivateProvider(Guid id, [FromBody] DeactivateConsignorRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var consignor = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Active)
                .FirstOrDefaultAsync();

            if (consignor == null)
            {
                return NotFound(ApiResponse<ConsignorDetailDto>.ErrorResult("Active consignor not found"));
            }

            consignor.Status = ConsignorStatus.Deactivated;
            consignor.StatusChangedAt = DateTime.UtcNow;
            consignor.StatusChangedReason = request.Reason?.Trim();
            consignor.UpdatedAt = DateTime.UtcNow;
            consignor.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetConsignor(consignor.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ConsignorDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor deactivated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor deactivated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating consignor {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to deactivate consignor"));
        }
    }

    // REACTIVATE - Restore deactivated consignor
    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<ApiResponse<ConsignorDetailDto>>> ReactivateProvider(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var consignor = await _context.Consignors
                .Where(p => p.Id == id && p.OrganizationId == organizationId && p.Status == ConsignorStatus.Deactivated)
                .FirstOrDefaultAsync();

            if (consignor == null)
            {
                return NotFound(ApiResponse<ConsignorDetailDto>.ErrorResult("Deactivated consignor not found"));
            }

            consignor.Status = ConsignorStatus.Active;
            consignor.StatusChangedAt = DateTime.UtcNow;
            consignor.StatusChangedReason = "Reactivated";
            consignor.UpdatedAt = DateTime.UtcNow;
            consignor.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var result = await GetConsignor(consignor.Id);
            if (result.Result is OkObjectResult okResult &&
                okResult.Value is ApiResponse<ConsignorDetailDto> apiResponse)
            {
                apiResponse.Message = "Consignor reactivated successfully";
                return Ok(apiResponse);
            }

            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Consignor reactivated but failed to retrieve details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating consignor {ConsignorId}", id);
            return StatusCode(500, ApiResponse<ConsignorDetailDto>.ErrorResult("Failed to reactivate consignor"));
        }
    }

    #region Private Helper Methods

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

    private static List<dynamic> ApplySorting(List<dynamic> consignors, ConsignorQueryParams queryParams)
    {
        return queryParams.SortBy?.ToLower() switch
        {
            "name" => queryParams.SortDirection?.ToLower() == "desc"
                ? consignors.OrderByDescending(p => ((Consignor)p.Consignor).FirstName + " " + ((Consignor)p.Consignor).LastName).ToList()
                : consignors.OrderBy(p => ((Consignor)p.Consignor).FirstName + " " + ((Consignor)p.Consignor).LastName).ToList(),
            "createdat" => queryParams.SortDirection?.ToLower() == "desc"
                ? consignors.OrderByDescending(p => ((Consignor)p.Consignor).CreatedAt).ToList()
                : consignors.OrderBy(p => ((Consignor)p.Consignor).CreatedAt).ToList(),
            "itemcount" => queryParams.SortDirection?.ToLower() == "desc"
                ? consignors.OrderByDescending(p => (int)p.TotalItemCount).ToList()
                : consignors.OrderBy(p => (int)p.TotalItemCount).ToList(),
            "balance" => queryParams.SortDirection?.ToLower() == "desc"
                ? consignors.OrderByDescending(p => (decimal)p.PendingBalance).ToList()
                : consignors.OrderBy(p => (decimal)p.PendingBalance).ToList(),
            _ => consignors.OrderBy(p => ((Consignor)p.Consignor).FirstName + " " + ((Consignor)p.Consignor).LastName).ToList()
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

    // CREATE INVITATION - Send invitation to new consignor
    [HttpPost("invitations")]
    public async Task<ActionResult<ProviderDTOs.ConsignorInvitationResultDto>> CreateInvitation([FromBody] ProviderDTOs.CreateConsignorInvitationDto request)
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
            _logger.LogError(ex, "Error creating consignor invitation");
            return StatusCode(500, new ProviderDTOs.ConsignorInvitationResultDto
            {
                Success = false,
                Message = "Internal server error occurred while creating invitation."
            });
        }
    }

    // GET PENDING INVITATIONS - List all pending invitations
    [HttpGet("invitations")]
    public async Task<ActionResult<IEnumerable<ProviderDTOs.ConsignorInvitationDto>>> GetPendingInvitations()
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