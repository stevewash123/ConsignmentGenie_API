using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class InventoryController : ControllerBase
{
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
        [FromQuery] string? provider = null)
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
}