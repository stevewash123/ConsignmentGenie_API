using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;

namespace ConsignmentGenie.Tests.Controllers
{
    public class MobileControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<MobileController>> _mockLogger;
        private readonly Mock<IRepository<Item>> _mockItemRepository;
        private readonly Mock<IRepository<Provider>> _mockProviderRepository;
        private readonly Mock<IRepository<Transaction>> _mockTransactionRepository;
        private readonly MobileController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();
        private readonly Guid _providerId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();

        public MobileControllerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<MobileController>>();
            _mockItemRepository = new Mock<IRepository<Item>>();
            _mockProviderRepository = new Mock<IRepository<Provider>>();
            _mockTransactionRepository = new Mock<IRepository<Transaction>>();

            _mockUnitOfWork.Setup(u => u.Items).Returns(_mockItemRepository.Object);
            _mockUnitOfWork.Setup(u => u.Providers).Returns(_mockProviderRepository.Object);
            _mockUnitOfWork.Setup(u => u.Transactions).Returns(_mockTransactionRepository.Object);

            _controller = new MobileController(_mockUnitOfWork.Object, _mockLogger.Object);

            // Setup user claims for shop owner
            SetupUserClaims("Owner");
        }

        private void SetupUserClaims(string role, Guid? providerId = null)
        {
            var claims = new List<Claim>
            {
                new("OrganizationId", _organizationId.ToString()),
                new("userId", _userId.ToString()),
                new(ClaimTypes.Role, role)
            };

            if (providerId.HasValue)
            {
                claims.Add(new("ProviderId", providerId.Value.ToString()));
            }

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
            var controller = new MobileController(_mockUnitOfWork.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task GetMobileDashboard_WithShopOwnerRole_ReturnsShopOwnerDashboard()
        {
            // Arrange
            SetupUserClaims("Owner");

            _mockTransactionRepository.Setup(t => t.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(5);

            var todayTransactions = new List<Transaction>
            {
                new Transaction { SalePrice = 100.00m, SaleDate = DateTime.Today },
                new Transaction { SalePrice = 50.00m, SaleDate = DateTime.Today }
            };

            _mockTransactionRepository.Setup(t => t.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Transaction, bool>>>(), null))
                .ReturnsAsync(todayTransactions);

            _mockItemRepository.Setup(i => i.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>()))
                .ReturnsAsync(25);

            _mockProviderRepository.Setup(p => p.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Provider, bool>>>(), "Items"))
                .ReturnsAsync(new List<Provider>());

            // Act
            var result = await _controller.GetMobileDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Check response structure
            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.NotNull(successProp);
            Assert.NotNull(dataProp);
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public async Task GetMobileDashboard_WithProviderRole_ReturnsProviderDashboard()
        {
            // Arrange
            SetupUserClaims("Provider", _providerId);

            var provider = new Provider
            {
                Id = _providerId,
                DisplayName = "Test Provider",
                DefaultSplitPercentage = 60.0m,
                Items = new List<Item>
                {
                    new Item { Status = ItemStatus.Available, Price = 100.00m },
                    new Item { Status = ItemStatus.Sold, Price = 50.00m, UpdatedAt = DateTime.Now }
                },
                Payouts = new List<Payout>()
            };

            _mockProviderRepository.Setup(p => p.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Provider, bool>>>(), "Items,Payouts"))
                .ReturnsAsync(provider);

            // Act
            var result = await _controller.GetMobileDashboard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public async Task GetMobileDashboard_WithoutOrganizationId_ReturnsBadRequest()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            var result = await _controller.GetMobileDashboard();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Organization not found", badRequestResult.Value);
        }

        [Fact]
        public async Task GetQuickSaleData_ReturnsRecentItems()
        {
            // Arrange
            var items = new List<Item>
            {
                new Item
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Item 1",
                    Price = 100.00m,
                    Sku = "SKU001",
                    Status = ItemStatus.Available,
                    UpdatedAt = DateTime.UtcNow,
                    Provider = new Provider { DisplayName = "Provider 1" }
                },
                new Item
                {
                    Id = Guid.NewGuid(),
                    Title = "Test Item 2",
                    Price = 50.00m,
                    Sku = "SKU002",
                    Status = ItemStatus.Available,
                    UpdatedAt = DateTime.UtcNow.AddHours(-1),
                    Provider = new Provider { DisplayName = "Provider 2" }
                }
            };

            _mockItemRepository.Setup(i => i.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                "Provider"))
                .ReturnsAsync(items);

            // Act
            var result = await _controller.GetQuickSaleData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            Assert.NotNull(dataProp.GetValue(response));
        }

        [Fact]
        public async Task ProcessQuickSale_WithValidItem_ProcessesSale()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                Title = "Test Item",
                Price = 100.00m,
                Status = ItemStatus.Available,
                ProviderId = _providerId,
                Provider = new Provider
                {
                    Id = _providerId,
                    CommissionRate = 60.0m,
                    DefaultSplitPercentage = 60.0m
                }
            };

            var request = new QuickSaleRequest
            {
                ItemId = itemId,
                PaymentMethod = "Cash"
            };

            _mockItemRepository.Setup(i => i.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                "Provider"))
                .ReturnsAsync(item);

            _mockTransactionRepository.Setup(t => t.AddAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            _mockItemRepository.Setup(i => i.UpdateAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _controller.ProcessQuickSale(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            _mockTransactionRepository.Verify(t => t.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _mockItemRepository.Verify(i => i.UpdateAsync(It.IsAny<Item>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessQuickSale_WithUnavailableItem_ReturnsBadRequest()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                Status = ItemStatus.Sold // Item already sold
            };

            var request = new QuickSaleRequest
            {
                ItemId = itemId,
                PaymentMethod = "Cash"
            };

            _mockItemRepository.Setup(i => i.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                "Provider"))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.ProcessQuickSale(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Item not available for sale", badRequestResult.Value);
        }

        [Fact]
        public async Task BarcodeItemLookup_WithValidBarcode_ReturnsItem()
        {
            // Arrange
            var barcode = "TEST123";
            var item = new Item
            {
                Id = Guid.NewGuid(),
                Sku = barcode,
                Title = "Test Item",
                Price = 100.00m,
                Description = "Test Description",
                Status = ItemStatus.Available,
                Category = "Electronics",
                CreatedAt = DateTime.UtcNow,
                Provider = new Provider
                {
                    Id = _providerId,
                    DisplayName = "Test Provider",
                    DefaultSplitPercentage = 60.0m
                }
            };

            _mockItemRepository.Setup(i => i.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                "Provider,Photos"))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.BarcodeItemLookup(barcode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            Assert.NotNull(dataProp.GetValue(response));
        }

        [Fact]
        public async Task BarcodeItemLookup_WithInvalidBarcode_ReturnsNotFound()
        {
            // Arrange
            var barcode = "INVALID123";

            _mockItemRepository.Setup(i => i.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                "Provider,Photos"))
                .ReturnsAsync((Item)null);

            // Act
            var result = await _controller.BarcodeItemLookup(barcode);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Item not found or not available", notFoundResult.Value);
        }

        [Fact]
        public async Task GetRecentSales_ReturnsRecentTransactions()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    SalePrice = 100.00m,
                    SaleDate = DateTime.UtcNow,
                    PaymentMethod = "Cash",
                    Item = new Item { Title = "Test Item 1" },
                    Provider = new Provider { DisplayName = "Provider 1" }
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    SalePrice = 50.00m,
                    SaleDate = DateTime.UtcNow.AddHours(-1),
                    PaymentMethod = "Card",
                    Item = new Item { Title = "Test Item 2" },
                    Provider = new Provider { DisplayName = "Provider 2" }
                }
            };

            _mockTransactionRepository.Setup(t => t.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Transaction, bool>>>(),
                "Item,Provider"))
                .ReturnsAsync(transactions);

            // Act
            var result = await _controller.GetRecentSales(5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            Assert.NotNull(dataProp.GetValue(response));
        }

        [Fact]
        public async Task SyncOfflineData_WithValidData_ProcessesSales()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                Status = ItemStatus.Available,
                ProviderId = _providerId
            };

            var request = new OfflineSyncRequest
            {
                OfflineSales = new List<OfflineSaleData>
                {
                    new OfflineSaleData
                    {
                        ItemId = itemId,
                        Amount = 100.00m,
                        ShopAmount = 40.00m,
                        ProviderAmount = 60.00m,
                        SaleDate = DateTime.UtcNow,
                        PaymentMethod = "Cash"
                    }
                }
            };

            _mockItemRepository.Setup(i => i.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockTransactionRepository.Setup(t => t.AddAsync(It.IsAny<Transaction>()))
                .Returns(Task.CompletedTask);

            _mockItemRepository.Setup(i => i.UpdateAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _controller.SyncOfflineData(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            _mockTransactionRepository.Verify(t => t.AddAsync(It.IsAny<Transaction>()), Times.Once);
            _mockItemRepository.Verify(i => i.UpdateAsync(It.IsAny<Item>()), Times.Once);
        }

        [Fact]
        public async Task SyncOfflineData_WithUnavailableItem_ReturnsErrorsInResponse()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                Status = ItemStatus.Sold // Item already sold
            };

            var request = new OfflineSyncRequest
            {
                OfflineSales = new List<OfflineSaleData>
                {
                    new OfflineSaleData
                    {
                        ItemId = itemId,
                        Amount = 100.00m,
                        ShopAmount = 40.00m,
                        ProviderAmount = 60.00m,
                        SaleDate = DateTime.UtcNow,
                        PaymentMethod = "Cash"
                    }
                }
            };

            _mockItemRepository.Setup(i => i.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(0);

            // Act
            var result = await _controller.SyncOfflineData(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Verify no transactions were added
            _mockTransactionRepository.Verify(t => t.AddAsync(It.IsAny<Transaction>()), Times.Never);
        }
    }
}