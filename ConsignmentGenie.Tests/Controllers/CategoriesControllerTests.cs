using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Items;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;

namespace ConsignmentGenie.Tests.Controllers
{
    public class CategoriesControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<ILogger<CategoriesController>> _mockLogger;
        private readonly CategoriesController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();

        public CategoriesControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<CategoriesController>>();
            _controller = new CategoriesController(_context, _mockLogger.Object);

            // Setup user claims
            var claims = new List<Claim>
            {
                new("organizationId", _organizationId.ToString()),
                new("userId", _userId.ToString()),
                new(ClaimTypes.Role, "Owner")
            };
            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Add organization
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Organization",
                Slug = "test-org",
                CreatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add categories
            var category1 = new Category
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Name = "Clothing",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            var category2 = new Category
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Name = "Electronics",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            var category3 = new Category
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Name = "Inactive Category",
                DisplayOrder = 3,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            _context.Categories.AddRange(category1, category2, category3);

            // Add items for testing usage stats
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = Guid.NewGuid(),
                Sku = "TEST001",
                Title = "Test Item 1",
                Category = "Clothing",
                Status = ItemStatus.Available,
                Price = 25.00m,
                Condition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            var item2 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = Guid.NewGuid(),
                Sku = "TEST002",
                Title = "Test Item 2",
                Category = "Clothing",
                Status = ItemStatus.Sold,
                Price = 30.00m,
                Condition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            _context.Items.AddRange(item1, item2);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetCategories_ReturnsActiveCategories()
        {
            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<CategoryDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count); // Only active categories
            Assert.Contains(apiResponse.Data, c => c.Name == "Clothing");
            Assert.Contains(apiResponse.Data, c => c.Name == "Electronics");
            Assert.DoesNotContain(apiResponse.Data, c => c.Name == "Inactive Category");
        }

        [Fact]
        public async Task GetCategory_WithValidId_ReturnsCategory()
        {
            // Arrange
            var category = _context.Categories.First(c => c.Name == "Clothing");

            // Act
            var result = await _controller.GetCategory(category.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Clothing", apiResponse.Data.Name);
            Assert.Equal(1, apiResponse.Data.DisplayOrder);
        }

        [Fact]
        public async Task GetCategory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _controller.GetCategory(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Category not found", apiResponse.Errors);
        }

        [Fact]
        public async Task CreateCategory_WithValidData_CreatesSuccessfully()
        {
            // Arrange
            var request = new CreateCategoryRequest
            {
                Name = "New Category"
            };

            // Act
            var result = await _controller.CreateCategory(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(createdAtActionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("New Category", apiResponse.Data.Name);
            Assert.Equal(4, apiResponse.Data.DisplayOrder); // Should be max + 1 (inactive category has DisplayOrder 3)

            // Verify in database
            var categoryInDb = await _context.Categories.FindAsync(apiResponse.Data.Id);
            Assert.NotNull(categoryInDb);
            Assert.Equal("New Category", categoryInDb.Name);
        }

        [Fact]
        public async Task CreateCategory_WithDuplicateName_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateCategoryRequest
            {
                Name = "Clothing" // Already exists
            };

            // Act
            var result = await _controller.CreateCategory(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("A category with this name already exists", apiResponse.Errors);
        }

        [Fact]
        public async Task CreateCategory_WithCustomDisplayOrder_UsesSpecifiedOrder()
        {
            // Arrange
            var request = new CreateCategoryRequest
            {
                Name = "Custom Order Category",
                DisplayOrder = 5
            };

            // Act
            var result = await _controller.CreateCategory(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(createdAtActionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(5, apiResponse.Data.DisplayOrder);
        }

        [Fact]
        public async Task UpdateCategory_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var category = _context.Categories.First(c => c.Name == "Electronics");
            var request = new UpdateCategoryRequest
            {
                Name = "Updated Electronics",
                DisplayOrder = 10
            };

            // Act
            var result = await _controller.UpdateCategory(category.Id, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Updated Electronics", apiResponse.Data.Name);
            Assert.Equal(10, apiResponse.Data.DisplayOrder);

            // Verify in database
            var updatedCategory = await _context.Categories.FindAsync(category.Id);
            Assert.Equal("Updated Electronics", updatedCategory.Name);
            Assert.Equal(10, updatedCategory.DisplayOrder);
        }

        [Fact]
        public async Task UpdateCategory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var request = new UpdateCategoryRequest
            {
                Name = "Updated Name",
                DisplayOrder = 1
            };

            // Act
            var result = await _controller.UpdateCategory(invalidId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Category not found", apiResponse.Errors);
        }

        [Fact]
        public async Task UpdateCategory_WithDuplicateName_ReturnsBadRequest()
        {
            // Arrange
            var category = _context.Categories.First(c => c.Name == "Electronics");
            var request = new UpdateCategoryRequest
            {
                Name = "Clothing", // Already exists
                DisplayOrder = 1
            };

            // Act
            var result = await _controller.UpdateCategory(category.Id, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("A category with this name already exists", apiResponse.Errors);
        }

        [Fact]
        public async Task DeleteCategory_WithoutItems_DeletesSuccessfully()
        {
            // Arrange
            var category = _context.Categories.First(c => c.Name == "Electronics");

            // Act
            var result = await _controller.DeleteCategory(category.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<DeleteResponseDto>>(okResult.Value);
            Assert.True(apiResponse.Success);

            // Verify soft delete in database
            var deletedCategory = await _context.Categories.FindAsync(category.Id);
            Assert.NotNull(deletedCategory);
            Assert.False(deletedCategory.IsActive);
        }

        [Fact]
        public async Task DeleteCategory_WithItems_ReturnsBadRequest()
        {
            // Arrange
            var category = _context.Categories.First(c => c.Name == "Clothing"); // Has items

            // Act
            var result = await _controller.DeleteCategory(category.Id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<DeleteResponseDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Cannot delete category that is assigned to items", apiResponse.Errors);
        }

        [Fact]
        public async Task DeleteCategory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteCategory(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<DeleteResponseDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Category not found", apiResponse.Errors);
        }

        [Fact]
        public async Task ReorderCategories_WithValidData_ReordersSuccessfully()
        {
            // Arrange
            var categories = _context.Categories.Where(c => c.IsActive).ToList();
            var request = new ReorderCategoriesRequest
            {
                CategoryOrders = new List<CategoryOrderUpdate>
                {
                    new CategoryOrderUpdate { CategoryId = categories[0].Id, DisplayOrder = 2 },
                    new CategoryOrderUpdate { CategoryId = categories[1].Id, DisplayOrder = 1 }
                }
            };

            // Act
            var result = await _controller.ReorderCategories(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReorderResponseDto>>(okResult.Value);
            Assert.True(apiResponse.Success);

            // Verify reordering in database
            var reorderedCategories = _context.Categories.Where(c => c.IsActive).ToList();
            var firstCategory = reorderedCategories.First(c => c.Id == categories[0].Id);
            var secondCategory = reorderedCategories.First(c => c.Id == categories[1].Id);
            Assert.Equal(2, firstCategory.DisplayOrder);
            Assert.Equal(1, secondCategory.DisplayOrder);
        }

        [Fact]
        public async Task ReorderCategories_WithInvalidCategoryIds_ReturnsBadRequest()
        {
            // Arrange
            var request = new ReorderCategoriesRequest
            {
                CategoryOrders = new List<CategoryOrderUpdate>
                {
                    new CategoryOrderUpdate { CategoryId = Guid.NewGuid(), DisplayOrder = 1 }
                }
            };

            // Act
            var result = await _controller.ReorderCategories(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ReorderResponseDto>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Some categories not found", apiResponse.Errors);
        }

        [Fact]
        public async Task GetCategoryUsageStats_ReturnsUsageStatistics()
        {
            // Act
            var result = await _controller.GetCategoryUsageStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<CategoryUsageDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);

            var clothingStats = apiResponse.Data.First(c => c.CategoryName == "Clothing");
            Assert.Equal(2, clothingStats.ItemCount);
            Assert.Equal(1, clothingStats.AvailableItemCount);
            Assert.Equal(1, clothingStats.SoldItemCount);

            var electronicsStats = apiResponse.Data.First(c => c.CategoryName == "Electronics");
            Assert.Equal(0, electronicsStats.ItemCount);
            Assert.Equal(0, electronicsStats.AvailableItemCount);
            Assert.Equal(0, electronicsStats.SoldItemCount);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}