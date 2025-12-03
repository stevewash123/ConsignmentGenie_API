using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Items;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ItemsControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly ItemsController _controller;
        private readonly Mock<ILogger<ItemsController>> _loggerMock;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");
        private readonly Guid _userId = new("22222222-2222-2222-2222-222222222222");
        private readonly Guid _providerId = new("66666666-6666-6666-6666-666666666666");

        public ItemsControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<ItemsController>>();
            _controller = new ItemsController(_context, _loggerMock.Object);

            // Setup user claims
            var claims = new List<Claim>
            {
                new("organizationId", _organizationId.ToString()),
                new("userId", _userId.ToString()),
                new(ClaimTypes.Role, "Manager")
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
                Name = "Test Shop",
                Slug = "test-shop",
                CreatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);

            // Add provider
            var provider = new Consignor
            {
                Id = _providerId,
                OrganizationId = _organizationId,
                DisplayName = "Test Consignor",
                Email = "provider@test.com",
                DefaultSplitPercentage = 60.0m,
                Status = ConsignorStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Consignors.Add(provider);

            // Add category
            var category = new Category
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Name = "Clothing",
                CreatedAt = DateTime.UtcNow
            };
            _context.Categories.Add(category);

            // Add sample items
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _providerId,
                Sku = "ITEM001",
                Title = "Test Item 1",
                Description = "A test item",
                Category = "Clothing",
                Condition = ItemCondition.Good,
                Price = 25.99m,
                Status = ItemStatus.Available,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            var item2 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _providerId,
                Sku = "ITEM002",
                Title = "Test Item 2",
                Description = "Another test item",
                Category = "Clothing",
                Condition = ItemCondition.LikeNew,
                Price = 45.00m,
                Status = ItemStatus.Sold,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _userId
            };

            _context.Items.Add(item1);
            _context.Items.Add(item2);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetItems_ReturnsPagedResults()
        {
            // Arrange
            var queryParams = new ItemQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetItems(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ItemListDto>>(okResult.Value);
            Assert.Equal(2, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.Count);
        }

        [Fact]
        public async Task GetItems_WithStatusFilter_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = new ItemQueryParams
            {
                Page = 1,
                PageSize = 10,
                Status = "Available"
            };

            // Act
            var result = await _controller.GetItems(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ItemListDto>>(okResult.Value);
            Assert.Equal(1, pagedResult.TotalCount);
            Assert.Single(pagedResult.Items);
            Assert.Equal("Test Item 1", pagedResult.Items.First().Title);
        }

        [Fact]
        public async Task GetItems_WithSearchQuery_ReturnsMatchingItems()
        {
            // Arrange
            var queryParams = new ItemQueryParams
            {
                Page = 1,
                PageSize = 10,
                Search = "Test Item 2"
            };

            // Act
            var result = await _controller.GetItems(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ItemListDto>>(okResult.Value);
            Assert.Equal(1, pagedResult.TotalCount);
            Assert.Single(pagedResult.Items);
            Assert.Contains("Test Item 2", pagedResult.Items.First().Title);
        }

        [Fact]
        public async Task CreateItem_WithValidData_CreatesSuccessfully()
        {
            // Arrange
            var createRequest = new CreateItemRequest
            {
                ConsignorId = _providerId,
                Title = "New Test Item",
                Description = "A new test item",
                Category = "Clothing",
                Condition = ItemCondition.New,
                Price = 19.99m,
                Materials = "Cotton",
                Measurements = "Medium"
            };

            // Act
            var result = await _controller.CreateItem(createRequest);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDetailDto>>(createdAtActionResult.Value);
            Assert.True(response.Success);
            Assert.Equal("New Test Item", response.Data.Title);
            Assert.StartsWith("ITEM", response.Data.Sku);

            // Verify item was created in database
            var itemInDb = await _context.Items.FindAsync(response.Data.ItemId);
            Assert.NotNull(itemInDb);
            Assert.Equal("New Test Item", itemInDb.Title);
        }

        [Fact]
        public async Task CreateItem_WithInvalidProvider_ReturnsBadRequest()
        {
            // Arrange
            var invalidProviderId = Guid.NewGuid();
            var createRequest = new CreateItemRequest
            {
                ConsignorId = invalidProviderId,
                Title = "Invalid Consignor Item",
                Description = "This should fail",
                Category = "Clothing",
                Condition = ItemCondition.Good,
                Price = 29.99m
            };

            // Act
            var result = await _controller.CreateItem(createRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid provider", response.Errors);
        }

        [Fact]
        public async Task UpdateItemStatus_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var item = _context.Items.First(i => i.Status == ItemStatus.Available);
            var updateRequest = new UpdateItemStatusRequest
            {
                Status = "Removed",
                Reason = "Damaged"
            };

            // Act
            var result = await _controller.UpdateItemStatus(item.Id, updateRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDetailDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(ItemStatus.Removed, response.Data.Status);

            // Verify status was updated in database
            var itemInDb = await _context.Items.FindAsync(item.Id);
            Assert.Equal(ItemStatus.Removed, itemInDb!.Status);
        }

        [Fact]
        public async Task GenerateNextSku_GeneratesSequentialSkus()
        {
            // Arrange - Add an existing item with TEST prefix to test incremental behavior
            var existingItem = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _providerId,
                Sku = "TEST-00001",
                Title = "Existing Test Item",
                Price = 10.00m,
                Status = ItemStatus.Available,
                ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
                Condition = ItemCondition.Good
            };
            _context.Items.Add(existingItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GenerateSku("TEST");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<string>>(okResult.Value);

            Assert.True(response.Success);
            Assert.Equal("TEST-00002", response.Data);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}