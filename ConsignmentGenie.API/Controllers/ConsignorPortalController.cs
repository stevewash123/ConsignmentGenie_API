using ConsignmentGenie.Application.DTOs.Consignor;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/provider")]
[Authorize(Roles = "Consignor")]
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

    // DASHBOARD
    [HttpGet("dashboard")]
    public async Task<ActionResult<ProviderDashboardDto>> GetDashboard()
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var provider = await _unitOfWork.Consignors
                .GetAsync(p => p.Id == providerId.Value,
                    includeProperties: "Items,Payouts,Transactions,Organization");

            if (provider == null)
                return NotFound("Consignor not found");

            // Calculate dashboard metrics
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);

            var dashboard = new ProviderDashboardDto
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
                    .Where(t => t.PayoutId == null)
                    .Sum(t => t.ConsignorAmount),
                TotalEarningsAllTime = provider.Transactions
                    .Sum(t => t.ConsignorAmount),
                EarningsThisMonth = provider.Transactions
                    .Where(t => t.SaleDate >= thisMonthStart)
                    .Sum(t => t.ConsignorAmount),

                // Recent activity - get last 5 sales
                RecentSales = provider.Transactions
                    .OrderByDescending(t => t.SaleDate)
                    .Take(5)
                    .Select(t => new ProviderSaleDto
                    {
                        TransactionId = t.Id,
                        SaleDate = t.SaleDate,
                        ItemTitle = t.Item?.Title ?? "Unknown Item",
                        ItemSku = t.Item?.Sku ?? "",
                        SalePrice = t.SalePrice,
                        MyEarnings = t.ConsignorAmount,
                        PayoutStatus = t.PayoutId != null ? "Paid" : "Pending"
                    }).ToList(),

                // Last payout
                LastPayout = provider.Payouts
                    .Where(p => p.PaidAt.HasValue)
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => new ProviderPayoutDto
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

    // MY ITEMS
    [HttpGet("items")]
    public async Task<ActionResult<PagedResult<ProviderItemDto>>> GetMyItems(
        [FromQuery] ProviderItemQueryParams queryParams)
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var itemsQuery = await _unitOfWork.Items
                .GetAllAsync(i => i.ConsignorId == providerId.Value,
                    includeProperties: "Consignor,Transactions");
            var items = itemsQuery.ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                if (Enum.TryParse<ItemStatus>(queryParams.Status, out var status))
                    items = items.Where(i => i.Status == status).ToList();
            }

            if (!string.IsNullOrEmpty(queryParams.Category))
                items = items.Where(i => i.Category == queryParams.Category).ToList();

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
                .Select(item => new ProviderItemDto
                {
                    ItemId = item.Id,
                    Sku = item.Sku,
                    Title = item.Title,
                    PrimaryImageUrl = ExtractPrimaryImage(item.Photos),
                    Price = item.Price,
                    MyEarnings = item.Price * (item.Consignor.CommissionRate / 100),
                    Category = item.Category,
                    Status = item.Status.ToString(),
                    ReceivedDate = item.CreatedAt,
                    SoldDate = item.Status == ItemStatus.Sold ? (DateTime?)item.Transaction.SaleDate : null,
                    SalePrice = item.Status == ItemStatus.Sold ? (decimal?)item.Transaction.SalePrice : null
                })
                .ToList();

            var result = new PagedResult<ProviderItemDto>
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
    public async Task<ActionResult<ProviderItemDetailDto>> GetMyItem(Guid id)
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var item = await _unitOfWork.Items
                .GetAsync(i => i.Id == id && i.ConsignorId == providerId.Value,
                    includeProperties: "Consignor,Transactions");

            if (item == null)
                return NotFound("Item not found");

            var itemDetail = new ProviderItemDetailDto
            {
                ItemId = item.Id,
                Sku = item.Sku,
                Title = item.Title,
                Description = item.Description,
                PrimaryImageUrl = ExtractPrimaryImage(item.Photos),
                ImageUrls = ExtractAllImages(item.Photos),
                Price = item.Price,
                MyEarnings = item.Price * (item.Consignor.CommissionRate / 100),
                Category = item.Category,
                Status = item.Status.ToString(),
                ReceivedDate = item.CreatedAt,
                SoldDate = item.Status == ItemStatus.Sold ? item.Transaction.SaleDate : null,
                SalePrice = item.Status == ItemStatus.Sold ? item.Transaction.SalePrice : null,
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
    public async Task<ActionResult<PagedResult<ProviderSaleDto>>> GetMySales(
        [FromQuery] ProviderSaleQueryParams queryParams)
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var transactionsQuery = await _unitOfWork.Transactions
                .GetAllAsync(t => t.ConsignorId == providerId.Value,
                    includeProperties: "Item,Payouts");
            var transactions = transactionsQuery.ToList();

            // Apply filters
            if (queryParams.DateFrom.HasValue)
                transactions = transactions.Where(t => t.SaleDate >= queryParams.DateFrom.Value).ToList();

            if (queryParams.DateTo.HasValue)
                transactions = transactions.Where(t => t.SaleDate <= queryParams.DateTo.Value).ToList();

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
                .OrderByDescending(t => t.SaleDate)
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(t => new ProviderSaleDto
                {
                    TransactionId = t.Id,
                    SaleDate = t.SaleDate,
                    ItemTitle = t.Item?.Title ?? "Unknown Item",
                    ItemSku = t.Item?.Sku ?? "",
                    SalePrice = t.SalePrice,
                    MyEarnings = t.ConsignorAmount,
                    PayoutStatus = t.PayoutId != null ? "Paid" : "Pending"
                })
                .ToList();

            var result = new PagedResult<ProviderSaleDto>
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
    public async Task<ActionResult<PagedResult<ProviderPayoutDto>>> GetMyPayouts()
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var payouts = await _unitOfWork.Payouts
                .GetAllAsync(p => p.ConsignorId == providerId.Value,
                    includeProperties: "Consignor,Transactions");

            var payoutDtos = payouts
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProviderPayoutDto
                {
                    PayoutId = p.Id,
                    PayoutNumber = p.PayoutNumber,
                    PayoutDate = p.PaidAt ?? p.CreatedAt,
                    Amount = p.TotalAmount,
                    PaymentMethod = p.Consignor.PaymentMethod ?? "Not Set",
                    ItemCount = p.Transactions.Count()
                })
                .ToList();

            var result = new PagedResult<ProviderPayoutDto>
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
    public async Task<ActionResult<ProviderPayoutDetailDto>> GetMyPayout(Guid id)
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var payout = await _unitOfWork.Payouts
                .GetAsync(p => p.Id == id && p.ConsignorId == providerId.Value,
                    includeProperties: "Consignor,Transactions.Item");

            if (payout == null)
                return NotFound("Payout not found");

            var payoutDetail = new ProviderPayoutDetailDto
            {
                PayoutId = payout.Id,
                PayoutNumber = payout.PayoutNumber,
                PayoutDate = payout.PaidAt ?? payout.CreatedAt,
                Amount = payout.TotalAmount,
                PaymentMethod = payout.Consignor.PaymentMethod ?? "Not Set",
                ItemCount = payout.Transactions.Count(),
                PaymentReference = payout.PaymentReference ?? "",
                PeriodStart = payout.PeriodStart,
                PeriodEnd = payout.PeriodEnd,
                Items = payout.Transactions
                    .Select(t => new ProviderSaleDto
                    {
                        TransactionId = t.Id,
                        SaleDate = t.SaleDate,
                        ItemTitle = t.Item?.Title ?? "Unknown Item",
                        ItemSku = t.Item?.Sku ?? "",
                        SalePrice = t.SalePrice,
                        MyEarnings = t.ConsignorAmount,
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
    public async Task<ActionResult<ProviderProfileDto>> GetProfile()
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var provider = await _unitOfWork.Consignors
                .GetAsync(p => p.Id == providerId.Value,
                    includeProperties: "Organization");

            if (provider == null)
                return NotFound("Consignor not found");

            var profile = new ProviderProfileDto
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
    public async Task<ActionResult<ProviderProfileDto>> UpdateProfile(
        [FromBody] UpdateProviderProfileRequest request)
    {
        try
        {
            var providerId = GetCurrentProviderId();
            if (providerId == null)
                return BadRequest("Consignor not found");

            var provider = await _unitOfWork.Consignors
                .GetAsync(p => p.Id == providerId.Value,
                    includeProperties: "Organization");

            if (provider == null)
                return NotFound("Consignor not found");

            // Update editable fields
            provider.DisplayName = request.FullName;
            provider.Phone = request.Phone;
            provider.PaymentMethod = request.PreferredPaymentMethod;
            provider.PaymentDetails = request.PaymentDetails;
            // TODO: Add EmailNotifications to Consignor entity

            await _unitOfWork.Consignors.UpdateAsync(provider);
            await _unitOfWork.SaveChangesAsync();

            var updatedProfile = new ProviderProfileDto
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

    // Helper methods
    private Guid? GetCurrentProviderId()
    {
        var providerIdClaim = User.FindFirst("ConsignorId")?.Value;
        if (string.IsNullOrEmpty(providerIdClaim) || !Guid.TryParse(providerIdClaim, out var providerId))
            return null;
        return providerId;
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
}