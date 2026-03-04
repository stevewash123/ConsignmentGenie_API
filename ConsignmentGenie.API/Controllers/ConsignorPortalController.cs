using ConsignmentGenie.Application.DTOs.Consignor;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/consignor")]
[Authorize(Roles = "Consignor")]
public class ConsignorPortalController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly ILogger<ConsignorPortalController> _logger;
    private readonly INotificationService _notificationService;
    private readonly IDocumentService _documentService;
    private readonly IAgreementGeneratorService _agreementGeneratorService;
    private readonly IPdfReportGenerator _pdfReportGenerator;
    private readonly IPhotoService _photoService;

    public ConsignorPortalController(
        IUnitOfWork unitOfWork,
        IAuthService authService,
        ILogger<ConsignorPortalController> logger,
        INotificationService notificationService,
        IDocumentService documentService,
        IAgreementGeneratorService agreementGeneratorService,
        IPdfReportGenerator pdfReportGenerator,
        IPhotoService photoService)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
        _logger = logger;
        _notificationService = notificationService;
        _documentService = documentService;
        _agreementGeneratorService = agreementGeneratorService;
        _pdfReportGenerator = pdfReportGenerator;
        _photoService = photoService;
    }

    // DASHBOARD
    [HttpGet("dashboard")]
    public async Task<ActionResult<ConsignorDashboardDto>> GetDashboard()
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var provider = await _unitOfWork.Consignors
                .GetAsync(p => p.Id == consignorId.Value,
                    includeProperties: "Items,Payouts,Transactions,Organization");

            if (provider == null)
                return NotFound("Consignor not found");

            // Calculate dashboard metrics
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dashboard = new ConsignorDashboardDto
            {
                ShopName = provider.Organization.Name,
                ConsignorName = provider.DisplayName,

                // Items
                TotalItems = provider.Items.Count,
                AvailableItems = provider.Items.Count(i => i.Status == ItemStatus.Available),
                SoldItems = provider.Items.Count(i => i.Status == ItemStatus.Sold),
                InventoryValue = provider.Items
                    .Where(i => i.Status == ItemStatus.Available)
                    .Sum(i => i.Price * (provider.CommissionRate / 100)),

                // Earnings
                PendingBalance = provider.Transactions
                    .Where(t => !t.ConsignorPaidOut)
                    .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == provider.Id))
                    .Sum(ti => ti.ConsignorAmount),
                TotalEarningsAllTime = provider.Transactions
                    .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == provider.Id))
                    .Sum(ti => ti.ConsignorAmount),
                EarningsThisMonth = provider.Transactions
                    .Where(t => t.TransactionDate >= thisMonthStart)
                    .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == provider.Id))
                    .Sum(ti => ti.ConsignorAmount),

                // Recent activity - get last 5 sales
                RecentSales = provider.Transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .SelectMany(t => t.Items.Where(ti => ti.ConsignorId == provider.Id)
                        .Select(ti => new { Transaction = t, TransactionItem = ti }))
                    .Take(5)
                    .Select(x => new ConsignorSaleDto
                    {
                        TransactionId = x.Transaction.Id,
                        SaleDate = x.Transaction.TransactionDate,
                        ItemTitle = x.TransactionItem.Item?.Title ?? "Unknown Item",
                        ItemSku = x.TransactionItem.Item?.Sku ?? "",
                        SalePrice = x.TransactionItem.UnitPrice * x.TransactionItem.Quantity,
                        MyEarnings = x.TransactionItem.ConsignorAmount,
                        PayoutStatus = x.Transaction.ConsignorPaidOut ? "Paid" : "Pending"
                    }).ToList(),

                // Last payout
                LastPayout = provider.Payouts
                    .Where(p => p.PaidAt.HasValue)
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => new ConsignorPayoutDto
                    {
                        PayoutId = p.Id,
                        PayoutNumber = p.PayoutNumber,
                        PayoutDate = p.PaidAt!.Value,
                        Amount = p.TotalAmount,
                        PaymentMethod = provider.PaymentMethod ?? "Not Set",
                        ItemCount = p.Transactions.Count()
                    })
                    .FirstOrDefault()
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider dashboard");
            return StatusCode(500, "Internal server error");
        }
    }

    // EARNINGS SUMMARY
    [HttpGet("earnings-summary")]
    public async Task<ActionResult<object>> GetEarningsSummary()
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId.Value, includeProperties: "Organization");

            if (consignor == null)
                return NotFound("Consignor not found");

            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Get all transactions for this consignor
            var transactions = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == consignor.OrganizationId);

            // Filter to transactions involving this consignor (simple approach for now)
            var allTransactions = transactions.ToList();
            var totalEarnings = 0m; // Simple calculation - can be enhanced later

            // Get this month's payouts
            var thisMonthPayouts = await _unitOfWork.Payouts
                .GetAllAsync(p => p.ConsignorId == consignorId.Value && p.CreatedAt >= thisMonthStart);

            var paidThisMonth = thisMonthPayouts.Sum(p => p.Amount);
            var payoutCountThisMonth = thisMonthPayouts.Count();

            // Calculate pending amount (simplified - total earnings minus total paid out)
            var totalPaidOut = await _unitOfWork.Payouts
                .GetAllAsync(p => p.ConsignorId == consignorId.Value);
            var allPayouts = totalPaidOut.Sum(p => p.Amount);
            var pendingAmount = Math.Max(0, totalEarnings - allPayouts);

            // Simple next payout date - first of next month
            var nextPayoutDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

            // Generate tooltip
            var pendingTooltip = pendingAmount > 0
                ? $"Expected payout date {nextPayoutDate:M/d/yyyy}"
                : "No pending earnings";

            var summary = new
            {
                pending = pendingAmount,
                pendingTooltip = pendingTooltip,
                paidThisMonth = paidThisMonth,
                payoutCountThisMonth = payoutCountThisMonth,
                nextPayoutDate = nextPayoutDate
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consignor earnings summary");
            return StatusCode(500, "Internal server error");
        }
    }

    // MY ITEMS
    [HttpGet("items")]
    public async Task<ActionResult<ConsignmentGenie.Application.DTOs.Consignor.PagedResult<ConsignorItemDto>>> GetMyItems(
        [FromQuery] ConsignorItemQueryParams queryParams)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            _logger.LogInformation("GetMyItems: Fetching items for consignorId {ConsignorId} with includeProperties: 'Consignor,Transaction'", consignorId.Value);

            var itemsQuery = await _unitOfWork.Items
                .GetAllAsync(i => i.ConsignorId == consignorId.Value,
                    includeProperties: "Consignor,Transaction");
            var items = itemsQuery.ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                if (Enum.TryParse<ItemStatus>(queryParams.Status, out var status))
                    items = items.Where(i => i.Status == status).ToList();
            }

            if (!string.IsNullOrEmpty(queryParams.Category))
                items = items.Where(i => i.ItemCategoryNavigation != null && i.ItemCategoryNavigation.Name == queryParams.Category).ToList();

            if (!string.IsNullOrEmpty(queryParams.Search))
                items = items.Where(i => i.Title.Contains(queryParams.Search, StringComparison.OrdinalIgnoreCase)
                    || i.Description.Contains(queryParams.Search, StringComparison.OrdinalIgnoreCase)).ToList();

            if (queryParams.DateFrom.HasValue)
                items = items.Where(i => i.CreatedAt >= queryParams.DateFrom.Value).ToList();

            if (queryParams.DateTo.HasValue)
                items = items.Where(i => i.CreatedAt <= queryParams.DateTo.Value).ToList();

            // Apply paging
            var totalCount = items.Count;
            var pagedItems = items
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(item => new ConsignorItemDto
                {
                    ItemId = item.Id,
                    Sku = item.Sku,
                    Title = item.Title,
                    PrimaryImageUrl = ExtractPrimaryImage(item.Photos),
                    Price = item.Price,
                    MyEarnings = item.Price * (item.Consignor.CommissionRate / 100),
                    Category = item.ItemCategoryNavigation?.Name,
                    Status = item.Status.ToString(),
                    ReceivedDate = item.CreatedAt,
                    SoldDate = item.Status == ItemStatus.Sold ? (DateTime?)item.Transaction.SaleDate : null,
                    SalePrice = item.Status == ItemStatus.Sold ? (decimal?)item.Transaction.SalePrice() : null
                })
                .ToList();

            var result = new ConsignmentGenie.Application.DTOs.Consignor.PagedResult<ConsignorItemDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider items");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("items/{id}")]
    public async Task<ActionResult<ConsignorItemDetailDto>> GetMyItem(Guid id)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var item = await _unitOfWork.Items
                .GetAsync(i => i.Id == id && i.ConsignorId == consignorId.Value,
                    includeProperties: "Consignor,Transactions");

            if (item == null)
                return NotFound("Item not found");

            var itemDetail = new ConsignorItemDetailDto
            {
                ItemId = item.Id,
                Sku = item.Sku,
                Title = item.Title,
                Description = item.Description,
                PrimaryImageUrl = ExtractPrimaryImage(item.Photos),
                ImageUrls = ExtractAllImages(item.Photos),
                Price = item.Price,
                MyEarnings = item.Price * (item.Consignor.CommissionRate / 100),
                Category = item.ItemCategoryNavigation?.Name,
                Status = item.Status.ToString(),
                ReceivedDate = item.CreatedAt,
                SoldDate = item.Status == ItemStatus.Sold ? item.Transaction.SaleDate : null,
                SalePrice = item.Status == ItemStatus.Sold ? item.Transaction.SalePrice() : null,
                Notes = item.Notes ?? ""
            };

            return Ok(itemDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider item detail");
            return StatusCode(500, "Internal server error");
        }
    }

    // MY SALES
    [HttpGet("sales")]
    public async Task<ActionResult<ConsignmentGenie.Application.DTOs.Consignor.PagedResult<ConsignorSaleDto>>> GetMySales(
        [FromQuery] ConsignorSaleQueryParams queryParams)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var transactionsQuery = await _unitOfWork.Transactions
                .GetAllAsync(t => t.ConsignorId() == consignorId.Value,
                    includeProperties: "Item,Payouts");
            var transactions = transactionsQuery.ToList();

            // Apply filters
            if (queryParams.DateFrom.HasValue)
                transactions = transactions.Where(t => t.SaleDate() >= queryParams.DateFrom.Value).ToList();

            if (queryParams.DateTo.HasValue)
                transactions = transactions.Where(t => t.SaleDate() <= queryParams.DateTo.Value).ToList();

            if (!string.IsNullOrEmpty(queryParams.PayoutStatus))
            {
                switch (queryParams.PayoutStatus.ToLower())
                {
                    case "paid":
                        transactions = transactions.Where(t => t.PayoutId != null).ToList();
                        break;
                    case "pending":
                        transactions = transactions.Where(t => t.PayoutId == null).ToList();
                        break;
                }
            }

            // Apply paging
            var totalCount = transactions.Count;
            var pagedSales = transactions
                .OrderByDescending(t => t.SaleDate())
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(t => new ConsignorSaleDto
                {
                    TransactionId = t.Id,
                    SaleDate = t.SaleDate(),
                    ItemTitle = t.Item()?.Title ?? "Unknown Item",
                    ItemSku = t.Item()?.Sku ?? "",
                    SalePrice = t.SalePrice(),
                    MyEarnings = t.ConsignorAmount(),
                    PayoutStatus = t.PayoutId != null ? "Paid" : "Pending"
                })
                .ToList();

            var result = new ConsignmentGenie.Application.DTOs.Consignor.PagedResult<ConsignorSaleDto>
            {
                Items = pagedSales,
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider sales");
            return StatusCode(500, "Internal server error");
        }
    }

    // MY PAYOUTS
    [HttpGet("payouts")]
    public async Task<ActionResult<ConsignmentGenie.Application.DTOs.Consignor.PagedResult<ConsignorPayoutDto>>> GetMyPayouts()
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var payouts = await _unitOfWork.Payouts
                .GetAllAsync(p => p.ConsignorId == consignorId.Value,
                    includeProperties: "Consignor,Transactions");

            var payoutDtos = payouts
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ConsignorPayoutDto
                {
                    PayoutId = p.Id,
                    PayoutNumber = p.PayoutNumber,
                    PayoutDate = p.PaidAt ?? p.CreatedAt,
                    Amount = p.TotalAmount,
                    PaymentMethod = p.Consignor.PaymentMethod ?? "Not Set",
                    ItemCount = p.Transactions.Count()
                })
                .ToList();

            var result = new ConsignmentGenie.Application.DTOs.Consignor.PagedResult<ConsignorPayoutDto>
            {
                Items = payoutDtos,
                TotalCount = payoutDtos.Count,
                Page = 1,
                PageSize = payoutDtos.Count
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider payouts");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("payouts/{id}")]
    public async Task<ActionResult<ConsignorPayoutDetailDto>> GetMyPayout(Guid id)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var payout = await _unitOfWork.Payouts
                .GetAsync(p => p.Id == id && p.ConsignorId == consignorId.Value,
                    includeProperties: "Consignor,Transactions.Item");

            if (payout == null)
                return NotFound("Payout not found");

            var payoutDetail = new ConsignorPayoutDetailDto
            {
                PayoutId = payout.Id,
                PayoutNumber = payout.PayoutNumber,
                PayoutDate = payout.PaidAt ?? payout.CreatedAt,
                Amount = payout.TotalAmount,
                PaymentMethod = payout.Consignor.PaymentMethod ?? "Not Set",
                ItemCount = payout.Transactions.Count(),
                PaymentReference = payout.PaymentReference ?? "",
                PeriodStart = DateOnly.FromDateTime(payout.PeriodStart),
                PeriodEnd = DateOnly.FromDateTime(payout.PeriodEnd),
                Items = payout.Transactions
                    .Select(t => new ConsignorSaleDto
                    {
                        TransactionId = t.Id,
                        SaleDate = t.SaleDate(),
                        ItemTitle = t.Item()?.Title ?? "Unknown Item",
                        ItemSku = t.Item()?.Sku ?? "",
                        SalePrice = t.SalePrice(),
                        MyEarnings = t.ConsignorAmount(),
                        PayoutStatus = "Paid"
                    })
                    .ToList()
            };

            return Ok(payoutDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider payout detail");
            return StatusCode(500, "Internal server error");
        }
    }

    // PROFILE
    [HttpGet("profile")]
    public async Task<ActionResult<ConsignorProfileDto>> GetProfile()
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var provider = await _unitOfWork.Consignors
                .GetAsync(p => p.Id == consignorId.Value,
                    includeProperties: "Organization");

            if (provider == null)
                return NotFound("Consignor not found");

            var profile = new ConsignorProfileDto
            {
                ConsignorId = provider.Id,
                FullName = provider.DisplayName,
                Email = provider.Email,
                Phone = provider.Phone,
                CommissionRate = provider.CommissionRate,
                PreferredPaymentMethod = provider.PaymentMethod,
                PaymentDetails = provider.PaymentDetails,
                EmailNotifications = true, // TODO: Add to Consignor entity
                MemberSince = provider.CreatedAt,
                OrganizationName = provider.Organization.Name
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider profile");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ConsignorProfileDto>> UpdateProfile(
        [FromBody] UpdateConsignorProfileRequest request)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var provider = await _unitOfWork.Consignors
                .GetAsync(p => p.Id == consignorId.Value,
                    includeProperties: "Organization");

            if (provider == null)
                return NotFound("Consignor not found");

            // Update editable fields
            provider.DisplayName = request.Name;
            provider.Phone = request.Phone;
            provider.PaymentMethod = request.PreferredPaymentMethod;
            provider.PaymentDetails = request.PaymentDetails;
            // TODO: Add EmailNotifications to Consignor entity

            await _unitOfWork.Consignors.UpdateAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            var updatedProfile = new ConsignorProfileDto
            {
                ConsignorId = provider.Id,
                FullName = provider.DisplayName,
                Email = provider.Email,
                Phone = provider.Phone,
                CommissionRate = provider.CommissionRate,
                PreferredPaymentMethod = provider.PaymentMethod,
                PaymentDetails = provider.PaymentDetails,
                EmailNotifications = request.EmailNotifications,
                MemberSince = provider.CreatedAt,
                OrganizationName = provider.Organization.Name
            };

            return Ok(updatedProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider profile");
            return StatusCode(500, "Internal server error");
        }
    }

    // DROPOFF REQUESTS
    [HttpPost("dropoff-requests")]
    public async Task<ActionResult<DropoffRequestDetailDto>> CreateDropoffRequest(
        [FromBody] CreateDropoffRequestDto request)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId.Value, includeProperties: "Organization");

            if (consignor == null)
                return NotFound("Consignor not found");

            // Check if consignor is approved
            if (consignor.ApprovalStatus != "Approved")
            {
                _logger.LogWarning("[DROPOFF] Consignor {ConsignorId} attempted to create dropoff request but is not approved (Status: {ApprovalStatus})", consignorId.Value, consignor.ApprovalStatus);
                return Forbid("Your account is pending approval. Please wait for the shop owner to approve your registration before submitting items.");
            }

            // Create the dropoff request
            var dropoffRequestId = Guid.NewGuid();
            _logger.LogInformation("[DROPOFF] Creating dropoff request {DropoffRequestId} for consignor {ConsignorId} in organization {OrganizationId} with {ItemCount} items",
                dropoffRequestId, consignorId.Value, consignor.OrganizationId, request.Items.Count);

            var dropoffRequest = new DropoffRequest
            {
                Id = dropoffRequestId,
                OrganizationId = consignor.OrganizationId,
                ConsignorId = consignorId.Value,
                PlannedDate = request.PlannedDate,
                PlannedTimeSlot = request.PlannedTimeSlot,
                ConsignorMessage = request.Message,
                Status = DropoffRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dropoffRequest.SetItems(request.Items);

            await _unitOfWork.DropoffRequests.AddAsync(dropoffRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("[DROPOFF] Successfully saved dropoff request {DropoffRequestId} to database. Total value: ${TotalValue:F2}",
                dropoffRequestId, dropoffRequest.SuggestedTotal);

            // Send notification to shop owner
            _logger.LogInformation("[DROPOFF] Starting notification process for dropoff request {DropoffRequestId}", dropoffRequestId);
            await SendDropoffManifestNotificationAsync(dropoffRequest, consignor);

            var result = new DropoffRequestDetailDto
            {
                Id = dropoffRequest.Id,
                ItemCount = dropoffRequest.ItemCount,
                SuggestedTotal = dropoffRequest.SuggestedTotal,
                PlannedDate = dropoffRequest.PlannedDate,
                PlannedTimeSlot = dropoffRequest.PlannedTimeSlot,
                Status = dropoffRequest.Status,
                CreatedAt = dropoffRequest.CreatedAt,
                ImportedAt = dropoffRequest.ImportedAt,
                Items = dropoffRequest.GetItems(),
                Message = dropoffRequest.ConsignorMessage,
                Shop = new ShopSummaryDto
                {
                    Name = consignor.Organization.ShopName ?? consignor.Organization.Name,
                    Address = FormatShopAddress(consignor.Organization),
                    Phone = consignor.Organization.ShopPhone
                }
            };

            return Created($"/api/consignor/dropoff-requests/{dropoffRequest.Id}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dropoff request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("dropoff-requests")]
    public async Task<ActionResult<List<DropoffRequestListDto>>> GetMyDropoffRequests()
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var requests = await _unitOfWork.DropoffRequests
                .GetAllAsync(dr => dr.ConsignorId == consignorId.Value);

            var result = requests.OrderByDescending(dr => dr.CreatedAt)
                .Select(dr => new DropoffRequestListDto
                {
                    Id = dr.Id,
                    ItemCount = dr.ItemCount,
                    SuggestedTotal = dr.SuggestedTotal,
                    PlannedDate = dr.PlannedDate,
                    PlannedTimeSlot = dr.PlannedTimeSlot,
                    Status = dr.Status,
                    CreatedAt = dr.CreatedAt,
                    ImportedAt = dr.ImportedAt
                })
                .ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dropoff requests");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("dropoff-requests/{id}")]
    public async Task<ActionResult<DropoffRequestDetailDto>> GetDropoffRequest(Guid id)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var dropoffRequest = await _unitOfWork.DropoffRequests
                .GetAsync(dr => dr.Id == id && dr.ConsignorId == consignorId.Value,
                    includeProperties: "Organization");

            if (dropoffRequest == null)
                return NotFound("Dropoff request not found");

            var result = new DropoffRequestDetailDto
            {
                Id = dropoffRequest.Id,
                ItemCount = dropoffRequest.ItemCount,
                SuggestedTotal = dropoffRequest.SuggestedTotal,
                PlannedDate = dropoffRequest.PlannedDate,
                PlannedTimeSlot = dropoffRequest.PlannedTimeSlot,
                Status = dropoffRequest.Status,
                CreatedAt = dropoffRequest.CreatedAt,
                ImportedAt = dropoffRequest.ImportedAt,
                Items = dropoffRequest.GetItems(),
                Message = dropoffRequest.ConsignorMessage,
                Shop = new ShopSummaryDto
                {
                    Name = dropoffRequest.Organization.ShopName ?? dropoffRequest.Organization.Name,
                    Address = FormatShopAddress(dropoffRequest.Organization),
                    Phone = dropoffRequest.Organization.ShopPhone
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dropoff request detail");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("dropoff-requests/{id}")]
    public async Task<ActionResult> CancelDropoffRequest(Guid id)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var dropoffRequest = await _unitOfWork.DropoffRequests
                .GetAsync(dr => dr.Id == id && dr.ConsignorId == consignorId.Value);

            if (dropoffRequest == null)
                return NotFound("Dropoff request not found");

            // Only allow cancellation of pending requests
            if (dropoffRequest.Status != DropoffRequestStatus.Pending)
                return BadRequest("Can only cancel pending dropoff requests");

            // Update status to cancelled
            dropoffRequest.Status = DropoffRequestStatus.Cancelled;
            dropoffRequest.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DropoffRequests.UpdateAsync(dropoffRequest);
            await _unitOfWork.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling dropoff request");
            return StatusCode(500, "Internal server error");
        }
    }

    // Helper methods
    private Guid? GetCurrentConsignorId()
    {
        var consignorIdClaim = User.FindFirst("consignorId")?.Value;
        _logger.LogInformation("GetCurrentConsignorId: claim value = {ClaimValue}", consignorIdClaim ?? "[NULL]");

        if (string.IsNullOrEmpty(consignorIdClaim))
        {
            _logger.LogWarning("GetCurrentConsignorId: consignorId claim is null or empty");
            return null;
        }

        if (!Guid.TryParse(consignorIdClaim, out var consignorId))
        {
            _logger.LogWarning("GetCurrentConsignorId: Failed to parse consignorId claim '{ClaimValue}' as Guid", consignorIdClaim);
            return null;
        }

        _logger.LogInformation("GetCurrentConsignorId: Successfully parsed consignorId = {ConsignorId}", consignorId);
        return consignorId;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("GetCurrentUserId: claim value = {ClaimValue}", userIdClaim ?? "[NULL]");

        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogWarning("GetCurrentUserId: NameIdentifier claim is null or empty");
            return null;
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("GetCurrentUserId: Failed to parse NameIdentifier claim '{ClaimValue}' as Guid", userIdClaim);
            return null;
        }

        _logger.LogInformation("GetCurrentUserId: Successfully parsed userId = {UserId}", userId);
        return userId;
    }

    private string ExtractPrimaryImage(string? photosJson)
    {
        // TODO: Parse Photos JSON field and return first image URL
        // For now return placeholder
        return "";
    }

    private List<string> ExtractAllImages(string? photosJson)
    {
        // TODO: Parse Photos JSON field and return all image URLs
        // For now return empty list
        return new List<string>();
    }

    private string? FormatShopAddress(Organization organization)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(organization.ShopAddress1))
            parts.Add(organization.ShopAddress1);

        if (!string.IsNullOrWhiteSpace(organization.ShopAddress2))
            parts.Add(organization.ShopAddress2);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(organization.ShopCity))
            cityStateZip.Add(organization.ShopCity);

        if (!string.IsNullOrWhiteSpace(organization.ShopState))
            cityStateZip.Add(organization.ShopState);

        if (!string.IsNullOrWhiteSpace(organization.ShopZip))
            cityStateZip.Add(organization.ShopZip);

        if (cityStateZip.Any())
            parts.Add(string.Join(", ", cityStateZip));

        return parts.Any() ? string.Join(", ", parts) : null;
    }


    [HttpGet("agreement/status")]
    public async Task<IActionResult> GetMyAgreementStatus()
    {
        try
        {
            _logger.LogInformation("=== AGREEMENT STATUS DEBUG ===");

            var consignorIdClaim = User.FindFirst("consignorId")?.Value;
            _logger.LogInformation("ConsignorId claim from JWT: {ConsignorIdClaim}", consignorIdClaim);

            if (!Guid.TryParse(consignorIdClaim, out var consignorId))
            {
                _logger.LogWarning("Could not parse consignorId claim: {ConsignorIdClaim}", consignorIdClaim);
                return BadRequest("Consignor not found");
            }

            _logger.LogInformation("Parsed ConsignorId: {ConsignorId}", consignorId);

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId, includeProperties: "Organization");

            if (consignor == null)
            {
                _logger.LogWarning("Consignor not found in database: {ConsignorId}", consignorId);
                return NotFound("Consignor not found");
            }

            _logger.LogInformation("Found consignor: {ConsignorEmail}, AgreementStatus: {AgreementStatus}",
                consignor.Email, consignor.AgreementStatus);
            _logger.LogInformation("Organization settings - AgreementMethod: {AgreementMethod}, AcknowledgeTermsText length: {TextLength}",
                consignor.Organization.AgreementMethod,
                consignor.Organization.AcknowledgeTermsText?.Length ?? 0);

            // Determine agreement status based on organization requirements
            string agreementStatus;
            bool hasAgreement;
            bool requiresAgreement;

            if (consignor.Organization.AgreementMethod == "none")
            {
                agreementStatus = "not_required";
                hasAgreement = true; // No agreement needed, so consider it "complete"
                requiresAgreement = false;
            }
            else
            {
                requiresAgreement = true;
                // Check if consignor has completed the required agreement action
                hasAgreement = !string.IsNullOrEmpty(consignor.AgreementStatus) &&
                               consignor.AgreementStatus != "pending" &&
                               consignor.AgreementStatus != "rejected";

                if (hasAgreement)
                {
                    agreementStatus = consignor.AgreementStatus; // "uploaded", "acknowledged", etc.
                }
                else
                {
                    agreementStatus = "pending"; // Required but not completed
                }
            }

            var result = new
            {
                // Debug info
                Debug = new
                {
                    ConsignorId = consignorId,
                    ConsignorEmail = consignor.Email,
                    RawAgreementStatus = consignor.AgreementStatus,
                    OrganizationId = consignor.Organization.Id,
                    OrganizationName = consignor.Organization.Name
                },

                // Main response
                HasAgreement = hasAgreement,
                AgreementStatus = agreementStatus,
                AgreementMethod = consignor.Organization.AgreementMethod,

                // Additional context
                TermsTextConfigured = !string.IsNullOrEmpty(consignor.Organization.AcknowledgeTermsText),
                TermsTextLength = consignor.Organization.AcknowledgeTermsText?.Length ?? 0
            };

            _logger.LogInformation("Returning agreement status result: {@Result}", result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consignor agreement status");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("agreement/text")]
    public async Task<IActionResult> GetAgreementText()
    {
        try
        {
            _logger.LogInformation("=== AGREEMENT TEXT DEBUG ===");

            var consignorIdClaim = User.FindFirst("consignorId")?.Value;
            _logger.LogInformation("ConsignorId claim from JWT: {ConsignorIdClaim}", consignorIdClaim);

            if (!Guid.TryParse(consignorIdClaim, out var consignorId))
            {
                _logger.LogWarning("Could not parse consignorId claim: {ConsignorIdClaim}", consignorIdClaim);
                return BadRequest("Consignor not found");
            }

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId, includeProperties: "Organization");

            if (consignor == null)
            {
                _logger.LogWarning("Consignor not found in database: {ConsignorId}", consignorId);
                return NotFound("Consignor not found");
            }

            // Set agreement text based on method and actual file existence
            string agreementText;

            switch (consignor.Organization.AgreementMethod)
            {
                case "acknowledge":
                    agreementText = consignor.Organization.AcknowledgeTermsText ?? "No terms text configured for acknowledgment.";
                    break;

                case "upload":
                    // For upload mode, check if there's a Cloudinary URL in the dedicated field
                    var uploadCloudinaryUrl = consignor.Organization.AgreementTemplateCloudinaryUrl;
                    if (!string.IsNullOrEmpty(uploadCloudinaryUrl) && uploadCloudinaryUrl.Contains("cloudinary.com"))
                    {
                        _logger.LogInformation("[AGREEMENT_DEBUG] Bypassing existence check and extracting text directly from: {CloudinaryUrl}", uploadCloudinaryUrl);

                        // Bypass DocumentExistsAsync since it's returning false for valid files
                        // Go straight to text extraction which works for owner display
                        try
                        {
                            var templateText = await _documentService.ExtractTextFromDocumentAsync(uploadCloudinaryUrl);
                            if (!string.IsNullOrEmpty(templateText))
                            {
                                // Use AgreementGeneratorService to merge consignor data into the template
                                var personalizedText = _agreementGeneratorService.GenerateAgreement(templateText, consignor.Organization, consignor);
                                agreementText = personalizedText;
                                _logger.LogInformation("[AGREEMENT_DEBUG] Successfully extracted and personalized template text ({TemplateLength} chars -> {PersonalizedLength} chars)",
                                    templateText.Length, personalizedText.Length);
                            }
                            else
                            {
                                agreementText = "Agreement template is available for download. Click the Download button to get your personalized agreement.";
                                _logger.LogWarning("[AGREEMENT_DEBUG] Text extraction returned empty content");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[AGREEMENT_DEBUG] Error extracting text from template, falling back to download message");
                            agreementText = "Agreement template is available for download. Click the Download button to get your personalized agreement.";
                        }
                    }
                    else
                    {
                        agreementText = "No agreement template configured. Please contact the shop owner.";
                    }
                    break;

                case "none":
                    agreementText = "No agreement required.";
                    break;

                default:
                    agreementText = "Unknown agreement method configured.";
                    break;
            }
            _logger.LogInformation("Organization agreement settings - Method: {AgreementMethod}, AcknowledgeTermsText: {AcknowledgeTermsText}",
                consignor.Organization.AgreementMethod,
                string.IsNullOrEmpty(consignor.Organization.AcknowledgeTermsText) ? "[NULL/EMPTY]" : $"[{consignor.Organization.AcknowledgeTermsText.Length} chars]");

            var result = new
            {
                // Debug info
                Debug = new
                {
                    ConsignorId = consignorId,
                    OrganizationId = consignor.Organization.Id,
                    OrganizationName = consignor.Organization.Name,
                    RawAcknowledgeTermsText = consignor.Organization.AcknowledgeTermsText
                },

                // Main response
                Text = agreementText,
                AgreementMethod = consignor.Organization.AgreementMethod,

                // Additional context
                IsTextConfigured = !string.IsNullOrEmpty(consignor.Organization.AcknowledgeTermsText),
                TextLength = consignor.Organization.AcknowledgeTermsText?.Length ?? 0,

                // HTML-friendly display version
                HtmlDisplay = $"<pre>Method: {consignor.Organization.AgreementMethod}\nRequired: {(consignor.Organization.AgreementMethod != "none")}\nText: {agreementText}</pre>"
            };

            _logger.LogInformation("Returning agreement text result: {@Result}", result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agreement text");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("agreement/template")]
    public async Task<IActionResult> DownloadAgreementTemplate()
    {
        try
        {
            var consignorIdClaim = User.FindFirst("consignorId")?.Value;
            if (!Guid.TryParse(consignorIdClaim, out var consignorId))
            {
                return BadRequest("Consignor not found");
            }

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId, includeProperties: "Organization");

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            // Check if organization is in upload mode and has a Cloudinary URL
            if (consignor.Organization.AgreementMethod == "upload")
            {
                var cloudinaryUrl = consignor.Organization.AgreementTemplateCloudinaryUrl;
                if (!string.IsNullOrEmpty(cloudinaryUrl) && cloudinaryUrl.Contains("cloudinary.com"))
                {
                    // Try to extract text directly - if it works, file exists
                    // Note: Skipping DocumentExistsAsync check as it may have ResourceType mismatch
                    try
                    {
                        _logger.LogInformation("Processing agreement template for consignor {ConsignorId} from {CloudinaryUrl}", consignorId, cloudinaryUrl);

                        // Extract the template text from Cloudinary
                        var templateText = await _documentService.ExtractTextFromDocumentAsync(cloudinaryUrl);

                        if (string.IsNullOrEmpty(templateText))
                        {
                            _logger.LogWarning("Could not extract text from agreement template at {CloudinaryUrl}", cloudinaryUrl);
                            return StatusCode(500, "Could not process agreement template. Please contact support.");
                        }

                        // Use the existing AgreementGeneratorService to merge the data
                        var personalizedText = _agreementGeneratorService.GenerateAgreement(templateText, consignor.Organization, consignor);

                        // Generate PDF from the personalized text using QuestPDF
                        var pdfBytes = await _pdfReportGenerator.GenerateTextDocumentPdfAsync(personalizedText, "Consignment Agreement");
                        var orgName = consignor.Organization.ShopName ?? consignor.Organization.Name;
                        var consignorDisplayName = $"{consignor.Name} {consignor}".Trim();
                        var agreementFileName = $"{orgName.Replace(" ", "_")}_Agreement_{consignorDisplayName.Replace(" ", "_")}.pdf";

                        _logger.LogInformation("Returning personalized agreement PDF for consignor {ConsignorId}", consignorId);
                        return File(pdfBytes, "application/pdf", agreementFileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing agreement template for consignor {ConsignorId}: {CloudinaryUrl}", consignorId, cloudinaryUrl);
                        return StatusCode(500, "Could not process agreement template. Please contact support.");
                    }
                }
                else
                {
                    return NotFound("No agreement template configured for this organization");
                }
            }

            // Fallback: Generate personalized agreement from text template if available (for acknowledge mode)
            var agreementTemplate = consignor.Organization.AcknowledgeTermsText;
            if (string.IsNullOrEmpty(agreementTemplate))
            {
                return NotFound("No agreement template configured for this organization");
            }

            // Create a simple personalized agreement
            var organizationName = consignor.Organization.ShopName ?? consignor.Organization.Name;
            var consignorName = $"{consignor.Name} {consignor}".Trim();

            var personalizedContent = $@"CONSIGNMENT AGREEMENT
{organizationName}

CONSIGNOR: {consignorName}
EMAIL: {consignor.Email}
DATE: {DateTime.Now:MMMM d, yyyy}

AGREEMENT TERMS:

{agreementTemplate}

SIGNATURE SECTION:
Consignor Name: {consignorName}

Consignor Signature: _________________________________

Date: _________________________________

Shop Representative: _________________________________

Date: _________________________________

Agreement ID: AGR-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999):D5}";

            var bytes = System.Text.Encoding.UTF8.GetBytes(personalizedContent);
            var fileName = $"{organizationName.Replace(" ", "_")}_Agreement_{consignorName.Replace(" ", "_")}.txt";

            return File(bytes, "text/plain", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating agreement template");
            return StatusCode(500, "Error generating agreement template");
        }
    }


    [HttpGet("agreement/debug")]
    public async Task<IActionResult> GetAgreementDebug()
    {
        try
        {
            var consignorIdClaim = User.FindFirst("consignorId")?.Value;
            if (!Guid.TryParse(consignorIdClaim, out var consignorId))
            {
                return BadRequest("Consignor not found");
            }

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId, includeProperties: "Organization");

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            // Set agreement text based on method (same logic as text endpoint)
            string agreementText;
            switch (consignor.Organization.AgreementMethod)
            {
                case "acknowledge":
                    agreementText = consignor.Organization.AcknowledgeTermsText ?? "No terms text configured for acknowledgment.";
                    break;
                case "upload":
                    var debugUploadCloudinaryUrl = consignor.Organization.AgreementTemplateCloudinaryUrl;
                    if (!string.IsNullOrEmpty(debugUploadCloudinaryUrl) && debugUploadCloudinaryUrl.Contains("cloudinary.com"))
                    {
                        var templateExists = await _documentService.DocumentExistsAsync(debugUploadCloudinaryUrl);
                        agreementText = templateExists
                            ? "Agreement template is available for download."
                            : "Agreement template configured but file not found in storage.";
                    }
                    else
                    {
                        agreementText = "No agreement template configured.";
                    }
                    break;
                case "none":
                    agreementText = "No agreement required.";
                    break;
                default:
                    agreementText = "Unknown agreement method configured.";
                    break;
            }

            var debugHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Agreement Debug</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .debug-section {{ background: #f0f0f0; padding: 15px; margin: 10px 0; border-radius: 5px; }}
        .error {{ background: #ffe6e6; color: #cc0000; }}
        .success {{ background: #e6ffe6; color: #006600; }}
        .warning {{ background: #fff3cd; color: #856404; }}
    </style>
</head>
<body>
    <h1>Consignor Agreement Debug</h1>

    <div class=""debug-section"">
        <h2>Current Situation Analysis</h2>
        <p><strong>Problem:</strong> Angular component is empty despite working API</p>
        <p><strong>Likely Issue:</strong> Component doesn't know how to handle this data combination</p>
    </div>

    <div class=""debug-section {(consignor.AgreementStatus == "uploaded" ? "success" : "warning")}"">
        <h2>Agreement Status</h2>
        <p><strong>Status:</strong> {consignor.AgreementStatus ?? "not_required"}</p>
        <p><strong>Has Agreement:</strong> {(!string.IsNullOrEmpty(consignor.AgreementStatus) && consignor.AgreementStatus != "not_required")}</p>
        <p><strong>Organization Requires Agreement:</strong> {(consignor.Organization.AgreementMethod != "none")}</p>
    </div>

    <div class=""debug-section {(consignor.Organization.AgreementMethod == "upload" && string.IsNullOrEmpty(consignor.Organization.AcknowledgeTermsText) ? "error" : "")}"">
        <h2>Agreement Method Configuration</h2>
        <p><strong>Method:</strong> {consignor.Organization.AgreementMethod}</p>
        <p><strong>Text Configured:</strong> {!string.IsNullOrEmpty(consignor.Organization.AcknowledgeTermsText)}</p>
        <p><strong>Text Length:</strong> {consignor.Organization.AcknowledgeTermsText?.Length ?? 0} characters</p>
        {(consignor.Organization.AgreementMethod == "upload" && string.IsNullOrEmpty(consignor.Organization.AcknowledgeTermsText) ?
            "<p class=\"error\"><strong>⚠️ CONFIGURATION ISSUE:</strong> Method is 'upload' but no acknowledgment text is configured!</p>" : "")}
    </div>

    <div class=""debug-section"">
        <h2>What Angular Component Should Show</h2>
        {(consignor.AgreementStatus == "uploaded" ?
            "<p class=\"success\">✅ Agreement already uploaded - should show completion status</p>" :
        consignor.Organization.AgreementMethod == "acknowledge" ?
            "<p>📝 Should show text acknowledgment form</p>" :
        consignor.Organization.AgreementMethod == "upload" ?
            "<p>📎 Should show file upload form</p>" :
            "<p>ℹ️ No agreement required</p>")}
    </div>

    <div class=""debug-section"">
        <h2>Possible Angular Component Issues</h2>
        <ul>
            <li>Component expects different property names (PascalCase vs camelCase)</li>
            <li>Component has logic that doesn't handle 'uploaded' status properly</li>
            <li>Component expects both agreementMethod='acknowledge' AND text to be present</li>
            <li>Component is waiting for user interaction that never happens</li>
            <li>Component has conditional rendering that's never triggered</li>
        </ul>
    </div>

    <div class=""debug-section"">
        <h2>Raw Data</h2>
        <pre>{System.Text.Json.JsonSerializer.Serialize(new {
            consignor = new {
                Id = consignor.Id,
                Email = consignor.Email,
                AgreementStatus = consignor.AgreementStatus
            },
            organization = new {
                Id = consignor.Organization.Id,
                Name = consignor.Organization.Name,
                AgreementMethod = consignor.Organization.AgreementMethod,
                AcknowledgeTermsText = consignor.Organization.AcknowledgeTermsText
            }
        }, new JsonSerializerOptions { WriteIndented = true })}</pre>
    </div>
</body>
</html>";

            return Content(debugHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agreement debug");
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<User?> GetOwnerByOrganizationIdAsync(Guid organizationId)
    {
        return await _unitOfWork.Users.GetAsync(u => u.OrganizationId == organizationId && u.Role == UserRole.Owner);
    }

    private async Task SendDropoffManifestNotificationAsync(DropoffRequest dropoffRequest, Consignor consignor)
    {
        try
        {
            _logger.LogInformation("[NOTIFICATION] Starting dropoff manifest notification for request {DropoffRequestId} in organization {OrganizationId}",
                dropoffRequest.Id, dropoffRequest.OrganizationId);

            // Get the shop owner for this organization
            var owner = await GetOwnerByOrganizationIdAsync(dropoffRequest.OrganizationId);
            if (owner == null)
            {
                _logger.LogWarning("[NOTIFICATION] No owner found for organization {OrganizationId} to notify about dropoff request {DropoffRequestId}",
                    dropoffRequest.OrganizationId, dropoffRequest.Id);
                return;
            }

            _logger.LogInformation("[NOTIFICATION] Found owner {OwnerId} ({OwnerEmail}) for organization {OrganizationId}",
                owner.Id, owner.Email, dropoffRequest.OrganizationId);

            var itemCount = dropoffRequest.ItemCount;
            var consignorName = !string.IsNullOrEmpty(consignor.DisplayName) ? consignor.DisplayName : consignor.Email;
            var totalValue = dropoffRequest.SuggestedTotal;
            var notificationType = NotificationType.DropoffManifest.ToStringValue();

            _logger.LogInformation("[NOTIFICATION] Creating notification: Type={NotificationType}, ItemCount={ItemCount}, TotalValue=${TotalValue:F2}, ConsignorName={ConsignorName}",
                notificationType, itemCount, totalValue, consignorName);

            var notification = new CreateNotificationRequest
            {
                OrganizationId = dropoffRequest.OrganizationId,
                FromUserId = consignor.UserId,
                FromType = "consignor",
                ToUserId = owner.Id,
                ToType = "owner",
                Type = notificationType,
                Title = "New Item Manifest Submitted",
                Message = $"{consignorName} submitted a manifest with {itemCount} items (${totalValue:F2} total value)",
                ActionUrl = $"/owner/dropoff-requests/{dropoffRequest.Id}",
                ReferenceType = "DropoffRequest",
                ReferenceId = dropoffRequest.Id,
                Metadata = new NotificationMetadata
                {
                    ActionButtonText = "Review Import"
                },
                Payload = new
                {
                    dropoffRequestId = dropoffRequest.Id,
                    consignorId = consignor.Id,
                    consignorName = consignorName,
                    itemCount = itemCount,
                    totalValue = totalValue,
                    plannedDate = dropoffRequest.PlannedDate,
                    plannedTimeSlot = dropoffRequest.PlannedTimeSlot
                }
            };

            _logger.LogInformation("[NOTIFICATION] Calling notification service to create notification for user {UserId} with type '{NotificationType}'",
                owner.Id, notificationType);

            await _notificationService.CreateAsync(notification);

            _logger.LogInformation("[NOTIFICATION] ✅ Successfully created dropoff manifest notification for request {DropoffRequestId} to owner {UserId} ({OwnerEmail})",
                dropoffRequest.Id, owner.Id, owner.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION] ❌ Failed to send dropoff manifest notification for request {DropoffRequestId}", dropoffRequest.Id);
            // Don't throw - notification failure shouldn't break the dropoff creation
        }
    }

    [HttpGet("agreement/signed")]
    public async Task<IActionResult> DownloadMySignedAgreement()
    {
        try
        {
            var consignorIdClaim = User.FindFirst("consignorId")?.Value;
            if (!Guid.TryParse(consignorIdClaim, out var consignorId))
            {
                return BadRequest("Consignor not found");
            }

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId, includeProperties: "Organization");

            if (consignor == null)
            {
                return NotFound("Consignor not found");
            }

            if (string.IsNullOrEmpty(consignor.SignedAgreementUrl))
            {
                return NotFound("No signed agreement on file. Please upload your signed agreement first.");
            }

            // Return redirect to the stored file URL (assumes file is publicly accessible)
            // Alternative: proxy the file through this endpoint if needed
            return Redirect(consignor.SignedAgreementUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading signed agreement for consignor {ConsignorId}", GetCurrentConsignorId());
            return StatusCode(500, "Error downloading signed agreement");
        }
    }

    [HttpPost("agreement/upload")]
    public async Task<IActionResult> UploadSignedAgreement([FromForm] IFormFile? signedAgreement)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            // Debug: Log all form files received
            _logger.LogInformation("Agreement upload attempt - Form files count: {Count}", Request.Form.Files.Count);
            foreach (var formFile in Request.Form.Files)
            {
                _logger.LogInformation("Form file: Name={Name}, FileName={FileName}, Length={Length}",
                    formFile.Name, formFile.FileName, formFile.Length);
            }

            // Try to get the file from any of these parameter names
            var file = signedAgreement ??
                      Request.Form.Files.GetFile("signedAgreement") ??
                      Request.Form.Files.GetFile("file") ??
                      Request.Form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new {
                    error = "No file provided",
                    expectedParameter = "signedAgreement",
                    receivedFiles = Request.Form.Files.Select(f => new { name = f.Name, fileName = f.FileName }).ToArray(),
                    debug = "Make sure to send file with parameter name 'signedAgreement'"
                });
            }

            // Validate file type and size
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Invalid file type. Only PDF, DOC, DOCX, JPG, and PNG files are supported.");
            }

            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (file.Length > maxFileSize)
            {
                return BadRequest("File size exceeds 50MB limit");
            }

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId.Value, includeProperties: "Organization");

            if (consignor == null)
                return NotFound("Consignor not found");

            _logger.LogInformation("Uploading signed agreement for consignor {ConsignorId}, file: {FileName} ({FileSize} bytes)",
                consignorId, file.FileName, file.Length);

            // Upload to document storage
            using var fileStream = file.OpenReadStream();
            var documentUrl = await _documentService.UploadSignedAgreementAsync(
                consignor.OrganizationId,
                consignorId.Value,
                fileStream,
                file.FileName);

            // Update consignor record
            consignor.SignedAgreementUrl = documentUrl;
            consignor.SignedAgreementUploadedAt = DateTime.UtcNow;
            consignor.AgreementStatus = "uploaded";

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully uploaded signed agreement for consignor {ConsignorId}: {DocumentUrl}",
                consignorId, documentUrl);

            return Ok(new {
                success = true,
                message = "Signed agreement uploaded successfully",
                data = new {
                    uploadedAt = consignor.SignedAgreementUploadedAt,
                    fileName = file.FileName,
                    status = consignor.AgreementStatus
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading signed agreement for consignor {ConsignorId}", GetCurrentConsignorId());
            return StatusCode(500, "Error uploading signed agreement. Please try again.");
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<ItemCategoryDto>>> GetCategories()
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId.Value, includeProperties: "Organization");

            if (consignor == null)
                return NotFound("Consignor not found");

            // Get active categories for this organization
            var categories = await _unitOfWork.ItemCategories.GetAllAsync(
                c => c.OrganizationId == consignor.OrganizationId && c.IsActive,
                includeProperties: "SubCategories");

            var categoryDtos = categories.Select(c => new ItemCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                IsActive = c.IsActive,
                ParentCategoryId = c.ParentCategoryId,
                SortOrder = c.SortOrder,
                DefaultCommissionRate = c.DefaultCommissionRate,
                SubCategoryCount = c.SubCategories?.Count(sc => sc.IsActive) ?? 0,
                CreatedAt = c.CreatedAt
            }).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();

            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consignor categories");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("photos/upload")]
    public async Task<ActionResult<object>> UploadPhoto(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            var consignor = await _unitOfWork.Consignors
                .GetAsync(c => c.Id == consignorId.Value, includeProperties: "Organization");

            if (consignor == null)
                return NotFound("Consignor not found");

            // Use a temporary item ID for the upload (since this is for dropoff request, not an existing item)
            var tempItemId = Guid.NewGuid();

            var photoUrl = await _photoService.UploadPhotoAsync(consignor.OrganizationId, tempItemId, file.OpenReadStream(), file.FileName);

            // Extract public ID from Cloudinary URL for frontend use
            var publicId = ExtractPublicIdFromUrl(photoUrl);

            return Ok(new { url = photoUrl, publicId = publicId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for consignor {ConsignorId}", GetCurrentConsignorId());
            return StatusCode(500, "Error uploading photo. Please try again.");
        }
    }

    [HttpPatch("items/{itemId}")]
    public async Task<ActionResult<object>> UpdateMyItem(Guid itemId, [FromBody] UpdateConsignorItemRequest request)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
                return BadRequest("Consignor not found");

            // Get the item and verify it belongs to this consignor
            var item = await _unitOfWork.Items.GetByIdAsync(itemId);

            if (item == null || item.ConsignorId != consignorId.Value)
                return NotFound("Item not found or you don't have permission to edit it");

            // Check if consignor has permission to edit items
            var consignor = await _unitOfWork.Consignors.GetByIdAsync(consignorId.Value);

            // Check if consignor has permission to edit items - for now, allow if consignor exists
            // TODO: Implement proper permission checking once ConsignorPermissions structure is clarified
            if (consignor?.Organization == null)
                return Forbid("You don't have permission to edit items");

            // Update only the fields consignors are allowed to edit
            if (!string.IsNullOrWhiteSpace(request.Title))
                item.Title = request.Title.Trim();

            if (request.Description != null)
                item.Description = request.Description.Trim();

            if (request.Notes != null)
                item.Notes = request.Notes.Trim();

            item.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return Ok(new { success = true, message = "Item updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId} for consignor {ConsignorId}", itemId, GetCurrentConsignorId());
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("notifications/activated-items/{notificationId}")]
    public async Task<ActionResult<object>> GetActivatedItemsForNotification(Guid notificationId)
    {
        try
        {
            var consignorId = GetCurrentConsignorId();
            if (consignorId == null)
            {
                _logger.LogWarning("GetActivatedItemsForNotification: consignorId is null");
                return BadRequest("Consignor not found");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                _logger.LogWarning("GetActivatedItemsForNotification: currentUserId is null");
                return BadRequest("User not found");
            }

            _logger.LogInformation("GetActivatedItemsForNotification: notificationId={NotificationId}, consignorId={ConsignorId}, userId={UserId}",
                notificationId, consignorId.Value, currentUserId.Value);

            // Get the notification and ensure it belongs to this user
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);

            if (notification == null)
            {
                _logger.LogWarning("GetActivatedItemsForNotification: notification not found for id {NotificationId}", notificationId);
                return NotFound("Notification not found");
            }

            // Check if notification belongs to current user by comparing ToUserId with current user ID
            if (notification.ToUserId != currentUserId.Value)
            {
                _logger.LogWarning("GetActivatedItemsForNotification: notification {NotificationId} belongs to user {ToUserId}, but current user is {CurrentUserId}",
                    notificationId, notification.ToUserId, currentUserId.Value);
                return NotFound("Notification not found");
            }

            _logger.LogInformation("GetActivatedItemsForNotification: notification found and authorized, ToType={ToType}, Type={Type}",
                notification.ToType, notification.Type);

            // Parse the payload to get ItemIds
            List<Guid> itemIds = new List<Guid>();
            if (!string.IsNullOrEmpty(notification.Payload))
            {
                _logger.LogInformation("GetActivatedItemsForNotification: parsing payload: {Payload}", notification.Payload);
                try
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Payload);
                    _logger.LogInformation("GetActivatedItemsForNotification: payload deserialized, keys: {Keys}", string.Join(", ", payload.Keys));

                    if (payload.ContainsKey("ItemIds"))
                    {
                        var itemIdsElement = payload["ItemIds"] as JsonElement?;
                        if (itemIdsElement.HasValue && itemIdsElement.Value.ValueKind == JsonValueKind.Array)
                        {
                            itemIds = itemIdsElement.Value.EnumerateArray()
                                .Select(id => Guid.Parse(id.GetString()))
                                .ToList();
                            _logger.LogInformation("GetActivatedItemsForNotification: parsed {Count} item IDs: {ItemIds}",
                                itemIds.Count, string.Join(", ", itemIds));
                        }
                        else
                        {
                            _logger.LogWarning("GetActivatedItemsForNotification: ItemIds element is not a valid array, ValueKind: {ValueKind}",
                                itemIdsElement?.ValueKind);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("GetActivatedItemsForNotification: payload does not contain 'ItemIds' key");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse notification payload for notification {NotificationId}, payload: {Payload}",
                        notificationId, notification.Payload);
                }
            }
            else
            {
                _logger.LogWarning("GetActivatedItemsForNotification: notification payload is null or empty");
            }

            // Get the item details
            _logger.LogInformation("GetActivatedItemsForNotification: retrieving items for consignor {ConsignorId}", consignorId.Value);
            var allItems = await _unitOfWork.Items.GetAllAsync();
            _logger.LogInformation("GetActivatedItemsForNotification: found {TotalItemCount} total items in database", allItems.Count());

            var items = allItems
                .Where(i => itemIds.Contains(i.Id) && i.ConsignorId == consignorId.Value)
                .Select(i => new
                {
                    id = i.Id.ToString(),
                    title = i.Title,
                    price = i.Price,
                    thumbnailUrl = i.PrimaryImageUrl,
                    status = i.Status.ToString(),
                    activatedAt = i.StatusChangedAt ?? i.CreatedAt
                })
                .ToList();

            _logger.LogInformation("GetActivatedItemsForNotification: returning {ItemCount} items for consignor {ConsignorId}. Items: {Items}",
                items.Count, consignorId.Value, string.Join(", ", items.Select(i => $"{i.id}:{i.title}")));

            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activated items for notification {NotificationId}", notificationId);
            return StatusCode(500, "Internal server error");
        }
    }

    private string ExtractPublicIdFromUrl(string cloudinaryUrl)
    {
        try
        {
            var uri = new Uri(cloudinaryUrl);
            var pathParts = uri.AbsolutePath.Split('/');

            // Find the upload index and extract everything after version
            var uploadIndex = Array.IndexOf(pathParts, "upload");
            if (uploadIndex == -1 || uploadIndex + 2 >= pathParts.Length)
                return Path.GetFileNameWithoutExtension(cloudinaryUrl);

            // Skip version and get public ID parts
            var publicIdParts = pathParts.Skip(uploadIndex + 2);
            var publicId = string.Join("/", publicIdParts);

            // Remove file extension
            if (publicId.Contains('.'))
                publicId = publicId.Substring(0, publicId.LastIndexOf('.'));

            return publicId;
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(cloudinaryUrl);
        }
    }

    // Balance & Payout endpoints - TODO: Implement these methods
    [HttpGet("balance")]
    public async Task<IActionResult> GetConsignorBalance()
    {
        throw new NotImplementedException("Consignor balance endpoint not yet implemented");
    }

    [HttpPost("payouts/{payoutId}/confirm-received")]
    public async Task<IActionResult> ConfirmPayoutReceived(string payoutId)
    {
        throw new NotImplementedException("Confirm payout received endpoint not yet implemented");
    }

    [HttpPost("payout-requests")]
    public async Task<IActionResult> RequestPayout([FromBody] object request)
    {
        throw new NotImplementedException("Payout request endpoint not yet implemented");
    }

    [HttpGet("payout-requests/status")]
    public async Task<IActionResult> GetPayoutRequestStatus()
    {
        throw new NotImplementedException("Payout request status endpoint not yet implemented");
    }

    [HttpDelete("payout-requests/{requestId}")]
    public async Task<IActionResult> CancelPayoutRequest(string requestId)
    {
        throw new NotImplementedException("Cancel payout request endpoint not yet implemented");
    }

    // Statement endpoints - TODO: Implement these methods
    [HttpGet("statements/{statementId}")]
    public async Task<IActionResult> GetStatement(string statementId)
    {
        throw new NotImplementedException("Get statement endpoint not yet implemented");
    }

    [HttpGet("statements/{year:int}/{month:int}")]
    public async Task<IActionResult> GetStatementByPeriod(int year, int month)
    {
        throw new NotImplementedException("Get statement by period endpoint not yet implemented");
    }

    [HttpGet("statements/{statementId}/pdf")]
    public async Task<IActionResult> DownloadStatementPdfById(string statementId)
    {
        throw new NotImplementedException("Download statement PDF by ID endpoint not yet implemented");
    }

    [HttpPost("statements/{statementId}/regenerate")]
    public async Task<IActionResult> RegenerateStatement(string statementId)
    {
        throw new NotImplementedException("Regenerate statement endpoint not yet implemented");
    }

}