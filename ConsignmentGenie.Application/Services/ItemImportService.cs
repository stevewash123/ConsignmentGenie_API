using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Items;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace ConsignmentGenie.Application.Services;

public class ItemImportService : IItemImportService
{
    private readonly ConsignmentGenieContext _context;

    public ItemImportService(ConsignmentGenieContext context)
    {
        _context = context;
    }

    public async Task<ImportResultDto> ImportFromCsvAsync(Stream csvStream, Guid organizationId)
    {
        var result = new ImportResultDto();
        var errors = new List<ImportErrorDto>();
        var importedItems = new List<ImportedItemDto>();
        var rowNumber = 1; // Start from 1 for header

        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);

            // Read header
            await csv.ReadAsync();
            csv.ReadHeader();

            // Prepare lookup dictionaries
            var consignors = await _context.Consignors
                .Where(c => c.OrganizationId == organizationId)
                .ToDictionaryAsync(c => c.ConsignorNumber, c => c);

            var categories = await _context.ItemCategories
                .Where(c => c.OrganizationId == organizationId)
                .ToDictionaryAsync(c => c.Name, c => c);

            var existingSkus = new HashSet<string>(
                await _context.Items
                    .Where(i => i.OrganizationId == organizationId)
                    .Select(i => i.Sku)
                    .ToListAsync(),
                StringComparer.OrdinalIgnoreCase);

            var validItems = new List<Item>();
            var generatedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (await csv.ReadAsync())
            {
                rowNumber++;
                result.TotalRows++;

                try
                {
                    var rowData = GetRowData(csv);
                    var validationResult = await ValidateRowAsync(rowData, rowNumber, organizationId,
                        consignors, categories, existingSkus, generatedSkus);

                    if (!validationResult.IsValid)
                    {
                        errors.AddRange(validationResult.Errors);
                        result.ErrorCount++;
                        continue;
                    }

                    var item = CreateItemFromRow(validationResult.Data!, organizationId);
                    validItems.Add(item);

                    // Track generated SKUs to prevent duplicates in this import
                    generatedSkus.Add(item.Sku);
                    existingSkus.Add(item.Sku);

                    importedItems.Add(new ImportedItemDto
                    {
                        RowNumber = rowNumber,
                        ItemId = item.Id,
                        Name = item.Title,
                        SKU = item.Sku
                    });

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    errors.Add(new ImportErrorDto
                    {
                        RowNumber = rowNumber,
                        Field = "General",
                        Error = $"Unexpected error: {ex.Message}",
                        OriginalData = GetRowData(csv)
                    });
                    result.ErrorCount++;
                }
            }

            // Bulk insert valid items
            if (validItems.Any())
            {
                // Process in batches to avoid memory issues
                const int batchSize = 500;
                for (int i = 0; i < validItems.Count; i += batchSize)
                {
                    var batch = validItems.Skip(i).Take(batchSize);
                    _context.Items.AddRange(batch);
                    await _context.SaveChangesAsync();
                }
            }

            result.ImportedItems = importedItems;
            result.Errors = errors;

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"CSV import failed: {ex.Message}", ex);
        }
    }

    public byte[] GenerateTemplateAsync()
    {
        var csv = new StringBuilder();

        // Headers
        csv.AppendLine("Name,Description,SKU,Price,ConsignorNumber,Category,Condition,ReceivedDate,ExpirationDate,Location,Notes");

        // Sample data
        csv.AppendLine("\"Sample Item 1\",\"A beautiful handmade item\",\"\",25.99,\"001LLA\",\"Clothing\",\"Good\",\"2024-01-15\",\"\",\"Rack A1\",\"Perfect condition\"");
        csv.AppendLine("\"Sample Item 2\",\"Vintage collectible piece\",\"CUST-00001\",45.00,\"002LLB\",\"Collectibles\",\"LikeNew\",\"2024-01-16\",\"2024-12-31\",\"Display Case\",\"Limited edition\"");

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private Dictionary<string, string> GetRowData(CsvReader csv)
    {
        var data = new Dictionary<string, string>();

        foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
        {
            if (csv.TryGetField(header, out string? value))
            {
                data[header] = value ?? string.Empty;
            }
        }

        return data;
    }

    private async Task<RowValidationResult> ValidateRowAsync(
        Dictionary<string, string> rowData,
        int rowNumber,
        Guid organizationId,
        Dictionary<string, Consignor> consignors,
        Dictionary<string, ItemCategory> categories,
        HashSet<string> existingSkus,
        HashSet<string> generatedSkus)
    {
        var result = new RowValidationResult();
        var errors = new List<ImportErrorDto>();
        var validatedData = new ValidatedRowData();

        // Required: Name
        if (!rowData.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name))
        {
            errors.Add(CreateError(rowNumber, "Name", "Name is required", rowData));
        }
        else if (name.Length > 255)
        {
            errors.Add(CreateError(rowNumber, "Name", "Name cannot exceed 255 characters", rowData));
        }
        else
        {
            validatedData.Name = name;
        }

        // Required: Price
        if (!rowData.TryGetValue("Price", out var priceStr) || string.IsNullOrWhiteSpace(priceStr))
        {
            errors.Add(CreateError(rowNumber, "Price", "Price is required", rowData));
        }
        else if (!decimal.TryParse(priceStr, out var price) || price <= 0)
        {
            errors.Add(CreateError(rowNumber, "Price", "Price must be a positive number", rowData));
        }
        else
        {
            validatedData.Price = price;
        }

        // Required: ConsignorNumber
        if (!rowData.TryGetValue("ConsignorNumber", out var consignorNumber) || string.IsNullOrWhiteSpace(consignorNumber))
        {
            errors.Add(CreateError(rowNumber, "ConsignorNumber", "ConsignorNumber is required", rowData));
        }
        else if (!consignors.TryGetValue(consignorNumber, out var consignor))
        {
            errors.Add(CreateError(rowNumber, "ConsignorNumber", $"Consignor '{consignorNumber}' not found", rowData));
        }
        else
        {
            validatedData.Consignor = consignor;
        }

        // Optional: SKU (auto-generate if blank)
        if (rowData.TryGetValue("SKU", out var sku) && !string.IsNullOrWhiteSpace(sku))
        {
            if (existingSkus.Contains(sku) || generatedSkus.Contains(sku))
            {
                errors.Add(CreateError(rowNumber, "SKU", $"SKU '{sku}' already exists", rowData));
            }
            else
            {
                validatedData.Sku = sku;
            }
        }
        else
        {
            // Generate SKU
            validatedData.Sku = await GenerateUniqueSkuAsync(organizationId, existingSkus, generatedSkus);
        }

        // Optional: Category validation with auto-creation
        if (rowData.TryGetValue("Category", out var category) && !string.IsNullOrWhiteSpace(category))
        {
            if (!categories.ContainsKey(category))
            {
                // Auto-create missing category in ItemCategories table
                var newCategory = new ItemCategory
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Name = category,
                    Description = $"Auto-created category for '{category}'",
                    IsActive = true,
                    SortOrder = categories.Count + 1,
                    CreatedAt = DateTime.UtcNow
                };

                // Add to context and dictionary for immediate use
                _context.ItemCategories.Add(newCategory);
                categories[category] = newCategory;
            }

            // Use CategoryId (entity model uses CategoryId for ItemCategories FK)
            validatedData.CategoryId = categories[category].Id;
        }

        // Optional: Condition validation
        if (rowData.TryGetValue("Condition", out var condition) && !string.IsNullOrWhiteSpace(condition))
        {
            if (!Enum.TryParse<ItemCondition>(condition, true, out var conditionEnum))
            {
                // Clear invalid condition data instead of rejecting - default to Good
                validatedData.Condition = ItemCondition.Good;
            }
            else
            {
                validatedData.Condition = conditionEnum;
            }
        }
        else
        {
            validatedData.Condition = ItemCondition.Good; // Default
        }

        // Optional: Date validations
        if (rowData.TryGetValue("ReceivedDate", out var receivedDateStr) && !string.IsNullOrWhiteSpace(receivedDateStr))
        {
            if (!DateTime.TryParse(receivedDateStr, out var receivedDate))
            {
                errors.Add(CreateError(rowNumber, "ReceivedDate", "Invalid date format for ReceivedDate", rowData));
            }
            else
            {
                validatedData.ReceivedDate = DateOnly.FromDateTime(receivedDate);
            }
        }

        if (rowData.TryGetValue("ExpirationDate", out var expirationDateStr) && !string.IsNullOrWhiteSpace(expirationDateStr))
        {
            if (!DateTime.TryParse(expirationDateStr, out var expirationDate))
            {
                errors.Add(CreateError(rowNumber, "ExpirationDate", "Invalid date format for ExpirationDate", rowData));
            }
            else
            {
                var expDateOnly = DateOnly.FromDateTime(expirationDate);
                if (validatedData.ReceivedDate.HasValue && expDateOnly <= validatedData.ReceivedDate.Value)
                {
                    errors.Add(CreateError(rowNumber, "ExpirationDate", "ExpirationDate must be after ReceivedDate", rowData));
                }
                else
                {
                    validatedData.ExpirationDate = expDateOnly;
                }
            }
        }

        // Optional: Description (max length check)
        if (rowData.TryGetValue("Description", out var description) && !string.IsNullOrWhiteSpace(description))
        {
            if (description.Length > 2000)
            {
                errors.Add(CreateError(rowNumber, "Description", "Description cannot exceed 2000 characters", rowData));
            }
            else
            {
                validatedData.Description = description;
            }
        }

        // Optional: Location (max length check)
        if (rowData.TryGetValue("Location", out var location) && !string.IsNullOrWhiteSpace(location))
        {
            if (location.Length > 100)
            {
                errors.Add(CreateError(rowNumber, "Location", "Location cannot exceed 100 characters", rowData));
            }
            else
            {
                validatedData.Location = location;
            }
        }

        // Optional: Notes (max length check)
        if (rowData.TryGetValue("Notes", out var notes) && !string.IsNullOrWhiteSpace(notes))
        {
            if (notes.Length > 1000)
            {
                errors.Add(CreateError(rowNumber, "Notes", "Notes cannot exceed 1000 characters", rowData));
            }
            else
            {
                validatedData.Notes = notes;
            }
        }

        result.IsValid = !errors.Any();
        result.Errors = errors;
        result.Data = validatedData;

        return result;
    }

    private async Task<string> GenerateUniqueSkuAsync(Guid organizationId, HashSet<string> existingSkus, HashSet<string> generatedSkus)
    {
        const string prefix = "ITM";
        var attempts = 0;
        const int maxAttempts = 1000;

        while (attempts < maxAttempts)
        {
            var sku = await GenerateSkuAsync(organizationId, prefix);

            if (!existingSkus.Contains(sku) && !generatedSkus.Contains(sku))
            {
                return sku;
            }

            attempts++;
        }

        throw new InvalidOperationException("Unable to generate unique SKU after maximum attempts");
    }

    private async Task<string> GenerateSkuAsync(Guid organizationId, string? prefix = null)
    {
        prefix ??= "ITM";

        // Get highest existing SKU number for this prefix
        var lastSku = await _context.Items
            .Where(i => i.OrganizationId == organizationId && i.Sku.StartsWith(prefix + "-"))
            .OrderByDescending(i => i.Sku)
            .Select(i => i.Sku)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastSku != null)
        {
            var parts = lastSku.Split('-');
            if (parts.Length > 1 && int.TryParse(parts.Last(), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}-{nextNumber:D5}";
    }

    private ImportErrorDto CreateError(int rowNumber, string field, string error, Dictionary<string, string> rowData)
    {
        return new ImportErrorDto
        {
            RowNumber = rowNumber,
            Name = rowData.TryGetValue("Name", out var name) ? name : null,
            Field = field,
            Error = error,
            OriginalData = rowData
        };
    }

    private Item CreateItemFromRow(ValidatedRowData data, Guid organizationId)
    {
        return new Item
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ConsignorId = data.Consignor!.Id,
            Sku = data.Sku!,
            Title = data.Name!,
            Description = data.Description,
            ItemCategoryId = data.CategoryId,
            Price = data.Price!.Value,
            Condition = data.Condition!.Value,
            ReceivedDate = data.ReceivedDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ExpirationDate = data.ExpirationDate,
            Location = data.Location,
            Notes = data.Notes,
            Status = ItemStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private class RowValidationResult
    {
        public bool IsValid { get; set; }
        public List<ImportErrorDto> Errors { get; set; } = new();
        public ValidatedRowData? Data { get; set; }
    }

    private class ValidatedRowData
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public Consignor? Consignor { get; set; }
        public string? Sku { get; set; }
        public Guid? CategoryId { get; set; }
        public ItemCondition? Condition { get; set; }
        public string? Description { get; set; }
        public DateOnly? ReceivedDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }
}