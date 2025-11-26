using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Models;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class ItemCategoriesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ItemCategoriesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var orgId) ? orgId : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemCategoryDto>>>> GetCategories([FromQuery] bool includeInactive = false)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var categories = await _unitOfWork.ItemCategories.GetAllAsync(
                c => c.OrganizationId == organizationId && (includeInactive || c.IsActive),
                "SubCategories"
            );

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
                SubCategoryCount = c.SubCategories.Count,
                CreatedAt = c.CreatedAt
            }).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();

            return Ok(ApiResponse<List<ItemCategoryDto>>.SuccessResult(categoryDtos));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<ItemCategoryDto>>.ErrorResult($"Failed to get categories: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> GetCategory(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var category = await _unitOfWork.ItemCategories.GetAsync(
                c => c.Id == id && c.OrganizationId == organizationId,
                "SubCategories,Items"
            );

            if (category == null)
            {
                return NotFound(ApiResponse<ItemCategoryDto>.ErrorResult("Category not found"));
            }

            var categoryDto = new ItemCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                IsActive = category.IsActive,
                ParentCategoryId = category.ParentCategoryId,
                SortOrder = category.SortOrder,
                DefaultCommissionRate = category.DefaultCommissionRate,
                SubCategoryCount = category.SubCategories.Count,
                ItemCount = category.Items.Count,
                CreatedAt = category.CreatedAt
            };

            return Ok(ApiResponse<ItemCategoryDto>.SuccessResult(categoryDto));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult($"Failed to get category: {ex.Message}"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> CreateCategory(CreateItemCategoryDto createDto)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var category = new ItemCategory
            {
                OrganizationId = organizationId,
                Name = createDto.Name,
                Description = createDto.Description,
                Color = createDto.Color,
                ParentCategoryId = createDto.ParentCategoryId,
                SortOrder = createDto.SortOrder,
                DefaultCommissionRate = createDto.DefaultCommissionRate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ItemCategories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var categoryDto = new ItemCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                IsActive = category.IsActive,
                ParentCategoryId = category.ParentCategoryId,
                SortOrder = category.SortOrder,
                DefaultCommissionRate = category.DefaultCommissionRate,
                SubCategoryCount = 0,
                ItemCount = 0,
                CreatedAt = category.CreatedAt
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
                ApiResponse<ItemCategoryDto>.SuccessResult(categoryDto, "Category created successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult($"Failed to create category: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> UpdateCategory(Guid id, UpdateItemCategoryDto updateDto)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var category = await _unitOfWork.ItemCategories.GetAsync(
                c => c.Id == id && c.OrganizationId == organizationId
            );

            if (category == null)
            {
                return NotFound(ApiResponse<ItemCategoryDto>.ErrorResult("Category not found"));
            }

            category.Name = updateDto.Name;
            category.Description = updateDto.Description;
            category.Color = updateDto.Color;
            category.ParentCategoryId = updateDto.ParentCategoryId;
            category.SortOrder = updateDto.SortOrder;
            category.DefaultCommissionRate = updateDto.DefaultCommissionRate;
            category.IsActive = updateDto.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.ItemCategories.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var categoryDto = new ItemCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                IsActive = category.IsActive,
                ParentCategoryId = category.ParentCategoryId,
                SortOrder = category.SortOrder,
                DefaultCommissionRate = category.DefaultCommissionRate,
                CreatedAt = category.CreatedAt
            };

            return Ok(ApiResponse<ItemCategoryDto>.SuccessResult(categoryDto, "Category updated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult($"Failed to update category: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var category = await _unitOfWork.ItemCategories.GetAsync(
                c => c.Id == id && c.OrganizationId == organizationId,
                "Items,SubCategories"
            );

            if (category == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Category not found"));
            }

            if (category.Items.Any())
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Cannot delete category that has items assigned to it"));
            }

            if (category.SubCategories.Any())
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Cannot delete category that has subcategories"));
            }

            await _unitOfWork.ItemCategories.DeleteAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Category deleted successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult($"Failed to delete category: {ex.Message}"));
        }
    }
}