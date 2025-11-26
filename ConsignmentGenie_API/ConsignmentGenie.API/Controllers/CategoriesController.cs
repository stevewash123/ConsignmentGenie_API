using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Items;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ConsignmentGenieContext context, ILogger<CategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        try
        {
            var organizationId = GetOrganizationId();

            var categories = await _context.Categories
                .Where(c => c.OrganizationId == organizationId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<CategoryDto>>.SuccessResult(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<List<CategoryDto>>.ErrorResult("Failed to retrieve categories"));
        }
    }

    // GET: api/categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var category = await _context.Categories
                .Where(c => c.Id == id && c.OrganizationId == organizationId)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound(ApiResponse<CategoryDto>.ErrorResult("Category not found"));
            }

            return Ok(ApiResponse<CategoryDto>.SuccessResult(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return StatusCode(500, ApiResponse<CategoryDto>.ErrorResult("Failed to retrieve category"));
        }
    }

    // POST: api/categories
    [HttpPost]
    [Authorize(Roles = "Owner,Manager")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(CreateCategoryRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            // Check if category name already exists for this organization
            var existingCategory = await _context.Categories
                .AnyAsync(c => c.OrganizationId == organizationId &&
                              c.Name.ToLower() == request.Name.ToLower() &&
                              c.IsActive);

            if (existingCategory)
            {
                return BadRequest(ApiResponse<CategoryDto>.ErrorResult("A category with this name already exists"));
            }

            // Get next display order
            var maxDisplayOrder = await _context.Categories
                .Where(c => c.OrganizationId == organizationId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

            var category = new Category
            {
                OrganizationId = organizationId,
                Name = request.Name.Trim(),
                DisplayOrder = request.DisplayOrder ?? (maxDisplayOrder + 1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
                ApiResponse<CategoryDto>.SuccessResult(categoryDto, "Category created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, ApiResponse<CategoryDto>.ErrorResult("Failed to create category"));
        }
    }

    // PUT: api/categories/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(Guid id, UpdateCategoryRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

            if (category == null)
            {
                return NotFound(ApiResponse<CategoryDto>.ErrorResult("Category not found"));
            }

            // Check if new name conflicts with existing categories (excluding current one)
            if (!string.Equals(category.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var existingCategory = await _context.Categories
                    .AnyAsync(c => c.OrganizationId == organizationId &&
                                  c.Id != id &&
                                  c.Name.ToLower() == request.Name.ToLower() &&
                                  c.IsActive);

                if (existingCategory)
                {
                    return BadRequest(ApiResponse<CategoryDto>.ErrorResult("A category with this name already exists"));
                }
            }

            // Update category
            category.Name = request.Name.Trim();
            category.DisplayOrder = request.DisplayOrder;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            return Ok(ApiResponse<CategoryDto>.SuccessResult(categoryDto, "Category updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, ApiResponse<CategoryDto>.ErrorResult("Failed to update category"));
        }
    }

    // DELETE: api/categories/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Manager")]
    public async Task<ActionResult<ApiResponse<DeleteResponseDto>>> DeleteCategory(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId);

            if (category == null)
            {
                return NotFound(ApiResponse<DeleteResponseDto>.ErrorResult("Category not found"));
            }

            // Check if category is being used by any items
            var hasItems = await _context.Items
                .AnyAsync(i => i.OrganizationId == organizationId && i.Category == category.Name);

            if (hasItems)
            {
                return BadRequest(ApiResponse<DeleteResponseDto>.ErrorResult("Cannot delete category that is assigned to items"));
            }

            // Soft delete by setting IsActive = false
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = GetUserId();

            await _context.SaveChangesAsync();

            var response = new DeleteResponseDto { Message = "Category deleted successfully" };
            return Ok(ApiResponse<DeleteResponseDto>.SuccessResult(response, "Category deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, ApiResponse<DeleteResponseDto>.ErrorResult("Failed to delete category"));
        }
    }

    // PUT: api/categories/reorder
    [HttpPut("reorder")]
    [Authorize(Roles = "Owner,Manager")]
    public async Task<ActionResult<ApiResponse<ReorderResponseDto>>> ReorderCategories(ReorderCategoriesRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var userId = GetUserId();

            // Get all categories for this organization
            var categories = await _context.Categories
                .Where(c => c.OrganizationId == organizationId &&
                           request.CategoryOrders.Select(co => co.CategoryId).Contains(c.Id))
                .ToListAsync();

            if (categories.Count != request.CategoryOrders.Count)
            {
                return BadRequest(ApiResponse<ReorderResponseDto>.ErrorResult("Some categories not found"));
            }

            // Update display orders
            foreach (var orderUpdate in request.CategoryOrders)
            {
                var category = categories.FirstOrDefault(c => c.Id == orderUpdate.CategoryId);
                if (category != null)
                {
                    category.DisplayOrder = orderUpdate.DisplayOrder;
                    category.UpdatedAt = DateTime.UtcNow;
                    category.UpdatedBy = userId;
                }
            }

            await _context.SaveChangesAsync();

            var response = new ReorderResponseDto { Message = "Categories reordered successfully" };
            return Ok(ApiResponse<ReorderResponseDto>.SuccessResult(response, "Categories reordered successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories");
            return StatusCode(500, ApiResponse<ReorderResponseDto>.ErrorResult("Failed to reorder categories"));
        }
    }

    // GET: api/categories/usage-stats
    [HttpGet("usage-stats")]
    [Authorize(Roles = "Owner,Manager")]
    public async Task<ActionResult<ApiResponse<List<CategoryUsageDto>>>> GetCategoryUsageStats()
    {
        try
        {
            var organizationId = GetOrganizationId();

            var categoryStats = await _context.Categories
                .Where(c => c.OrganizationId == organizationId && c.IsActive)
                .Select(c => new CategoryUsageDto
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    ItemCount = _context.Items.Count(i => i.OrganizationId == organizationId && i.Category == c.Name),
                    AvailableItemCount = _context.Items.Count(i => i.OrganizationId == organizationId &&
                                                                   i.Category == c.Name &&
                                                                   i.Status == ItemStatus.Available),
                    SoldItemCount = _context.Items.Count(i => i.OrganizationId == organizationId &&
                                                              i.Category == c.Name &&
                                                              i.Status == ItemStatus.Sold)
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return Ok(ApiResponse<List<CategoryUsageDto>>.SuccessResult(categoryStats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category usage stats for organization {OrganizationId}", GetOrganizationId());
            return StatusCode(500, ApiResponse<List<CategoryUsageDto>>.ErrorResult("Failed to retrieve category statistics"));
        }
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