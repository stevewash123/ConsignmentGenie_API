using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Interfaces;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner")]
public class InventoryController : ControllerBase
{
    private readonly PendingImportAssignmentService _assignmentService;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryController(PendingImportAssignmentService assignmentService, IUnitOfWork unitOfWork)
    {
        _assignmentService = assignmentService;
        _unitOfWork = unitOfWork;
    }
    [HttpGet("summary")]
    public IActionResult GetInventorySummary()
    {
        // Stubbed inventory summary
        var summary = new
        {
            totalValue = 42750.80m,
            totalItems = 342,
            itemsOnFloor = 320,
            itemsInStorage = 22,
            categories = new[]
            {
                new { name = "Clothing", count = 145, value = 18250.50m },
                new { name = "Accessories", count = 87, value = 12340.25m },
                new { name = "Jewelry", count = 65, value = 8950.75m },
                new { name = "Home Decor", count = 45, value = 3209.30m }
            },
            recentActivity = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    action = "added",
                    itemName = "Vintage Leather Jacket",
                    provider = "Sarah Johnson",
                    value = 125.00m,
                    timestamp = DateTime.UtcNow.AddHours(-1)
                },
                new
                {
                    id = Guid.NewGuid(),
                    action = "sold",
                    itemName = "Designer Handbag",
                    provider = "Mike Chen",
                    value = 285.00m,
                    timestamp = DateTime.UtcNow.AddHours(-3)
                }
            }
        };

        return Ok(new { success = true, data = summary });
    }

    [HttpGet("items")]
    public IActionResult GetInventoryItems(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] string? consignor = null)
    {
        // Stubbed inventory items
        var items = new[]
        {
            new
            {
                id = Guid.NewGuid(),
                name = "Vintage Designer Handbag",
                category = "Accessories",
                provider = "Sarah Johnson",
                consignmentPrice = 285.00m,
                shopSplit = 142.50m,
                providerSplit = 142.50m,
                dateAdded = DateTime.UtcNow.AddDays(-5),
                status = "Available",
                location = "Floor - Section A"
            },
            new
            {
                id = Guid.NewGuid(),
                name = "Antique Jewelry Set",
                category = "Jewelry",
                provider = "Lisa Martinez",
                consignmentPrice = 450.00m,
                shopSplit = 225.00m,
                providerSplit = 225.00m,
                dateAdded = DateTime.UtcNow.AddDays(-12),
                status = "Available",
                location = "Display Case 3"
            },
            new
            {
                id = Guid.NewGuid(),
                name = "Designer Evening Dress",
                category = "Clothing",
                provider = "Mike Chen",
                consignmentPrice = 175.00m,
                shopSplit = 87.50m,
                providerSplit = 87.50m,
                dateAdded = DateTime.UtcNow.AddDays(-8),
                status = "Hold",
                location = "Storage Room B"
            }
        };

        // Apply filters (stubbed)
        var filteredItems = items.AsEnumerable();

        if (!string.IsNullOrEmpty(category) && category != "All Categories")
        {
            filteredItems = filteredItems.Where(i =>
                i.category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(status) && status != "All Statuses")
        {
            filteredItems = filteredItems.Where(i =>
                i.status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var itemsArray = filteredItems.ToArray();

        // Pagination (stubbed)
        var totalCount = itemsArray.Length;
        var paginatedItems = itemsArray
            .Skip((page - 1) * limit)
            .Take(limit);

        return Ok(new
        {
            success = true,
            data = paginatedItems,
            pagination = new
            {
                page,
                limit,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / limit)
            }
        });
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = new[]
        {
            "All Categories",
            "Clothing",
            "Accessories",
            "Jewelry",
            "Home Decor",
            "Electronics",
            "Books",
            "Collectibles"
        };

        return Ok(new { success = true, data = categories });
    }

    [HttpGet("statuses")]
    public IActionResult GetStatuses()
    {
        var statuses = new[]
        {
            "All Statuses",
            "Available",
            "Sold",
            "Hold",
            "Returned",
            "Damaged"
        };

        return Ok(new { success = true, data = statuses });
    }

    [HttpGet("low-stock")]
    public IActionResult GetLowStockItems([FromQuery] int threshold = 5)
    {
        // Stubbed low stock items
        var lowStockItems = new[]
        {
            new
            {
                category = "Electronics",
                count = 3,
                threshold = threshold,
                lastAdded = DateTime.UtcNow.AddDays(-15)
            },
            new
            {
                category = "Books",
                count = 2,
                threshold = threshold,
                lastAdded = DateTime.UtcNow.AddDays(-20)
            }
        };

        return Ok(new { success = true, data = lowStockItems });
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : throw new UnauthorizedAccessException();
    }

    [HttpGet("pending-imports")]
    public async Task<IActionResult> GetPendingImports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var organizationId = GetOrganizationId();

        var pendingImports = await _assignmentService.GetPendingImportsAsync(organizationId);

        // Apply search filter if provided
        if (!string.IsNullOrEmpty(search))
        {
            pendingImports = pendingImports.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (p.Sku != null && p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        // Get pending transaction counts for each import
        var importDtos = new List<PendingImportDto>();
        foreach (var import in pendingImports)
        {
            var pendingTransactions = await _unitOfWork.PendingSquareTransactionItems
                .GetAllAsync(pti => pti.SquareCatalogId == import.SquareCatalogId);

            var pendingTransactionCount = pendingTransactions.Count();
            var pendingTransactionTotal = pendingTransactions.Sum(pt => pt.LineTotal);

            importDtos.Add(new PendingImportDto
            {
                Id = import.Id,
                Name = import.Name,
                Price = import.Price,
                Sku = import.Sku,
                Barcode = import.Barcode,
                Category = import.Category,
                ImportedAt = import.ImportedAt,
                PendingTransactionCount = pendingTransactionCount,
                PendingTransactionTotal = pendingTransactionTotal
            });
        }

        // Apply pagination
        var totalCount = importDtos.Count;
        var paginatedItems = importDtos
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            success = true,
            data = paginatedItems,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        });
    }

    [HttpPost("pending-imports/{id}/assign")]
    public async Task<IActionResult> AssignPendingImport(Guid id, [FromBody] AssignPendingImportRequest request)
    {
        try
        {
            var result = await _assignmentService.AssignConsignorAsync(id, request.ConsignorId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    item = new
                    {
                        id = result.Id,
                        name = result.Title,
                        price = result.Price,
                        consignorId = result.ConsignorId,
                        status = result.Status.ToString()
                    },
                    transactionsPromoted = 1, // This should come from the service
                    transactionsTotal = result.Price // This should be calculated from actual transactions promoted
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("pending-imports/bulk-assign")]
    public async Task<IActionResult> BulkAssignPendingImports([FromBody] BulkAssignPendingImportsRequest request)
    {
        try
        {
            var result = await _assignmentService.BulkAssignAsync(request.PendingImportIds, request.ConsignorId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    itemsPromoted = result.Assigned,
                    transactionsPromoted = 0, // TODO: Calculate from actual promoted transactions
                    transactionsTotal = 0m,   // TODO: Calculate from actual promoted transactions
                    failed = result.Failed.Select(f => new
                    {
                        pendingImportId = f.PendingImportId,
                        reason = f.Reason
                    })
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("pending-imports/{id}")]
    public async Task<IActionResult> DeletePendingImport(Guid id, [FromQuery] bool includeTransactions = false)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var pendingImport = await _unitOfWork.PendingSquareImports.GetByIdAsync(id);

            if (pendingImport == null || pendingImport.OrganizationId != organizationId)
            {
                return NotFound(new { success = false, message = "Pending import not found" });
            }

            // Check if there are pending transactions for this item
            var pendingTransactions = await _unitOfWork.PendingSquareTransactionItems
                .GetAllAsync(pti => pti.SquareCatalogId == pendingImport.SquareCatalogId);

            if (pendingTransactions.Any() && !includeTransactions)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "This item has pending transactions. Use includeTransactions=true to delete both item and transactions."
                });
            }

            // Delete pending transactions if they exist
            foreach (var transaction in pendingTransactions)
            {
                await _unitOfWork.PendingSquareTransactionItems.DeleteAsync(transaction);
            }

            // Delete the pending import
            await _unitOfWork.PendingSquareImports.DeleteAsync(pendingImport);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("pending-imports/counts")]
    public async Task<IActionResult> GetPendingCounts()
    {
        var organizationId = GetOrganizationId();

        var pendingItemsCount = await _assignmentService.GetPendingCountAsync(organizationId);

        var pendingTransactions = await _unitOfWork.PendingSquareTransactions
            .GetAllAsync(pt => pt.OrganizationId == organizationId);

        var pendingTransactionsCount = pendingTransactions.Count();
        var pendingTransactionsTotal = pendingTransactions.Sum(pt => pt.Total);

        return Ok(new
        {
            success = true,
            data = new
            {
                pendingItems = pendingItemsCount,
                pendingTransactions = pendingTransactionsCount,
                pendingTransactionsTotal = pendingTransactionsTotal
            }
        });
    }
}

// Request/Response DTOs
public class PendingImportDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public DateTime ImportedAt { get; set; }
    public int PendingTransactionCount { get; set; }
    public decimal PendingTransactionTotal { get; set; }
}

public class AssignPendingImportRequest
{
    public Guid ConsignorId { get; set; }
}

public class BulkAssignPendingImportsRequest
{
    public List<Guid> PendingImportIds { get; set; } = new();
    public Guid ConsignorId { get; set; }
}