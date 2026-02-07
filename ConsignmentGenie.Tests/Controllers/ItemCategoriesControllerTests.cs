using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ItemCategoriesControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<ItemCategory>> _mockItemCategoryRepository;
        private readonly ItemCategoriesController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _categoryId = Guid.NewGuid();

        public ItemCategoriesControllerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockItemCategoryRepository = new Mock<IRepository<ItemCategory>>();

            _mockUnitOfWork.Setup(u => u.ItemCategories).Returns(_mockItemCategoryRepository.Object);

            _controller = new ItemCategoriesController(_mockUnitOfWork.Object);

            // Setup user claims
            var claims = new List<Claim>
            {
                new("OrganizationId", _organizationId.ToString()),
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
        }

        [Fact]
        public void Constructor_WithValidDependencies_CreatesSuccessfully()
        {
            // Arrange & Act
            var controller = new ItemCategoriesController(_mockUnitOfWork.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task GetCategories_ReturnsActiveCategories()
        {
            // Arrange
            var categories = new List<ItemCategory>
            {
                new ItemCategory
                {
                    Id = _categoryId,
                    OrganizationId = _organizationId,
                    Name = "Electronics",
                    Description = "Electronic items",
                    Color = "#FF0000",
                    IsActive = true,
                    SortOrder = 1,
                    DefaultCommissionRate = 10.5m,
                    SubCategories = new List<ItemCategory>(),
                    CreatedAt = DateTime.UtcNow
                },
                new ItemCategory
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Name = "Clothing",
                    Description = "Clothing items",
                    IsActive = true,
                    SortOrder = 2,
                    SubCategories = new List<ItemCategory>(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockItemCategoryRepository.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "SubCategories"))
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ItemCategoryDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count);
            Assert.Equal("Electronics", apiResponse.Data.First().Name);
        }

        [Fact]
        public async Task GetCategories_WithIncludeInactive_ReturnsAllCategories()
        {
            // Arrange
            var categories = new List<ItemCategory>
            {
                new ItemCategory
                {
                    Id = _categoryId,
                    OrganizationId = _organizationId,
                    Name = "Active Category",
                    IsActive = true,
                    SortOrder = 1,
                    SubCategories = new List<ItemCategory>(),
                    CreatedAt = DateTime.UtcNow
                },
                new ItemCategory
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Name = "Inactive Category",
                    IsActive = false,
                    SortOrder = 2,
                    SubCategories = new List<ItemCategory>(),
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockItemCategoryRepository.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "SubCategories"))
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetCategories(includeInactive: true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ItemCategoryDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(2, apiResponse.Data.Count);
        }

        [Fact]
        public async Task GetCategory_WithValidId_ReturnsCategory()
        {
            // Arrange
            var category = new ItemCategory
            {
                Id = _categoryId,
                OrganizationId = _organizationId,
                Name = "Electronics",
                Description = "Electronic items",
                Color = "#FF0000",
                IsActive = true,
                SortOrder = 1,
                DefaultCommissionRate = 10.5m,
                SubCategories = new List<ItemCategory>(),
                Items = new List<Item>
                {
                    new Item { Id = Guid.NewGuid() },
                    new Item { Id = Guid.NewGuid() }
                },
                CreatedAt = DateTime.UtcNow
            };

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "SubCategories,Items"))
                .ReturnsAsync(category);

            // Act
            var result = await _controller.GetCategory(_categoryId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ItemCategoryDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(_categoryId, apiResponse.Data.Id);
            Assert.Equal("Electronics", apiResponse.Data.Name);
            Assert.Equal(2, apiResponse.Data.ItemCount);
        }

        [Fact]
        public async Task GetCategory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "SubCategories,Items"))
                .ReturnsAsync((ItemCategory?)null);

            // Act
            var result = await _controller.GetCategory(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ItemCategoryDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Category not found", apiResponse.Errors);
        }

        [Fact]
        public async Task CreateCategory_WithValidData_CreatesSuccessfully()
        {
            // Arrange
            var createDto = new CreateItemCategoryDto
            {
                Name = "New Category",
                Description = "New category description",
                Color = "#00FF00",
                SortOrder = 5,
                DefaultCommissionRate = 15.0m
            };

            _mockItemCategoryRepository.Setup(r => r.AddAsync(It.IsAny<ItemCategory>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _controller.CreateCategory(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ItemCategoryDto>>(createdAtActionResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("New Category", apiResponse.Data.Name);
            Assert.Equal("New category description", apiResponse.Data.Description);
            Assert.Equal("#00FF00", apiResponse.Data.Color);
            Assert.Equal(15.0m, apiResponse.Data.DefaultCommissionRate);

            // Verify repository calls
            _mockItemCategoryRepository.Verify(r => r.AddAsync(It.IsAny<ItemCategory>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateCategory_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var category = new ItemCategory
            {
                Id = _categoryId,
                OrganizationId = _organizationId,
                Name = "Old Name",
                Description = "Old description",
                Color = "#FF0000",
                IsActive = true,
                SortOrder = 1,
                DefaultCommissionRate = 10.0m,
                CreatedAt = DateTime.UtcNow
            };

            var updateDto = new UpdateItemCategoryDto
            {
                Name = "Updated Name",
                Description = "Updated description",
                Color = "#00FF00",
                SortOrder = 3,
                DefaultCommissionRate = 12.5m,
                IsActive = false
            };

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                ""))
                .ReturnsAsync(category);

            _mockItemCategoryRepository.Setup(r => r.UpdateAsync(It.IsAny<ItemCategory>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _controller.UpdateCategory(_categoryId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ItemCategoryDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Contains("Category updated successfully", apiResponse.Message);

            // Verify the category was updated
            Assert.Equal("Updated Name", category.Name);
            Assert.Equal("Updated description", category.Description);
            Assert.Equal("#00FF00", category.Color);
            Assert.Equal(3, category.SortOrder);
            Assert.Equal(12.5m, category.DefaultCommissionRate);
            Assert.False(category.IsActive);

            _mockItemCategoryRepository.Verify(r => r.UpdateAsync(It.IsAny<ItemCategory>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateCategory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new UpdateItemCategoryDto
            {
                Name = "Updated Name",
                IsActive = true
            };

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                ""))
                .ReturnsAsync((ItemCategory?)null);

            // Act
            var result = await _controller.UpdateCategory(invalidId, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ItemCategoryDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Category not found", apiResponse.Errors);
        }

        [Fact]
        public async Task DeleteCategory_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var category = new ItemCategory
            {
                Id = _categoryId,
                OrganizationId = _organizationId,
                Name = "Test Category",
                Items = new List<Item>(), // No items
                SubCategories = new List<ItemCategory>() // No subcategories
            };

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "Items,SubCategories"))
                .ReturnsAsync(category);

            _mockItemCategoryRepository.Setup(r => r.DeleteAsync(It.IsAny<ItemCategory>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteCategory(_categoryId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.True(apiResponse.Data);
            Assert.Contains("Category deleted successfully", apiResponse.Message);

            _mockItemCategoryRepository.Verify(r => r.DeleteAsync(It.IsAny<ItemCategory>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteCategory_WithItems_ReturnsBadRequest()
        {
            // Arrange
            var category = new ItemCategory
            {
                Id = _categoryId,
                OrganizationId = _organizationId,
                Name = "Test Category",
                Items = new List<Item> { new Item { Id = Guid.NewGuid() } }, // Has items
                SubCategories = new List<ItemCategory>()
            };

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "Items,SubCategories"))
                .ReturnsAsync(category);

            // Act
            var result = await _controller.DeleteCategory(_categoryId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Cannot delete category that has items assigned to it", apiResponse.Errors);
        }

        [Fact]
        public async Task DeleteCategory_WithSubCategories_ReturnsBadRequest()
        {
            // Arrange
            var category = new ItemCategory
            {
                Id = _categoryId,
                OrganizationId = _organizationId,
                Name = "Test Category",
                Items = new List<Item>(),
                SubCategories = new List<ItemCategory> { new ItemCategory { Id = Guid.NewGuid() } } // Has subcategories
            };

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "Items,SubCategories"))
                .ReturnsAsync(category);

            // Act
            var result = await _controller.DeleteCategory(_categoryId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Cannot delete category that has subcategories", apiResponse.Errors);
        }

        [Fact]
        public async Task DeleteCategory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            _mockItemCategoryRepository.Setup(r => r.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemCategory, bool>>>(),
                "Items,SubCategories"))
                .ReturnsAsync((ItemCategory?)null);

            // Act
            var result = await _controller.DeleteCategory(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Category not found", apiResponse.Errors);
        }
    }
}