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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Application.Services;
using System.Text;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ItemsControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly ItemsController _controller;
        private readonly Mock<ILogger<ItemsController>> _loggerMock;
        private readonly IItemImportService _importService;
        private readonly Mock<IFileUploadTrackingService> _fileTrackingServiceMock;
        private readonly Mock<PendingImportAssignmentService> _assignmentServiceMock;
        private readonly Mock<ConsignmentGenie.Core.Interfaces.INotificationService> _notificationServiceMock;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");
        private readonly Guid _userId = new("22222222-2222-2222-2222-222222222222");
        private readonly Guid _consignorId = new("66666666-6666-6666-6666-666666666666");

        public ItemsControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<ItemsController>>();
            _importService = new ItemImportService(_context);
            _fileTrackingServiceMock = new Mock<IFileUploadTrackingService>();
            _assignmentServiceMock = new Mock<PendingImportAssignmentService>(Mock.Of<ConsignmentGenie.Core.Interfaces.IUnitOfWork>(), Mock.Of<ILogger<PendingImportAssignmentService>>());
            _notificationServiceMock = new Mock<ConsignmentGenie.Core.Interfaces.INotificationService>();
            _controller = new ItemsController(_context, _loggerMock.Object, _importService, _fileTrackingServiceMock.Object, _assignmentServiceMock.Object, _notificationServiceMock.Object);

            // Setup user claims
            var claims = new List<Claim>
            {
                new("organizationId", _organizationId.ToString()),
                new(ClaimTypes.NameIdentifier, _userId.ToString()),
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

            // Add consignor
            var consignor = new Consignor
            {
                Id = _consignorId,
                OrganizationId = _organizationId,
                ConsignorNumber = "TEST-001",
                FirstName = "Test",
                LastName = "Consignor",
                DisplayName = "Test Consignor",
                Email = "consignor@test.com",
                DefaultSplitPercentage = 60.0m,
                Status = ConsignorStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Consignors.Add(consignor);

            // Add category
            var category = new ItemCategory
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Name = "Electronics",
                CreatedAt = DateTime.UtcNow
            };
            _context.ItemCategories.Add(category);

            // Add sample items
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _consignorId,
                Sku = "ITEM001",
                Title = "Test Item 1",
                Description = "A test item",
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
                ConsignorId = _consignorId,
                Sku = "ITEM002",
                Title = "Test Item 2",
                Description = "Another test item",
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
                ConsignorId = _consignorId,
                Title = "New Test Item",
                Description = "A new test item",
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
        public async Task CreateItem_WithInvalidConsignor_ReturnsBadRequest()
        {
            // Arrange
            var invalidConsignorId = Guid.NewGuid();
            var createRequest = new CreateItemRequest
            {
                ConsignorId = invalidConsignorId,
                Title = "Invalid Consignor Item",
                Description = "This should fail",
                Condition = ItemCondition.Good,
                Price = 29.99m
            };

            // Act
            var result = await _controller.CreateItem(createRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid consignor", response.Errors);
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
                ConsignorId = _consignorId,
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

        [Fact]
        public async Task CreateItem_DefaultsToGoodCondition()
        {
            // Arrange
            var createRequest = new CreateItemRequest
            {
                ConsignorId = _consignorId,
                Title = "Test Item",
                Price = 19.99m
                // Note: Condition not specified
            };

            // Act
            var result = await _controller.CreateItem(createRequest);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDetailDto>>(createdAtActionResult.Value);
            Assert.True(response.Success);
            Assert.Equal(ItemCondition.Good, response.Data.Condition);
            Assert.Equal("Good", response.Data.ConditionLabel);
        }

        [Fact]
        public async Task CreateItem_WithSpecifiedCondition_SavesCorrectCondition()
        {
            // Arrange
            var createRequest = new CreateItemRequest
            {
                ConsignorId = _consignorId,
                Title = "Test Item",
                Condition = ItemCondition.LikeNew,
                Price = 19.99m
            };

            // Act
            var result = await _controller.CreateItem(createRequest);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDetailDto>>(createdAtActionResult.Value);
            Assert.True(response.Success);
            Assert.Equal(ItemCondition.LikeNew, response.Data.Condition);
            Assert.Equal("Like New", response.Data.ConditionLabel);
        }

        [Fact]
        public async Task GetItems_ReturnsConditionAndConditionLabel()
        {
            // Arrange
            var queryParams = new ItemQueryParams
            {
                Page = 1,
                PageSize = 10,
                SortBy = "CreatedAt",
                SortDirection = "asc"
            };

            // Act
            var result = await _controller.GetItems(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ItemListDto>>(okResult.Value);

            var firstItem = pagedResult.Items.First();
            Assert.True(Enum.IsDefined(typeof(ItemCondition), firstItem.Condition));
            Assert.False(string.IsNullOrEmpty(firstItem.ConditionLabel));
        }

        [Fact]
        public async Task GetItem_ReturnsConditionAndConditionLabel()
        {
            // Arrange
            var items = await _context.Items.ToListAsync();
            var itemId = items.First().Id;

            // Act
            var result = await _controller.GetItem(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDetailDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.True(Enum.IsDefined(typeof(ItemCondition), response.Data.Condition));
            Assert.False(string.IsNullOrEmpty(response.Data.ConditionLabel));
        }

        [Fact]
        public async Task GetItems_WithConditionFilter_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = new ItemQueryParams
            {
                Page = 1,
                PageSize = 10,
                Condition = "Fair"
            };

            // Act
            var result = await _controller.GetItems(queryParams);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResult<ItemListDto>>(okResult.Value);

            // All returned items should have Fair condition
            Assert.All(pagedResult.Items, item => Assert.Equal(ItemCondition.Fair, item.Condition));
        }

        [Fact]
        public async Task ImportItems_ValidCsvFile_ReturnsSuccess()
        {
            // Arrange
            var csvContent = "Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes\nTest Item,A test description,,25.99,TEST-001,Electronics,Good,2024-01-15,,Shelf A,Test notes";
            var fileBytes = Encoding.UTF8.GetBytes(csvContent);

            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.FileName).Returns("test.csv");
            formFile.Setup(f => f.Length).Returns(fileBytes.Length);
            formFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileBytes));

            // Act
            var result = await _controller.ImportItems(formFile.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportResultDto>>(okResult.Value);
            Assert.True(response.Success);

            var importResult = response.Data;
            Assert.Equal(1, importResult.TotalRows);
            Assert.Equal(1, importResult.SuccessCount);
            Assert.Equal(0, importResult.ErrorCount);
            Assert.Single(importResult.ImportedItems);
        }

        [Fact]
        public async Task ImportItems_InvalidFileType_ReturnsBadRequest()
        {
            // Arrange
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.FileName).Returns("test.txt");
            formFile.Setup(f => f.Length).Returns(100);

            // Act
            var result = await _controller.ImportItems(formFile.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Only CSV files are supported", response.Errors);
        }

        [Fact]
        public async Task ImportItems_FileTooBig_ReturnsBadRequest()
        {
            // Arrange
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.FileName).Returns("test.csv");
            formFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB - exceeds 5MB limit

            // Act
            var result = await _controller.ImportItems(formFile.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("File size exceeds 5MB limit", response.Errors);
        }

        [Fact]
        public async Task ImportItems_NoFile_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.ImportItems(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("No file provided", response.Errors);
        }

        [Fact]
        public void GetImportTemplate_ReturnsFileResult()
        {
            // Act
            var result = _controller.GetImportTemplate();

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv", fileResult.ContentType);
            Assert.Equal("item_import_template.csv", fileResult.FileDownloadName);
            Assert.True(fileResult.FileContents.Length > 0);

            var content = Encoding.UTF8.GetString(fileResult.FileContents);
            Assert.Contains("Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes", content);
        }

        [Fact]
        public async Task ImportItems_InvalidCsvData_ReturnsErrorResults()
        {
            // Arrange
            var csvContent = "Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes\n,Invalid description,,invalid_price,INVALID_CONSIGNOR,Electronics,InvalidCondition,invalid_date,,Shelf A,Test notes";
            var fileBytes = Encoding.UTF8.GetBytes(csvContent);

            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.FileName).Returns("test.csv");
            formFile.Setup(f => f.Length).Returns(fileBytes.Length);
            formFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileBytes));

            // Act
            var result = await _controller.ImportItems(formFile.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportResultDto>>(okResult.Value);
            Assert.True(response.Success); // API call succeeds but import has errors

            var importResult = response.Data;
            Assert.Equal(1, importResult.TotalRows);
            Assert.Equal(0, importResult.SuccessCount);
            Assert.Equal(1, importResult.ErrorCount);
            Assert.NotEmpty(importResult.Errors);

            // Should have multiple validation errors for this row
            var errors = importResult.Errors.Where(e => e.RowNumber == 2).ToList();
            Assert.True(errors.Count >= 3); // Name, Price, ConsignorNumber errors expected
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}