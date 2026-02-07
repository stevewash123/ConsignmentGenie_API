using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class ItemImportServiceTests : IDisposable
{
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly ItemImportService _importService;
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _consignorId = Guid.NewGuid();

    public ItemImportServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _importService = new ItemImportService(_context);

        // Seed test data
        SetupTestDataAsync().GetAwaiter().GetResult();
    }

    private async Task SetupTestDataAsync()
    {
        var organization = new Organization
        {
            Id = _organizationId,
            Name = "Test Organization"
        };

        var consignor = new Consignor
        {
            Id = _consignorId,
            OrganizationId = _organizationId,
            ConsignorNumber = "001LLA",
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe"
        };

        var category = new ItemCategory
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            Name = "Clothing"
        };

        _context.Organizations.Add(organization);
        _context.Consignors.Add(consignor);
        _context.ItemCategories.Add(category);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task ImportFromCsvAsync_ValidCsv_ReturnsSuccessResults()
    {
        // Arrange
        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
Test Item 1,A test item,,25.99,001LLA,Clothing,Good,2024-01-15,,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Single(result.ImportedItems);
        Assert.Empty(result.Errors);

        var importedItem = result.ImportedItems[0];
        Assert.Equal(2, importedItem.RowNumber); // Row 2 (after header)
        Assert.Equal("Test Item 1", importedItem.Name);
        Assert.StartsWith("ITM-", importedItem.SKU);
    }

    [Fact]
    public async Task ImportFromCsvAsync_MissingRequiredFields_ReturnsValidationErrors()
    {
        // Arrange
        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
,A test item,,invalid_price,invalid_consignor,Clothing,Good,2024-01-15,,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Empty(result.ImportedItems);
        Assert.NotEmpty(result.Errors);

        // Should have multiple validation errors for this row
        var errors = result.Errors.Where(e => e.RowNumber == 2).ToList();
        Assert.True(errors.Count >= 3); // Name, Price, ConsignorNumber errors
        Assert.Contains(errors, e => e.Field == "Name" && e.Error == "Name is required");
        Assert.Contains(errors, e => e.Field == "Price" && e.Error == "Price must be a positive number");
        Assert.Contains(errors, e => e.Field == "ConsignorNumber" && e.Error == "Consignor 'invalid_consignor' not found");
    }

    [Fact]
    public async Task ImportFromCsvAsync_InvalidConsignorNumber_ReturnsValidationError()
    {
        // Arrange
        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
Test Item,A test item,,25.99,INVALID,Clothing,Good,2024-01-15,,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);

        var error = result.Errors.First();
        Assert.Equal(2, error.RowNumber);
        Assert.Equal("ConsignorNumber", error.Field);
        Assert.Equal("Consignor 'INVALID' not found", error.Error);
    }

    [Fact]
    public async Task ImportFromCsvAsync_DuplicateSku_ReturnsValidationError()
    {
        // Arrange - First create an item with a specific SKU
        var existingItem = new Item
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ConsignorId = _consignorId,
            Sku = "DUPLICATE-SKU",
            Title = "Existing Item",
            Price = 10.00m,
            Condition = ItemCondition.Good,
            Status = ItemStatus.Available
        };
        _context.Items.Add(existingItem);
        await _context.SaveChangesAsync();

        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
Test Item,A test item,DUPLICATE-SKU,25.99,001LLA,Clothing,Good,2024-01-15,,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);

        var error = result.Errors.First();
        Assert.Equal(2, error.RowNumber);
        Assert.Equal("SKU", error.Field);
        Assert.Equal("SKU 'DUPLICATE-SKU' already exists", error.Error);
    }

    [Fact]
    public async Task ImportFromCsvAsync_InvalidCondition_ReturnsValidationError()
    {
        // Arrange
        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
Test Item,A test item,,25.99,001LLA,Clothing,InvalidCondition,2024-01-15,,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);

        var error = result.Errors.First();
        Assert.Equal(2, error.RowNumber);
        Assert.Equal("Condition", error.Field);
        Assert.Contains("Invalid condition 'InvalidCondition'", error.Error);
    }

    [Fact]
    public async Task ImportFromCsvAsync_InvalidDateFormats_ReturnsValidationErrors()
    {
        // Arrange
        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
Test Item,A test item,,25.99,001LLA,Clothing,Good,invalid-date,another-invalid-date,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);

        var errors = result.Errors.Where(e => e.RowNumber == 2).ToList();
        Assert.Contains(errors, e => e.Field == "ReceivedDate" && e.Error == "Invalid date format for ReceivedDate");
        Assert.Contains(errors, e => e.Field == "ExpirationDate" && e.Error == "Invalid date format for ExpirationDate");
    }

    [Fact]
    public async Task ImportFromCsvAsync_ExpirationBeforeReceived_ReturnsValidationError()
    {
        // Arrange
        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
Test Item,A test item,,25.99,001LLA,Clothing,Good,2024-01-15,2024-01-10,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);

        var error = result.Errors.First();
        Assert.Equal(2, error.RowNumber);
        Assert.Equal("ExpirationDate", error.Field);
        Assert.Equal("ExpirationDate must be after ReceivedDate", error.Error);
    }

    [Fact]
    public async Task ImportFromCsvAsync_MaxLengthValidation_ReturnsValidationErrors()
    {
        // Arrange
        var longString = new string('A', 300); // Exceeds 255 char limit for Name
        var csvContent = $@"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
{longString},A test item,,25.99,001LLA,Clothing,Good,2024-01-15,,Rack A1,Test notes";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.ErrorCount);

        var error = result.Errors.First();
        Assert.Equal(2, error.RowNumber);
        Assert.Equal("Name", error.Field);
        Assert.Equal("Name cannot exceed 255 characters", error.Error);
    }

    [Fact]
    public void GenerateTemplateAsync_ReturnsValidCsvTemplate()
    {
        // Act
        var templateBytes = _importService.GenerateTemplateAsync();

        // Assert
        Assert.NotNull(templateBytes);
        Assert.True(templateBytes.Length > 0);

        var templateContent = Encoding.UTF8.GetString(templateBytes);
        Assert.Contains("Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes", templateContent);
        Assert.Contains("Sample Item 1", templateContent);
        Assert.Contains("Sample Item 2", templateContent);
    }

    [Fact]
    public async Task ImportFromCsvAsync_MultipleValidItems_ProcessesCorrectly()
    {
        // Arrange
        var csvContent = @"Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes
Test Item 1,First test item,,25.99,001LLA,Clothing,Good,2024-01-15,,Rack A1,Test notes 1
Test Item 2,Second test item,CUSTOM-001,15.50,001LLA,Clothing,LikeNew,2024-01-16,,Rack A2,Test notes 2";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportFromCsvAsync(stream, _organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(2, result.ImportedItems.Count);
        Assert.Empty(result.Errors);

        var item1 = result.ImportedItems.First(i => i.Name == "Test Item 1");
        var item2 = result.ImportedItems.First(i => i.Name == "Test Item 2");

        Assert.StartsWith("ITM-", item1.SKU);
        Assert.Equal("CUSTOM-001", item2.SKU);

        // Verify items were actually saved to database
        var savedItems = await _context.Items
            .Where(i => i.OrganizationId == _organizationId)
            .ToListAsync();

        Assert.Equal(2, savedItems.Count);
        Assert.Contains(savedItems, i => i.Title == "Test Item 1" && i.Price == 25.99m);
        Assert.Contains(savedItems, i => i.Title == "Test Item 2" && i.Price == 15.50m && i.Sku == "CUSTOM-001");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}