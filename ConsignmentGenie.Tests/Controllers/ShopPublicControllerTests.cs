using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Shopper;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ShopPublicControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly Mock<ILogger<ShopPublicController>> _mockLogger;
        private readonly ShopPublicController _controller;

        public ShopPublicControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<ShopPublicController>>();
            _controller = new ShopPublicController(_context, _mockLogger.Object);

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Store",
                Slug = "test-store",
                CreatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetStoreInfo_ValidStoreSlug_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";

            // Act
            var result = await _controller.GetStoreInfo(storeSlug);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<StoreInfoDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Test Store", apiResponse.Data.Name);
            Assert.Equal(storeSlug, apiResponse.Data.Slug);
            Assert.True(apiResponse.Data.IsOpen);
        }

        [Fact]
        public async Task GetStoreInfo_InvalidStoreSlug_ReturnsNotFound()
        {
            // Arrange
            var storeSlug = "invalid-store";

            // Act
            var result = await _controller.GetStoreInfo(storeSlug);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<StoreInfoDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Store not found", apiResponse.Errors);
        }

        [Fact]
        public async Task GetItems_ValidStoreSlug_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";

            // Act
            var result = await _controller.GetItems(storeSlug);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ShopperCatalogDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data.Items);
        }

        [Fact]
        public async Task GetItems_InvalidStoreSlug_ReturnsNotFound()
        {
            // Arrange
            var storeSlug = "invalid-store";

            // Act
            var result = await _controller.GetItems(storeSlug);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ShopperCatalogDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Store not found", apiResponse.Errors);
        }

        [Fact]
        public async Task GetCategories_ValidStoreSlug_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";

            // Act
            var result = await _controller.GetCategories(storeSlug);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ShopperCategoryDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        [Fact]
        public async Task GetCategories_InvalidStoreSlug_ReturnsNotFound()
        {
            // Arrange
            var storeSlug = "invalid-store";

            // Act
            var result = await _controller.GetCategories(storeSlug);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ShopperCategoryDto>>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Store not found", apiResponse.Errors);
        }

        [Fact]
        public async Task SearchItems_ValidStoreSlugAndQuery_ReturnsOkResult()
        {
            // Arrange
            var storeSlug = "test-store";
            var searchQuery = "test";

            // Act
            var result = await _controller.SearchItems(storeSlug, searchQuery);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ShopperSearchResultDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data.Items);
        }

        [Fact]
        public async Task SearchItems_InvalidStoreSlug_ReturnsNotFound()
        {
            // Arrange
            var storeSlug = "invalid-store";
            var searchQuery = "test";

            // Act
            var result = await _controller.SearchItems(storeSlug, searchQuery);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ShopperSearchResultDto>>(notFoundResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Store not found", apiResponse.Errors);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}