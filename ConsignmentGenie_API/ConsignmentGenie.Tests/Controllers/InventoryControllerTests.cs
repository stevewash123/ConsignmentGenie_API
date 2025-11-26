using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;

namespace ConsignmentGenie.Tests.Controllers
{
    public class InventoryControllerTests
    {
        private readonly InventoryController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();

        public InventoryControllerTests()
        {
            _controller = new InventoryController();

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
        public void Constructor_CreatesSuccessfully()
        {
            // Arrange & Act
            var controller = new InventoryController();

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void GetInventorySummary_ReturnsInventorySummary()
        {
            // Act
            var result = _controller.GetInventorySummary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Check response structure using reflection
            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.NotNull(successProp);
            Assert.NotNull(dataProp);
            Assert.True((bool)successProp.GetValue(response));

            var data = dataProp.GetValue(response);
            Assert.NotNull(data);

            // Check data structure
            var dataType = data.GetType();
            var totalValueProp = dataType.GetProperty("totalValue");
            var totalItemsProp = dataType.GetProperty("totalItems");
            var categoriesProp = dataType.GetProperty("categories");
            var recentActivityProp = dataType.GetProperty("recentActivity");

            Assert.NotNull(totalValueProp);
            Assert.NotNull(totalItemsProp);
            Assert.NotNull(categoriesProp);
            Assert.NotNull(recentActivityProp);

            Assert.Equal(42750.80m, totalValueProp.GetValue(data));
            Assert.Equal(342, totalItemsProp.GetValue(data));
        }

        [Fact]
        public void GetInventoryItems_WithDefaultParameters_ReturnsPagedItems()
        {
            // Act
            var result = _controller.GetInventoryItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Check response structure
            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");
            var paginationProp = responseType.GetProperty("pagination");

            Assert.NotNull(successProp);
            Assert.NotNull(dataProp);
            Assert.NotNull(paginationProp);
            Assert.True((bool)successProp.GetValue(response));

            var pagination = paginationProp.GetValue(response);
            Assert.NotNull(pagination);

            // Check pagination structure
            var paginationType = pagination.GetType();
            var pageProp = paginationType.GetProperty("page");
            var limitProp = paginationType.GetProperty("limit");

            Assert.Equal(1, pageProp.GetValue(pagination));
            Assert.Equal(20, limitProp.GetValue(pagination));
        }

        [Fact]
        public void GetInventoryItems_WithCategoryFilter_ReturnsFilteredItems()
        {
            // Act
            var result = _controller.GetInventoryItems(category: "Clothing");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public void GetInventoryItems_WithStatusFilter_ReturnsFilteredItems()
        {
            // Act
            var result = _controller.GetInventoryItems(status: "Available");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public void GetInventoryItems_WithProviderFilter_ReturnsFilteredItems()
        {
            // Act
            var result = _controller.GetInventoryItems(provider: "Sarah Johnson");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public void GetInventoryItems_WithCustomPagination_ReturnsPaginatedItems()
        {
            // Act
            var result = _controller.GetInventoryItems(page: 2, limit: 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var paginationProp = responseType.GetProperty("pagination");
            var pagination = paginationProp.GetValue(response);

            var paginationType = pagination.GetType();
            var pageProp = paginationType.GetProperty("page");
            var limitProp = paginationType.GetProperty("limit");

            Assert.Equal(2, pageProp.GetValue(pagination));
            Assert.Equal(1, limitProp.GetValue(pagination));
        }

        [Fact]
        public void GetInventoryItems_WithMultipleFilters_ReturnsFilteredItems()
        {
            // Act
            var result = _controller.GetInventoryItems(
                page: 1,
                limit: 10,
                category: "Jewelry",
                status: "Available"
            );

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public void GetCategories_ReturnsListOfCategories()
        {
            // Act
            var result = _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            var categories = dataProp.GetValue(response) as string[];
            Assert.NotNull(categories);
            Assert.Contains("All Categories", categories);
            Assert.Contains("Clothing", categories);
            Assert.Contains("Accessories", categories);
            Assert.Contains("Jewelry", categories);
        }

        [Fact]
        public void GetStatuses_ReturnsListOfStatuses()
        {
            // Act
            var result = _controller.GetStatuses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            var statuses = dataProp.GetValue(response) as string[];
            Assert.NotNull(statuses);
            Assert.Contains("All Statuses", statuses);
            Assert.Contains("Available", statuses);
            Assert.Contains("Sold", statuses);
            Assert.Contains("Hold", statuses);
        }

        [Fact]
        public void GetLowStockItems_WithDefaultThreshold_ReturnsLowStockItems()
        {
            // Act
            var result = _controller.GetLowStockItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            var lowStockItems = dataProp.GetValue(response);
            Assert.NotNull(lowStockItems);
        }

        [Fact]
        public void GetLowStockItems_WithCustomThreshold_ReturnsLowStockItems()
        {
            // Act
            var result = _controller.GetLowStockItems(threshold: 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            var lowStockItems = dataProp.GetValue(response);
            Assert.NotNull(lowStockItems);
        }
    }
}