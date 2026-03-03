using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Models;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Consignor")]
public class ItemCategoriesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryValidationService _categoryValidationService;

    public ItemCategoriesController(IUnitOfWork unitOfWork, ICategoryValidationService categoryValidationService)
    {
        _unitOfWork = unitOfWork;
        _categoryValidationService = categoryValidationService;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var orgId) ? orgId : Guid.Empty;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private bool IsOwner()
    {
        return User.IsInRole("Owner");
    }

    private bool IsConsignor()
    {
        return User.IsInRole("Consignor");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ItemCategoryDto>>>> GetCategories([FromQuery] bool includeInactive = false)
    {
        try
        {
            var organizationId = GetOrganizationId();

            // Amendment 1: Filter categories based on user role
            // Consignors only see active categories, Owners see active + pending
            var categories = await _unitOfWork.ItemCategories.GetAllAsync(
                c => c.OrganizationId == organizationId &&
                     (includeInactive || c.IsActive) &&
                     (IsOwner() || c.Status == CategoryStatus.Active), // Consignors only see active categories
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
                CreatedAt = c.CreatedAt,
                // Amendment 1: Hybrid model fields
                Source = c.Source,
                Status = c.Status,
                ConsignorId = c.ConsignorId,
                SquareCategoryId = c.SquareCategoryId
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
                CreatedAt = category.CreatedAt,
                // Amendment 1: Hybrid model fields
                Source = category.Source,
                Status = category.Status,
                ConsignorId = category.ConsignorId,
                SquareCategoryId = category.SquareCategoryId
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
            var currentUserId = GetCurrentUserId();

            // Amendment 1: Validate pending category request
            if (createDto.RequestPending)
            {
                // Only consignors can request pending categories
                if (!IsConsignor())
                {
                    return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult("Only consignors can request pending categories"));
                }

                // Check if organization allows consignor category suggestions
                var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (organization == null || !organization.AllowConsignorCategorySuggestions)
                {
                    return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult("This shop does not allow category suggestions"));
                }
            }

            // Amendment 1: Check for duplicates considering status
            // For pending requests, check against both active and pending categories
            // For owner creation, check against active categories only (pending can have duplicates)
            var statusFilter = createDto.RequestPending
                ? new[] { CategoryStatus.Active, CategoryStatus.Pending }
                : new[] { CategoryStatus.Active };

            var existingCategory = await _unitOfWork.ItemCategories.GetAsync(
                c => c.OrganizationId == organizationId &&
                     c.Name.ToLower() == createDto.Name.Trim().ToLower() &&
                     statusFilter.Contains(c.Status));

            if (existingCategory != null)
            {
                var message = createDto.RequestPending
                    ? "A category with this name already exists or is pending approval"
                    : "A category with this name already exists";
                return Conflict(ApiResponse<ItemCategoryDto>.ErrorResult(message));
            }

            // If not confirming a new category, validate against fuzzy matches
            if (!createDto.ConfirmedNew)
            {
                var validation = await _categoryValidationService.ValidateCategoryNameAsync(organizationId, createDto.Name);

                if (validation.IsExactMatch)
                {
                    return Conflict(ApiResponse<ItemCategoryDto>.ErrorResult("A category with this name already exists"));
                }

                if (validation.SuggestedMatches.Any())
                {
                    var response = new CategoryValidationResponse
                    {
                        ExactMatch = validation.ExactMatch != null ? new CategoryMatchDto
                        {
                            CategoryId = validation.ExactMatch.Id,
                            Name = validation.ExactMatch.Name
                        } : null,
                        SuggestedMatches = validation.SuggestedMatches.Select(s => new CategorySuggestionDto
                        {
                            CategoryId = s.CategoryId,
                            Name = s.Name,
                            Similarity = s.Similarity
                        }).ToList(),
                        IsExactMatch = validation.IsExactMatch
                    };

                    var errorResponse = ApiResponse<CategoryValidationResponse>.ErrorResult("Similar categories found. Please confirm or select an existing category.");
                    errorResponse.Data = response;
                    return Conflict(errorResponse);
                }
            }

            // Amendment 1: Create category with appropriate Source, Status, and ConsignorId
            var category = new ItemCategory
            {
                OrganizationId = organizationId,
                Name = createDto.Name.Trim(),
                Description = createDto.Description,
                Color = createDto.Color,
                ParentCategoryId = createDto.ParentCategoryId,
                SortOrder = createDto.SortOrder,
                DefaultCommissionRate = createDto.DefaultCommissionRate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                // Amendment 1: Hybrid model fields
                Source = CategorySource.CG, // Always CG source for manual creation
                Status = createDto.RequestPending ? CategoryStatus.Pending : CategoryStatus.Active,
                ConsignorId = createDto.RequestPending ? currentUserId : null,
                SquareCategoryId = null // Not from Square
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
                CreatedAt = category.CreatedAt,
                // Amendment 1: Hybrid model fields
                Source = category.Source,
                Status = category.Status,
                ConsignorId = category.ConsignorId,
                SquareCategoryId = category.SquareCategoryId
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
    [Authorize(Roles = "Owner")]
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

            // Amendment 1: Prevent modification of Square-sourced categories
            if (category.Source == CategorySource.Square)
            {
                return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult("Cannot modify Square-sourced categories. Changes will be overwritten on next sync."));
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
                CreatedAt = category.CreatedAt,
                // Amendment 1: Hybrid model fields
                Source = category.Source,
                Status = category.Status,
                ConsignorId = category.ConsignorId,
                SquareCategoryId = category.SquareCategoryId
            };

            return Ok(ApiResponse<ItemCategoryDto>.SuccessResult(categoryDto, "Category updated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult($"Failed to update category: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Owner")]
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

            // Amendment 1: Prevent deletion of Square-sourced categories
            if (category.Source == CategorySource.Square)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Cannot delete Square-sourced categories. They will be recreated on next sync."));
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

    /// <summary>
    /// Validates a category name against existing categories using fuzzy matching.
    /// Returns exact match or suggests similar categories.
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<CategoryValidationResponse>> ValidateCategory(ValidateCategoryRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var validation = await _categoryValidationService.ValidateCategoryNameAsync(organizationId, request.Name);

            var response = new CategoryValidationResponse
            {
                ExactMatch = validation.ExactMatch != null ? new CategoryMatchDto
                {
                    CategoryId = validation.ExactMatch.Id,
                    Name = validation.ExactMatch.Name
                } : null,
                SuggestedMatches = validation.SuggestedMatches.Select(s => new CategorySuggestionDto
                {
                    CategoryId = s.CategoryId,
                    Name = s.Name,
                    Similarity = s.Similarity
                }).ToList(),
                IsExactMatch = validation.IsExactMatch
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CategoryValidationResponse>.ErrorResult($"Failed to validate category: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validates multiple category names in bulk for CSV import preview.
    /// Returns validation results for each name.
    /// </summary>
    [HttpPost("validate-bulk")]
    public async Task<ActionResult<List<CategoryValidationResponse>>> ValidateCategoriesBulk(ValidateCategoriesBulkRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var validations = await _categoryValidationService.ValidateCategoryNamesBulkAsync(organizationId, request.Names);

            var responses = validations.Select(validation => new CategoryValidationResponse
            {
                ExactMatch = validation.ExactMatch != null ? new CategoryMatchDto
                {
                    CategoryId = validation.ExactMatch.Id,
                    Name = validation.ExactMatch.Name
                } : null,
                SuggestedMatches = validation.SuggestedMatches.Select(s => new CategorySuggestionDto
                {
                    CategoryId = s.CategoryId,
                    Name = s.Name,
                    Similarity = s.Similarity
                }).ToList(),
                IsExactMatch = validation.IsExactMatch
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<CategoryValidationResponse>>.ErrorResult($"Failed to validate categories: {ex.Message}"));
        }
    }

    /// <summary>
    /// Amendment 1: Promotes a pending category to active status.
    /// Only owners can promote pending categories.
    /// </summary>
    [HttpPost("{id}/promote")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<ItemCategoryDto>>> PromoteCategory(Guid id)
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

            if (category.Status != CategoryStatus.Pending)
            {
                return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult("Only pending categories can be promoted"));
            }

            // Check if promoting would create a duplicate active category
            var existingActive = await _unitOfWork.ItemCategories.GetAsync(
                c => c.OrganizationId == organizationId &&
                     c.Name.ToLower() == category.Name.ToLower() &&
                     c.Status == CategoryStatus.Active
            );

            if (existingActive != null)
            {
                return Conflict(ApiResponse<ItemCategoryDto>.ErrorResult("An active category with this name already exists"));
            }

            // Promote to active
            category.Status = CategoryStatus.Active;
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
                CreatedAt = category.CreatedAt,
                Source = category.Source,
                Status = category.Status,
                ConsignorId = category.ConsignorId,
                SquareCategoryId = category.SquareCategoryId
            };

            return Ok(ApiResponse<ItemCategoryDto>.SuccessResult(categoryDto, "Category promoted successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ItemCategoryDto>.ErrorResult($"Failed to promote category: {ex.Message}"));
        }
    }

    /// <summary>
    /// Amendment 1: Rejects and deletes a pending category.
    /// Only owners can reject pending categories.
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ApiResponse<bool>>> RejectCategory(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var category = await _unitOfWork.ItemCategories.GetAsync(
                c => c.Id == id && c.OrganizationId == organizationId
            );

            if (category == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Category not found"));
            }

            if (category.Status != CategoryStatus.Pending)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Only pending categories can be rejected"));
            }

            await _unitOfWork.ItemCategories.DeleteAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResult(true, "Category rejected and removed"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult($"Failed to reject category: {ex.Message}"));
        }
    }
}