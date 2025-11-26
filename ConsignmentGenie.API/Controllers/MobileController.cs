using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/mobile")]
[Authorize]
public class MobileController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MobileController> _logger;

    public MobileController(IUnitOfWork unitOfWork, ILogger<MobileController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetMobileDashboard()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            if (userRole == "Provider")
            {
                return await GetProviderMobileDashboard();
            }
            else
            {
                return await GetShopOwnerMobileDashboard(Guid.Parse(organizationId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mobile dashboard");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("quick-sale")]
    public async Task<IActionResult> GetQuickSaleData()
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            // Get recent items for quick access
            var recentItems = await _unitOfWork.Items
                .GetAllAsync(i => i.OrganizationId == Guid.Parse(organizationId) &&
                               i.Status == ItemStatus.Available,
                    includeProperties: "Provider");

            var quickSaleData = recentItems
                .OrderByDescending(i => i.UpdatedAt)
                .Take(10)
                .Select(item => new
                {
                    id = item.Id,
                    name = item.Title,
                    price = item.Price,
                    provider = item.Provider.DisplayName,
                    barcode = item.Sku
                })
                .ToList();

            return Ok(new { success = true, data = quickSaleData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick sale data");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("quick-sale")]
    public async Task<IActionResult> ProcessQuickSale([FromBody] QuickSaleRequest request)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var item = await _unitOfWork.Items
                .GetAsync(i => i.Id == request.ItemId && i.OrganizationId == Guid.Parse(organizationId),
                    includeProperties: "Provider");

            if (item == null || item.Status != ItemStatus.Available)
                return BadRequest("Item not available for sale");

            // Create transaction
            var transaction = new ConsignmentGenie.Core.Entities.Transaction
            {
                OrganizationId = Guid.Parse(organizationId),
                ItemId = item.Id,
                ProviderId = item.ProviderId,
                SalePrice = item.Price,
                ShopAmount = item.Price * ((100 - (item.Provider.DefaultSplitPercentage ?? item.Provider.CommissionRate)) / 100),
                ProviderAmount = item.Price * ((item.Provider.DefaultSplitPercentage ?? item.Provider.CommissionRate) / 100),
                SaleDate = DateTime.UtcNow,
                PaymentMethod = request.PaymentMethod ?? "Cash"
            };

            await _unitOfWork.Transactions.AddAsync(transaction);

            // Update item status
            item.Status = ItemStatus.Sold;
            await _unitOfWork.Items.UpdateAsync(item);

            await _unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Sale processed successfully",
                data = new
                {
                    transactionId = transaction.Id,
                    itemName = item.Title,
                    saleAmount = item.Price,
                    providerAmount = transaction.ProviderAmount,
                    shopOwnerAmount = transaction.ShopAmount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing quick sale");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("barcode-lookup/{barcode}")]
    public async Task<IActionResult> BarcodeItemLookup(string barcode)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var item = await _unitOfWork.Items
                .GetAsync(i => i.Sku == barcode &&
                             i.OrganizationId == Guid.Parse(organizationId) &&
                             i.Status == ItemStatus.Available,
                    includeProperties: "Provider,Photos");

            if (item == null)
                return NotFound("Item not found or not available");

            var itemData = new
            {
                id = item.Id,
                name = item.Title,
                price = item.Price,
                description = item.Description,
                provider = new
                {
                    id = item.Provider.Id,
                    name = item.Provider.DisplayName,
                    splitPercentage = item.Provider.DefaultSplitPercentage
                },
                photos = new List<string>(), // TODO: Parse Photos JSON
                category = item.Category,
                dateAdded = item.CreatedAt
            };

            return Ok(new { success = true, data = itemData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up item by barcode");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("recent-sales")]
    public async Task<IActionResult> GetRecentSales([FromQuery] int limit = 10)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var recentSales = await _unitOfWork.Transactions
                .GetAllAsync(t => t.OrganizationId == Guid.Parse(organizationId),
                    includeProperties: "Item,Provider");

            var salesData = recentSales
                .OrderByDescending(t => t.SaleDate)
                .Take(limit)
                .Select(sale => new
                {
                    id = sale.Id,
                    itemName = sale.Item.Title,
                    provider = sale.Provider.DisplayName,
                    amount = sale.SalePrice,
                    date = sale.SaleDate,
                    paymentMethod = sale.PaymentMethod
                })
                .ToList();

            return Ok(new { success = true, data = salesData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent sales");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncOfflineData([FromBody] OfflineSyncRequest request)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Organization not found");

            var processedTransactions = new List<Guid>();
            var errors = new List<string>();

            foreach (var saleData in request.OfflineSales)
            {
                try
                {
                    var item = await _unitOfWork.Items
                        .GetByIdAsync(saleData.ItemId);

                    if (item?.Status != ItemStatus.Available)
                    {
                        errors.Add($"Item {saleData.ItemId} not available");
                        continue;
                    }

                    var transaction = new ConsignmentGenie.Core.Entities.Transaction
                    {
                        OrganizationId = Guid.Parse(organizationId),
                        ItemId = saleData.ItemId,
                        ProviderId = item.ProviderId,
                        SalePrice = saleData.Amount,
                        ShopAmount = saleData.ShopAmount,
                        ProviderAmount = saleData.ProviderAmount,
                        SaleDate = saleData.SaleDate,
                        PaymentMethod = saleData.PaymentMethod
                    };

                    await _unitOfWork.Transactions.AddAsync(transaction);

                    item.Status = ItemStatus.Sold;
                    await _unitOfWork.Items.UpdateAsync(item);

                    processedTransactions.Add(transaction.Id);
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing sale {saleData.ItemId}: {ex.Message}");
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Processed {processedTransactions.Count} sales",
                data = new
                {
                    processedCount = processedTransactions.Count,
                    processedTransactions,
                    errors
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing offline data");
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<IActionResult> GetShopOwnerMobileDashboard(Guid organizationId)
    {
        var today = DateTime.Today;
        var thisWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

        var todaySales = await _unitOfWork.Transactions
            .CountAsync(t => t.OrganizationId == organizationId && t.SaleDate.Date == today);

        var todayRevenue = (await _unitOfWork.Transactions
            .GetAllAsync(t => t.OrganizationId == organizationId && t.SaleDate.Date == today))
            .Sum(t => t.SalePrice);

        var activeItems = await _unitOfWork.Items
            .CountAsync(i => i.OrganizationId == organizationId && i.Status == ItemStatus.Available);

        var lowStockProviders = await _unitOfWork.Providers
            .GetAllAsync(p => p.OrganizationId == organizationId, includeProperties: "Items");

        var dashboard = new
        {
            todaySales,
            todayRevenue,
            activeItems,
            lowStockAlerts = lowStockProviders
                .Where(p => p.Items.Count(i => i.Status == ItemStatus.Available) < 5)
                .Count(),
            quickActions = new[]
            {
                new { action = "quick_sale", label = "Quick Sale", icon = "shopping_cart" },
                new { action = "add_item", label = "Add Item", icon = "add" },
                new { action = "scan_barcode", label = "Scan Barcode", icon = "barcode_scanner" },
                new { action = "view_analytics", label = "Analytics", icon = "analytics" }
            }
        };

        return Ok(new { success = true, data = dashboard });
    }

    private async Task<IActionResult> GetProviderMobileDashboard()
    {
        var providerId = User.FindFirst("ProviderId")?.Value;
        if (string.IsNullOrEmpty(providerId))
            return BadRequest("Provider not found");

        var provider = await _unitOfWork.Providers
            .GetAsync(p => p.Id == Guid.Parse(providerId), includeProperties: "Items,Payouts");

        if (provider == null)
            return NotFound("Provider not found");

        var activeItems = provider.Items.Count(i => i.Status == ItemStatus.Available);
        var soldThisMonth = provider.Items.Count(i => i.Status == ItemStatus.Sold &&
                                                    i.UpdatedAt.HasValue && i.UpdatedAt.Value.Month == DateTime.Now.Month);

        var pendingPayout = provider.Payouts
            .Where(p => !p.PaidAt.HasValue)
            .Sum(p => p.TotalAmount);

        var dashboard = new
        {
            providerName = provider.DisplayName,
            activeItems,
            soldThisMonth,
            pendingPayout,
            commissionRate = provider.DefaultSplitPercentage,
            recentActivity = provider.Items
                .Where(i => i.Status == ItemStatus.Sold)
                .OrderByDescending(i => i.UpdatedAt)
                .Take(3)
                .Select(i => new
                {
                    itemName = i.Title,
                    saleDate = i.UpdatedAt,
                    amount = i.Price * (provider.DefaultSplitPercentage / 100)
                })
                .ToList()
        };

        return Ok(new { success = true, data = dashboard });
    }
}

public class QuickSaleRequest
{
    public Guid ItemId { get; set; }
    public string? PaymentMethod { get; set; }
}

public class OfflineSyncRequest
{
    public List<OfflineSaleData> OfflineSales { get; set; } = new();
}

public class OfflineSaleData
{
    public Guid ItemId { get; set; }
    public decimal Amount { get; set; }
    public decimal ShopAmount { get; set; }
    public decimal ProviderAmount { get; set; }
    public DateTime SaleDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
}