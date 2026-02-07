using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class SplitCalculationServiceTests : IDisposable
{
    private readonly SplitCalculationService _service;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;

    public SplitCalculationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _service = new SplitCalculationService(_context);
    }

    [Theory]
    [InlineData(100.00, 50.00, 50.00, 50.00)]
    [InlineData(99.99, 60.00, 59.99, 40.00)]
    [InlineData(150.00, 40.00, 60.00, 90.00)]
    [InlineData(25.75, 30.00, 7.72, 18.03)]
    public void CalculateSplit_VariousInputs_ReturnsCorrectSplit(
        decimal salePrice,
        decimal splitPercentage,
        decimal expectedProviderAmount,
        decimal expectedShopAmount)
    {
        // Act
        var result = _service.CalculateSplit(salePrice, splitPercentage);

        // Assert
        Assert.Equal(expectedProviderAmount, result.ProviderAmount);
        Assert.Equal(expectedShopAmount, result.ShopAmount);
        Assert.Equal(splitPercentage, result.SplitPercentage);
        Assert.Equal(salePrice, result.ProviderAmount + result.ShopAmount);
    }

    [Fact]
    public async Task CalculatePayoutsAsync_WithTransactions_ReturnsCorrectSummary()
    {
        // Arrange
        var organization = new Organization
        {
            Name = "Test Shop",
            VerticalType = VerticalType.Consignment
        };
        _context.Organizations.Add(organization);

        var provider = new Provider
        {
            OrganizationId = organization.Id,
            FirstName = "Test",
            LastName = "Provider",
            Email = "provider@test.com",
            DefaultSplitPercentage = 50.00m
        };
        _context.Providers.Add(provider);

        var item1 = new Item
        {
            OrganizationId = organization.Id,
            ProviderId = provider.Id,
            Sku = "ITEM001",
            Title = "Test Item 1",
            Price = 100.00m
        };
        var item2 = new Item
        {
            OrganizationId = organization.Id,
            ProviderId = provider.Id,
            Sku = "ITEM002",
            Title = "Test Item 2",
            Price = 75.00m
        };
        _context.Items.AddRange(item1, item2);

        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        var transaction1 = new Transaction
        {
            OrganizationId = organization.Id,
            ItemId = item1.Id,
            ProviderId = provider.Id,
            SalePrice = 100.00m,
            SaleDate = DateTime.UtcNow.AddDays(-15),
            ProviderSplitPercentage = 50.00m,
            ProviderAmount = 50.00m,
            ShopAmount = 50.00m
        };
        var transaction2 = new Transaction
        {
            OrganizationId = organization.Id,
            ItemId = item2.Id,
            ProviderId = provider.Id,
            SalePrice = 75.00m,
            SaleDate = DateTime.UtcNow.AddDays(-10),
            ProviderSplitPercentage = 60.00m,
            ProviderAmount = 45.00m,
            ShopAmount = 30.00m
        };
        _context.Transactions.AddRange(transaction1, transaction2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CalculatePayoutsAsync(provider.Id, periodStart, periodEnd);

        // Assert
        Assert.Equal(provider.Id, result.ProviderId);
        Assert.Equal("Test Provider", result.ProviderName);
        Assert.Equal(periodStart, result.PeriodStart);
        Assert.Equal(periodEnd, result.PeriodEnd);
        Assert.Equal(95.00m, result.TotalAmount); // 50.00 + 45.00
        Assert.Equal(2, result.TransactionCount);
        Assert.Equal(2, result.Transactions.Count);

        var firstTransaction = result.Transactions.First(t => t.ItemSku == "ITEM001");
        Assert.Equal(100.00m, firstTransaction.SalePrice);
        Assert.Equal(50.00m, firstTransaction.ProviderAmount);

        var secondTransaction = result.Transactions.First(t => t.ItemSku == "ITEM002");
        Assert.Equal(75.00m, secondTransaction.SalePrice);
        Assert.Equal(45.00m, secondTransaction.ProviderAmount);
    }

    [Fact]
    public async Task CalculatePayoutsAsync_NoTransactions_ReturnsEmptySummary()
    {
        // Arrange
        var organization = new Organization
        {
            Name = "Test Shop",
            VerticalType = VerticalType.Consignment
        };
        _context.Organizations.Add(organization);

        var provider = new Provider
        {
            OrganizationId = organization.Id,
            FirstName = "Empty",
            LastName = "Provider",
            Email = "empty@test.com",
            DefaultSplitPercentage = 50.00m
        };
        _context.Providers.Add(provider);

        await _context.SaveChangesAsync();

        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        // Act
        var result = await _service.CalculatePayoutsAsync(provider.Id, periodStart, periodEnd);

        // Assert
        Assert.Equal(provider.Id, result.ProviderId);
        Assert.Equal("Empty Provider", result.ProviderName);
        Assert.Equal(0m, result.TotalAmount);
        Assert.Equal(0, result.TransactionCount);
        Assert.Empty(result.Transactions);
    }

    [Fact]
    public async Task CalculatePayoutsAsync_NonExistentProvider_ThrowsArgumentException()
    {
        // Arrange
        var nonExistentProviderId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CalculatePayoutsAsync(nonExistentProviderId, periodStart, periodEnd));
    }

    [Fact]
    public async Task CalculatePayoutsAsync_TransactionsOutsidePeriod_ExcludesFromSummary()
    {
        // Arrange
        var organization = new Organization
        {
            Name = "Test Shop",
            VerticalType = VerticalType.Consignment
        };

        var provider = new Provider
        {
            Organization = organization,
            FirstName = "Test",
            LastName = "Provider",
            Email = "provider@test.com",
            DefaultSplitPercentage = 50.00m
        };

        var item = new Item
        {
            Organization = organization,
            Provider = provider,
            Sku = "ITEM001",
            Title = "Test Item",
            Price = 100.00m
        };

        // Add all entities at once
        _context.Organizations.Add(organization);
        _context.Providers.Add(provider);
        _context.Items.Add(item);

        // Single save - EF handles all relationships together
        await _context.SaveChangesAsync();

        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow.AddDays(-10);

        // Create a new context to avoid tracking issues
        _context.ChangeTracker.Clear();

        // Transaction inside period
        _context.Transactions.Add(new Transaction
        {
            OrganizationId = organization.Id,
            ItemId = item.Id,
            ProviderId = provider.Id,
            SalePrice = 100.00m,
            SaleDate = DateTime.UtcNow.AddDays(-20), // Inside period
            ProviderAmount = 50.00m,
            ShopAmount = 50.00m,
            ProviderSplitPercentage = 50.00m
        });

        // Transaction outside period
        _context.Transactions.Add(new Transaction
        {
            OrganizationId = organization.Id,
            ItemId = item.Id,
            ProviderId = provider.Id,
            SalePrice = 200.00m,
            SaleDate = DateTime.UtcNow.AddDays(-5), // Outside period (after periodEnd)
            ProviderAmount = 100.00m,
            ShopAmount = 100.00m,
            ProviderSplitPercentage = 50.00m
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CalculatePayoutsAsync(provider.Id, periodStart, periodEnd);

        // Assert
        Assert.Equal(50.00m, result.TotalAmount); // Only the transaction inside the period
        Assert.Equal(1, result.TransactionCount);
        Assert.Single(result.Transactions);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}