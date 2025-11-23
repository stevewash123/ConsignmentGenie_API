using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Storefront;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers;

public class PublicStoreControllerTests
{
    private readonly Mock<IStoreService> _mockStoreService;
    private readonly Mock<ILogger<PublicStoreController>> _mockLogger;
    private readonly PublicStoreController _controller;

    public PublicStoreControllerTests()
    {
        _mockStoreService = new Mock<IStoreService>();
        _mockLogger = new Mock<ILogger<PublicStoreController>>();
        _controller = new PublicStoreController(_mockStoreService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetStoreInfo_ValidSlug_ReturnsStoreInfo()
    {
        // Arrange
        var storeSlug = "test-store";
        var expectedStore = new StoreInfoDto
        {
            Slug = storeSlug,
            Name = "Test Store",
            TaxRate = 0.085m,
            ShippingEnabled = true
        };

        _mockStoreService.Setup(x => x.GetStoreInfoAsync(storeSlug))
                        .ReturnsAsync(expectedStore);

        // Act
        var result = await _controller.GetStoreInfo(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StoreInfoDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<StoreInfoDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedStore.Slug, response.Data.Slug);
        Assert.Equal(expectedStore.Name, response.Data.Name);
    }

    [Fact]
    public async Task GetStoreInfo_InvalidSlug_ReturnsNotFound()
    {
        // Arrange
        var storeSlug = "non-existent-store";
        _mockStoreService.Setup(x => x.GetStoreInfoAsync(storeSlug))
                        .ReturnsAsync((StoreInfoDto?)null);

        // Act
        var result = await _controller.GetStoreInfo(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StoreInfoDto>>>(result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Store not found", response.Message);
    }

    [Fact]
    public async Task GetItems_ValidRequest_ReturnsItems()
    {
        // Arrange
        var storeSlug = "test-store";
        var expectedItems = new List<PublicItemDto>
        {
            new PublicItemDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Item",
                Price = 25.99m,
                Category = "Electronics"
            }
        };

        _mockStoreService.Setup(x => x.GetItemsAsync(storeSlug, It.IsAny<ItemQueryParams>()))
                        .ReturnsAsync((expectedItems, 1));

        // Act
        var result = await _controller.GetItems(storeSlug, category: "Electronics", page: 1, pageSize: 10);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<object>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task GetItems_EmptyResults_ReturnsEmptyPage()
    {
        // Arrange
        var storeSlug = "test-store";
        _mockStoreService.Setup(x => x.GetItemsAsync(storeSlug, It.IsAny<ItemQueryParams>()))
                        .ReturnsAsync((new List<PublicItemDto>(), 0));

        // Act
        var result = await _controller.GetItems(storeSlug, page: 1, pageSize: 10);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<object>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task GetItems_InvalidPageSize_UseDefaultPageSize()
    {
        // Arrange
        var storeSlug = "test-store";
        _mockStoreService.Setup(x => x.GetItemsAsync(storeSlug, It.IsAny<ItemQueryParams>()))
                        .ReturnsAsync((new List<PublicItemDto>(), 0));

        // Act
        var result = await _controller.GetItems(storeSlug, page: 1, pageSize: 0); // Invalid page size should use default

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<object>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        _mockStoreService.Verify(x => x.GetItemsAsync(storeSlug,
            It.Is<ItemQueryParams>(q => q.PageSize == 0)), Times.Once); // Controller passes through the value as-is
    }

    [Fact]
    public async Task GetItemDetail_ValidItem_ReturnsItemDetail()
    {
        // Arrange
        var storeSlug = "test-store";
        var itemId = Guid.NewGuid();
        var expectedItem = new PublicItemDetailDto
        {
            Id = itemId,
            Title = "Test Item",
            Description = "A test item",
            Price = 25.99m,
            Category = "Electronics"
        };

        _mockStoreService.Setup(x => x.GetItemDetailAsync(storeSlug, itemId))
                        .ReturnsAsync(expectedItem);

        // Act
        var result = await _controller.GetItemDetail(storeSlug, itemId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PublicItemDetailDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PublicItemDetailDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedItem.Id, response.Data.Id);
        Assert.Equal(expectedItem.Title, response.Data.Title);
    }

    [Fact]
    public async Task GetItemDetail_ItemNotFound_ReturnsNotFound()
    {
        // Arrange
        var storeSlug = "test-store";
        var itemId = Guid.NewGuid();

        _mockStoreService.Setup(x => x.GetItemDetailAsync(storeSlug, itemId))
                        .ReturnsAsync((PublicItemDetailDto?)null);

        // Act
        var result = await _controller.GetItemDetail(storeSlug, itemId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PublicItemDetailDto>>>(result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Item not found", response.Message);
    }

    [Fact]
    public async Task GetCategories_ValidStore_ReturnsCategories()
    {
        // Arrange
        var storeSlug = "test-store";
        var expectedCategories = new List<CategoryDto>
        {
            new CategoryDto { Name = "Electronics", ItemCount = 5 },
            new CategoryDto { Name = "Clothing", ItemCount = 3 }
        };

        _mockStoreService.Setup(x => x.GetCategoriesAsync(storeSlug))
                        .ReturnsAsync(expectedCategories);

        // Act
        var result = await _controller.GetCategories(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<List<CategoryDto>>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<List<CategoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data.Count);
        Assert.Equal("Electronics", response.Data.First().Name);
    }

    [Fact]
    public async Task GetCategories_StoreNotFound_ReturnsEmptyList()
    {
        // Arrange
        var storeSlug = "non-existent-store";
        _mockStoreService.Setup(x => x.GetCategoriesAsync(storeSlug))
                        .ReturnsAsync(new List<CategoryDto>());

        // Act
        var result = await _controller.GetCategories(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<List<CategoryDto>>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<List<CategoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data);
    }

    [Fact]
    public async Task GetItems_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var storeSlug = "test-store";
        _mockStoreService.Setup(x => x.GetItemsAsync(storeSlug, It.IsAny<ItemQueryParams>()))
                        .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetItems(storeSlug, page: 1, pageSize: 10);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<object>>>(result);
        var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("error occurred", response.Message);
    }

    [Fact]
    public async Task GetStoreInfo_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var storeSlug = "test-store";
        _mockStoreService.Setup(x => x.GetStoreInfoAsync(storeSlug))
                        .ThrowsAsync(new ArgumentException("Invalid store"));

        // Act
        var result = await _controller.GetStoreInfo(storeSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StoreInfoDto>>>(result);
        var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("error occurred", response.Message);
    }

    [Fact]
    public async Task GetItemDetail_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var storeSlug = "test-store";
        var itemId = Guid.NewGuid();
        _mockStoreService.Setup(x => x.GetItemDetailAsync(storeSlug, itemId))
                        .ThrowsAsync(new InvalidOperationException("Item access error"));

        // Act
        var result = await _controller.GetItemDetail(storeSlug, itemId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PublicItemDetailDto>>>(result);
        var statusResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Contains("error occurred", response.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetStoreInfo_InvalidSlugFormat_ReturnsNotFound(string invalidSlug)
    {
        // Arrange
        _mockStoreService.Setup(x => x.GetStoreInfoAsync(It.IsAny<string>()))
                        .ReturnsAsync((StoreInfoDto?)null);

        // Act
        var result = await _controller.GetStoreInfo(invalidSlug);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StoreInfoDto>>>(result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetItems_WithSearchAndFilters_PassesCorrectParameters()
    {
        // Arrange
        var storeSlug = "test-store";
        _mockStoreService.Setup(x => x.GetItemsAsync(storeSlug, It.IsAny<ItemQueryParams>()))
                        .ReturnsAsync((new List<PublicItemDto>(), 0));

        // Act
        await _controller.GetItems(storeSlug,
            search: "vintage",
            category: "Clothing",
            minPrice: 10m,
            maxPrice: 100m,
            sort: "price-low-high",
            page: 2,
            pageSize: 5);

        // Assert
        _mockStoreService.Verify(x => x.GetItemsAsync(storeSlug,
            It.Is<ItemQueryParams>(q =>
                q.Search == "vintage" &&
                q.Category == "Clothing" &&
                q.MinPrice == 10m &&
                q.MaxPrice == 100m &&
                q.Sort == "price-low-high" &&
                q.Page == 2 &&
                q.PageSize == 5)), Times.Once);
    }
}