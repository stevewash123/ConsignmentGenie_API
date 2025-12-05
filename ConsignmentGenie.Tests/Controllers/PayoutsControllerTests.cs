using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs.Payout;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;

namespace ConsignmentGenie.Tests.Controllers
{
    public class PayoutsControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly PayoutsController _controller;
        private readonly Guid _organizationId = Guid.NewGuid();
        private readonly Guid _consignorId = Guid.NewGuid();
        private readonly Guid _payoutId = Guid.NewGuid();

        public PayoutsControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _controller = new PayoutsController(_context);

            // Setup user claims
            var claims = new List<Claim>
            {
                new("OrganizationId", _organizationId.ToString()),
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

            // Add consignor
            var consignor = new Consignor
            {
                Id = _consignorId,
                OrganizationId = _organizationId,
                DisplayName = "Test Consignor",
                Email = "consignor@test.com",
                DefaultSplitPercentage = 60.0m,
                Status = ConsignorStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Consignors.Add(consignor);

            // Add items
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _consignorId,
                Sku = "ITEM001",
                Title = "Test Item 1",
                Price = 100.00m,
                Status = ItemStatus.Sold,
                Condition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow
            };

            var item2 = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _consignorId,
                Sku = "ITEM002",
                Title = "Test Item 2",
                Price = 80.00m,
                Status = ItemStatus.Sold,
                Condition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow
            };

            _context.Items.AddRange(item1, item2);

            // Add transactions
            var transaction1 = new Transaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _consignorId,
                ItemId = item1.Id,
                Item = item1,
                SaleDate = DateTime.UtcNow.AddDays(-10),
                SalePrice = 100.00m,
                ConsignorAmount = 60.00m,
                ShopAmount = 40.00m,
                PayoutStatus = "Pending",
                ConsignorPaidOut = false,
                CreatedAt = DateTime.UtcNow
            };

            var transaction2 = new Transaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = _consignorId,
                ItemId = item2.Id,
                Item = item2,
                SaleDate = DateTime.UtcNow.AddDays(-8),
                SalePrice = 80.00m,
                ConsignorAmount = 48.00m,
                ShopAmount = 32.00m,
                PayoutStatus = "Pending",
                ConsignorPaidOut = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.AddRange(transaction1, transaction2);

            // Add existing payout
            var existingPayout = new Payout
            {
                Id = _payoutId,
                OrganizationId = _organizationId,
                ConsignorId = _consignorId,
                PayoutNumber = "PAY-001",
                PayoutDate = DateTime.UtcNow.AddDays(-5),
                Amount = 108.00m,
                Status = PayoutStatus.Paid,
                PaymentMethod = "Check",
                PeriodStart = DateTime.UtcNow.AddDays(-15),
                PeriodEnd = DateTime.UtcNow.AddDays(-1),
                TransactionCount = 2,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payouts.Add(existingPayout);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetPayouts_ReturnsPagedResults()
        {
            // Arrange
            var request = new PayoutSearchRequestDto
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetPayouts(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            // Check that response has expected structure
            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");
            var totalCountProp = responseType.GetProperty("totalCount");

            Assert.NotNull(successProp);
            Assert.NotNull(dataProp);
            Assert.NotNull(totalCountProp);
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public async Task GetPayouts_WithConsignorId_ReturnsFilteredResults()
        {
            // Arrange
            var request = new PayoutSearchRequestDto
            {
                Page = 1,
                PageSize = 10,
                ConsignorId = _consignorId
            };

            // Act
            var result = await _controller.GetPayouts(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public async Task GetPayouts_WithStatus_ReturnsFilteredResults()
        {
            // Arrange
            var request = new PayoutSearchRequestDto
            {
                Page = 1,
                PageSize = 10,
                Status = PayoutStatus.Paid
            };

            // Act
            var result = await _controller.GetPayouts(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public async Task GetPayoutById_WithValidId_ReturnsPayout()
        {
            // Act
            var result = await _controller.GetPayoutById(_payoutId);

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
        public async Task GetPayoutById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _controller.GetPayoutById(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Payout not found", notFoundResult.Value);
        }

        [Fact]
        public async Task GetPendingPayouts_ReturnsPendingTransactions()
        {
            // Arrange
            var request = new PendingPayoutsRequestDto
            {
                ConsignorId = _consignorId
            };

            // Act
            var result = await _controller.GetPendingPayouts(request);

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
        public async Task GetPendingPayouts_WithMinimumAmount_ReturnsFilteredResults()
        {
            // Arrange
            var request = new PendingPayoutsRequestDto
            {
                ConsignorId = _consignorId,
                MinimumAmount = 50.00m
            };

            // Act
            var result = await _controller.GetPendingPayouts(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            Assert.True((bool)successProp.GetValue(response));
        }

        [Fact]
        public async Task CreatePayout_WithValidData_CreatesSuccessfully()
        {
            // Arrange
            var pendingTransactions = _context.Transactions
                .Where(t => t.PayoutStatus == "Pending")
                .ToList();

            var request = new CreatePayoutRequestDto
            {
                ConsignorId = _consignorId,
                TransactionIds = pendingTransactions.Select(t => t.Id).ToList(),
                PayoutDate = DateTime.UtcNow,
                PaymentMethod = "Direct Deposit",
                PaymentReference = "REF123",
                PeriodStart = DateTime.UtcNow.AddDays(-30),
                PeriodEnd = DateTime.UtcNow,
                Notes = "Test payout"
            };

            // Act
            var result = await _controller.CreatePayout(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var dataProp = responseType.GetProperty("data");

            Assert.True((bool)successProp.GetValue(response));
            Assert.NotNull(dataProp.GetValue(response));

            // Verify transactions were updated
            var updatedTransactions = await _context.Transactions
                .Where(t => pendingTransactions.Select(pt => pt.Id).Contains(t.Id))
                .ToListAsync();

            Assert.All(updatedTransactions, t =>
            {
                Assert.Equal("Paid", t.PayoutStatus);
                Assert.True(t.ConsignorPaidOut);
                Assert.NotNull(t.PayoutId);
            });
        }

        [Fact]
        public async Task CreatePayout_WithInvalidConsignor_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreatePayoutRequestDto
            {
                ConsignorId = Guid.NewGuid(), // Invalid consignor
                TransactionIds = new List<Guid> { Guid.NewGuid() },
                PayoutDate = DateTime.UtcNow,
                PaymentMethod = "Check",
                PeriodStart = DateTime.UtcNow.AddDays(-30),
                PeriodEnd = DateTime.UtcNow
            };

            // Act
            var result = await _controller.CreatePayout(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Consignor not found", badRequestResult.Value);
        }

        [Fact]
        public async Task CreatePayout_WithInvalidTransactions_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreatePayoutRequestDto
            {
                ConsignorId = _consignorId,
                TransactionIds = new List<Guid> { Guid.NewGuid() }, // Invalid transaction
                PayoutDate = DateTime.UtcNow,
                PaymentMethod = "Check",
                PeriodStart = DateTime.UtcNow.AddDays(-30),
                PeriodEnd = DateTime.UtcNow
            };

            // Act
            var result = await _controller.CreatePayout(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Some transactions are invalid or already paid out", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdatePayout_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var request = new UpdatePayoutRequestDto
            {
                PayoutDate = DateTime.UtcNow.AddDays(1),
                Status = PayoutStatus.Pending,
                PaymentReference = "UPDATED-REF",
                Notes = "Updated notes"
            };

            // Act
            var result = await _controller.UpdatePayout(_payoutId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var messageProp = responseType.GetProperty("message");

            Assert.True((bool)successProp.GetValue(response));
            Assert.Equal("Payout updated successfully", messageProp.GetValue(response));

            // Verify database was updated
            var updatedPayout = await _context.Payouts.FindAsync(_payoutId);
            Assert.NotNull(updatedPayout);
            Assert.Equal(PayoutStatus.Pending, updatedPayout.Status);
            Assert.Equal("UPDATED-REF", updatedPayout.PaymentReference);
            Assert.Equal("Updated notes", updatedPayout.Notes);
        }

        [Fact]
        public async Task UpdatePayout_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var request = new UpdatePayoutRequestDto
            {
                Notes = "Test notes"
            };

            // Act
            var result = await _controller.UpdatePayout(invalidId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Payout not found", notFoundResult.Value);
        }

        [Fact]
        public async Task DeletePayout_WithValidId_DeletesSuccessfully()
        {
            // Act
            var result = await _controller.DeletePayout(_payoutId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);

            var responseType = response.GetType();
            var successProp = responseType.GetProperty("success");
            var messageProp = responseType.GetProperty("message");

            Assert.True((bool)successProp.GetValue(response));
            Assert.Equal("Payout deleted successfully", messageProp.GetValue(response));

            // Verify payout was deleted
            var deletedPayout = await _context.Payouts.FindAsync(_payoutId);
            Assert.Null(deletedPayout);
        }

        [Fact]
        public async Task DeletePayout_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _controller.DeletePayout(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Payout not found", notFoundResult.Value);
        }

        [Fact]
        public async Task ExportPayoutToCsv_WithValidId_ReturnsFileResult()
        {
            // Act
            var result = await _controller.ExportPayoutToCsv(_payoutId);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv", fileResult.ContentType);
            Assert.StartsWith("payout-PAY-001", fileResult.FileDownloadName);
            Assert.True(fileResult.FileContents.Length > 0);
        }

        [Fact]
        public async Task ExportPayoutToCsv_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _controller.ExportPayoutToCsv(invalidId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Payout not found", notFoundResult.Value);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}