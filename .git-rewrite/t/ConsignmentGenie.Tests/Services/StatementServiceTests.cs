using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class StatementServiceTests : IDisposable
{
    private readonly StatementService _service;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Mock<ILogger<StatementService>> _mockLogger;

    public StatementServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<StatementService>>();

        _service = new StatementService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateStatementAsync_WithValidData_CreatesStatement()
    {
        // Arrange
        var testData = await CreateTestDataAsync();
        var periodStart = new DateOnly(2023, 11, 1);
        var periodEnd = new DateOnly(2023, 11, 30);

        // Add some transactions in the period
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = testData.Organization.Id,
                ItemId = testData.Item1.Id,
                ProviderId = testData.Provider.Id,
                SalePrice = 100.00m,
                TransactionDate = new DateTime(2023, 11, 15),
                Status = "Completed",
                ProviderSplitPercentage = 60.00m,
                ProviderAmount = 60.00m,
                ShopAmount = 40.00m
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = testData.Organization.Id,
                ItemId = testData.Item2.Id,
                ProviderId = testData.Provider.Id,
                SalePrice = 50.00m,
                TransactionDate = new DateTime(2023, 11, 20),
                Status = "Completed",
                ProviderSplitPercentage = 60.00m,
                ProviderAmount = 30.00m,
                ShopAmount = 20.00m
            }
        };

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateStatementAsync(testData.Provider.Id, periodStart, periodEnd);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id); // Statement should have its own ID
        Assert.Equal(2023, result.PeriodStart.Year);
        Assert.Equal(11, result.PeriodStart.Month);
        Assert.Equal(1, result.PeriodStart.Day);
        Assert.Equal(2023, result.PeriodEnd.Year);
        Assert.Equal(11, result.PeriodEnd.Month);
        Assert.Equal(30, result.PeriodEnd.Day);
        Assert.Equal(150.00m, result.TotalSales);
        Assert.Equal(90.00m, result.TotalEarnings);
        Assert.Equal(2, result.ItemsSold);
        Assert.Contains("November 2023", result.PeriodLabel);
        Assert.Contains("STMT-2023-11-", result.StatementNumber);
    }

    [Fact]
    public async Task GenerateStatementAsync_WithExistingStatement_ReturnsExistingStatement()
    {
        // Arrange
        var testData = await CreateTestDataAsync();
        var periodStart = new DateOnly(2023, 11, 1);
        var periodEnd = new DateOnly(2023, 11, 30);

        var existingStatement = new Statement
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ProviderId = testData.Provider.Id,
            StatementNumber = "STMT-2023-11-TEST",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalSales = 100.00m,
            TotalEarnings = 60.00m,
            ItemsSold = 1,
            GeneratedAt = DateTime.UtcNow,
            Status = "Generated"
        };

        _context.Statements.Add(existingStatement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateStatementAsync(testData.Provider.Id, periodStart, periodEnd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingStatement.StatementNumber, result.StatementNumber);
        Assert.Equal(100.00m, result.TotalSales);
        Assert.Equal(60.00m, result.TotalEarnings);
    }

    [Fact]
    public async Task GenerateStatementAsync_WithPayouts_CalculatesCorrectBalance()
    {
        // Arrange
        var testData = await CreateTestDataAsync();
        var periodStart = new DateOnly(2023, 11, 1);
        var periodEnd = new DateOnly(2023, 11, 30);

        // Add transaction and payout
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ItemId = testData.Item1.Id,
            ProviderId = testData.Provider.Id,
            SalePrice = 100.00m,
            TransactionDate = new DateTime(2023, 11, 15),
            Status = "Completed",
            ProviderSplitPercentage = 60.00m,
            ProviderAmount = 60.00m,
            ShopAmount = 40.00m
        };

        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ProviderId = testData.Provider.Id,
            Amount = 30.00m,
            PayoutDate = new DateTime(2023, 11, 25),
            PayoutNumber = "PAY-001",
            Status = PayoutStatus.Paid,
            PaymentMethod = "Check"
        };

        _context.Transactions.Add(transaction);
        _context.Payouts.Add(payout);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateStatementAsync(testData.Provider.Id, periodStart, periodEnd);

        // Assert
        Assert.Equal(60.00m, result.TotalEarnings);
        Assert.Equal(30.00m, result.TotalPayouts);
        Assert.Equal(30.00m, result.ClosingBalance); // 0 + 60 - 30 = 30
        Assert.Equal(1, result.PayoutCount);
    }

    [Fact]
    public async Task GetStatementsAsync_WithMultipleStatements_ReturnsOrderedByDate()
    {
        // Arrange
        var testData = await CreateTestDataAsync();

        var statements = new List<Statement>
        {
            new Statement
            {
                Id = Guid.NewGuid(),
                OrganizationId = testData.Organization.Id,
                ProviderId = testData.Provider.Id,
                StatementNumber = "STMT-2023-10-TEST",
                PeriodStart = new DateOnly(2023, 10, 1),
                PeriodEnd = new DateOnly(2023, 10, 31),
                TotalEarnings = 100.00m,
                ClosingBalance = 50.00m,
                ItemsSold = 2,
                Status = "Generated",
                GeneratedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Statement
            {
                Id = Guid.NewGuid(),
                OrganizationId = testData.Organization.Id,
                ProviderId = testData.Provider.Id,
                StatementNumber = "STMT-2023-11-TEST",
                PeriodStart = new DateOnly(2023, 11, 1),
                PeriodEnd = new DateOnly(2023, 11, 30),
                TotalEarnings = 150.00m,
                ClosingBalance = 75.00m,
                ItemsSold = 3,
                Status = "Generated",
                GeneratedAt = DateTime.UtcNow
            }
        };

        _context.Statements.AddRange(statements);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetStatementsAsync(testData.Provider.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result[0].PeriodStart > result[1].PeriodStart); // Most recent first
        Assert.Equal("November 2023", result[0].PeriodLabel);
        Assert.Equal("October 2023", result[1].PeriodLabel);
    }

    [Fact]
    public async Task GetStatementAsync_WithValidId_ReturnsStatement()
    {
        // Arrange
        var testData = await CreateTestDataAsync();

        var statement = new Statement
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ProviderId = testData.Provider.Id,
            StatementNumber = "STMT-2023-11-TEST",
            PeriodStart = new DateOnly(2023, 11, 1),
            PeriodEnd = new DateOnly(2023, 11, 30),
            TotalSales = 100.00m,
            TotalEarnings = 60.00m,
            ItemsSold = 1,
            Status = "Generated",
            GeneratedAt = DateTime.UtcNow
        };

        _context.Statements.Add(statement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetStatementAsync(statement.Id, testData.Provider.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(statement.Id, result.Id);
        Assert.Equal(statement.StatementNumber, result.StatementNumber);
        Assert.Equal(testData.Provider.FirstName + " " + testData.Provider.LastName, result.ProviderName);
        Assert.Equal(testData.Organization.Name, result.ShopName);
    }

    [Fact]
    public async Task GetStatementAsync_WithWrongProviderId_ReturnsNull()
    {
        // Arrange
        var testData = await CreateTestDataAsync();
        var wrongProviderId = Guid.NewGuid();

        var statement = new Statement
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ProviderId = testData.Provider.Id,
            StatementNumber = "STMT-2023-11-TEST",
            PeriodStart = new DateOnly(2023, 11, 1),
            PeriodEnd = new DateOnly(2023, 11, 30),
            Status = "Generated",
            GeneratedAt = DateTime.UtcNow
        };

        _context.Statements.Add(statement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetStatementAsync(statement.Id, wrongProviderId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MarkAsViewedAsync_WithValidStatement_UpdatesViewedStatus()
    {
        // Arrange
        var testData = await CreateTestDataAsync();

        var statement = new Statement
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ProviderId = testData.Provider.Id,
            StatementNumber = "STMT-2023-11-TEST",
            PeriodStart = new DateOnly(2023, 11, 1),
            PeriodEnd = new DateOnly(2023, 11, 30),
            Status = "Generated",
            GeneratedAt = DateTime.UtcNow,
            ViewedAt = null
        };

        _context.Statements.Add(statement);
        await _context.SaveChangesAsync();

        // Act
        await _service.MarkAsViewedAsync(statement.Id, testData.Provider.Id);

        // Assert
        var updatedStatement = await _context.Statements.FindAsync(statement.Id);
        Assert.NotNull(updatedStatement.ViewedAt);
        Assert.Equal("Viewed", updatedStatement.Status);
    }

    [Fact]
    public async Task RegenerateStatementAsync_WithExistingStatement_ReplacesStatement()
    {
        // Arrange
        var testData = await CreateTestDataAsync();

        var existingStatement = new Statement
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ProviderId = testData.Provider.Id,
            StatementNumber = "STMT-2023-11-OLD",
            PeriodStart = new DateOnly(2023, 11, 1),
            PeriodEnd = new DateOnly(2023, 11, 30),
            TotalSales = 50.00m,
            TotalEarnings = 30.00m,
            ItemsSold = 1,
            Status = "Generated",
            GeneratedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Statements.Add(existingStatement);
        await _context.SaveChangesAsync();

        // Add new transaction to be included in regeneration
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            ItemId = testData.Item1.Id,
            ProviderId = testData.Provider.Id,
            SalePrice = 100.00m,
            TransactionDate = new DateTime(2023, 11, 15),
            Status = "Completed",
            ProviderSplitPercentage = 60.00m,
            ProviderAmount = 60.00m,
            ShopAmount = 40.00m
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegenerateStatementAsync(existingStatement.Id, testData.Provider.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(existingStatement.Id, result.Id); // New statement created
        Assert.Equal(100.00m, result.TotalSales); // Updated with new transaction
        Assert.Equal(60.00m, result.TotalEarnings);
        Assert.Equal(1, result.ItemsSold);

        // Original statement should be deleted
        var deletedStatement = await _context.Statements.FindAsync(existingStatement.Id);
        Assert.Null(deletedStatement);
    }

    [Fact]
    public async Task GenerateStatementsForMonthAsync_WithMultipleProviders_CreatesAllStatements()
    {
        // Arrange
        var testData = await CreateTestDataAsync();

        // Create a second provider
        var provider2 = new Provider
        {
            Id = Guid.NewGuid(),
            OrganizationId = testData.Organization.Id,
            UserId = testData.User.Id,
            ProviderNumber = "PRV-002",
            FirstName = "Second",
            LastName = "Provider",
            Email = "provider2@test.com",
            CommissionRate = 50.00m,
            Status = ProviderStatus.Approved
        };

        _context.Providers.Add(provider2);
        await _context.SaveChangesAsync();

        // Act
        await _service.GenerateStatementsForMonthAsync(2023, 11);

        // Assert
        var statements = await _context.Statements.ToListAsync();
        Assert.Equal(2, statements.Count);
        Assert.Contains(statements, s => s.ProviderId == testData.Provider.Id);
        Assert.Contains(statements, s => s.ProviderId == provider2.Id);
    }

    private async Task<TestData> CreateTestDataAsync()
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@test.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.Provider,
            OrganizationId = organization.Id
        };

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = user.Id,
            ProviderNumber = "PRV-001",
            FirstName = "Test",
            LastName = "Provider",
            Email = "provider@test.com",
            CommissionRate = 60.00m,
            Status = ProviderStatus.Approved
        };

        var item1 = new Item
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ProviderId = provider.Id,
            Title = "Test Item 1",
            Description = "Test description 1",
            Price = 100.00m,
            Status = ItemStatus.Available,
            Sku = "SKU-001"
        };

        var item2 = new Item
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ProviderId = provider.Id,
            Title = "Test Item 2",
            Description = "Test description 2",
            Price = 50.00m,
            Status = ItemStatus.Available,
            Sku = "SKU-002"
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        _context.Providers.Add(provider);
        _context.Items.AddRange(item1, item2);
        await _context.SaveChangesAsync();

        return new TestData
        {
            Organization = organization,
            User = user,
            Provider = provider,
            Item1 = item1,
            Item2 = item2
        };
    }

    private class TestData
    {
        public Organization Organization { get; set; }
        public User User { get; set; }
        public Provider Provider { get; set; }
        public Item Item1 { get; set; }
        public Item Item2 { get; set; }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}