using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class PendingImportAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PendingImportAssignmentService> _logger;

    public PendingImportAssignmentService(
        IUnitOfWork unitOfWork,
        ILogger<PendingImportAssignmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all pending Square imports for an organization
    /// </summary>
    public async Task<IEnumerable<PendingSquareImport>> GetPendingImportsAsync(Guid organizationId)
    {
        return await _unitOfWork.PendingSquareImports.GetAllAsync(p => p.OrganizationId == organizationId);
    }

    /// <summary>
    /// Get count of pending imports for an organization
    /// </summary>
    public async Task<int> GetPendingCountAsync(Guid organizationId)
    {
        return await _unitOfWork.PendingSquareImports.CountAsync(p => p.OrganizationId == organizationId);
    }

    /// <summary>
    /// Assign a consignor to a pending import and promote it to the Items table
    /// </summary>
    public async Task<Item> AssignConsignorAsync(Guid pendingImportId, Guid consignorId)
    {
        var pendingImport = await _unitOfWork.PendingSquareImports.GetByIdAsync(pendingImportId);
        if (pendingImport == null)
        {
            throw new NotFoundException("Pending import not found");
        }

        var consignor = await _unitOfWork.Consignors.GetByIdAsync(consignorId);
        if (consignor == null)
        {
            throw new NotFoundException("Consignor not found");
        }

        if (consignor.OrganizationId != pendingImport.OrganizationId)
        {
            throw new ForbiddenException("Consignor belongs to different organization");
        }

        // Create item from pending import
        var item = new Item
        {
            Id = Guid.NewGuid(),
            OrganizationId = pendingImport.OrganizationId,
            ConsignorId = consignorId,
            SquareCatalogId = pendingImport.SquareCatalogId,
            SquareVariationId = pendingImport.SquareVariationId,
            Title = pendingImport.Name,
            Description = pendingImport.Description,
            Price = pendingImport.Price,
            Sku = pendingImport.Sku,
            Condition = ItemCondition.New, // Default condition for Square imports
            Status = ItemStatus.Available, // Now sellable
            ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsSquareManaged = true,
            SquareLastSyncAt = pendingImport.SquareUpdatedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add category if provided
        if (!string.IsNullOrWhiteSpace(pendingImport.Category))
        {
            var category = await _unitOfWork.ItemCategories.GetAsync(c =>
                c.OrganizationId == pendingImport.OrganizationId &&
                c.Name == pendingImport.Category);

            if (category != null)
            {
                item.ItemCategoryId = category.Id;
            }
        }

        // Add to Items table
        await _unitOfWork.Items.AddAsync(item);

        // Remove from pending table
        await _unitOfWork.PendingSquareImports.DeleteAsync(pendingImport);

        // Promote any pending transactions that reference this item
        await PromotePendingTransactionsForItemAsync(pendingImport.SquareCatalogId, item.Id, consignorId);

        // Save changes
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Assigned pending import {PendingImportId} to consignor {ConsignorId}, created item {ItemId}",
            pendingImportId, consignorId, item.Id);

        return item;
    }

    /// <summary>
    /// Bulk assign multiple pending imports to a consignor
    /// </summary>
    public async Task<BulkAssignResult> BulkAssignAsync(List<Guid> pendingImportIds, Guid consignorId)
    {
        var result = new BulkAssignResult();

        foreach (var pendingImportId in pendingImportIds)
        {
            try
            {
                await AssignConsignorAsync(pendingImportId, consignorId);
                result.Assigned++;
            }
            catch (Exception ex)
            {
                result.Failed.Add(new FailedAssignment
                {
                    PendingImportId = pendingImportId,
                    Reason = ex.Message
                });
                _logger.LogWarning(ex, "Failed to assign pending import {PendingImportId} to consignor {ConsignorId}",
                    pendingImportId, consignorId);
            }
        }

        _logger.LogInformation("Bulk assignment completed: {Assigned} assigned, {Failed} failed",
            result.Assigned, result.Failed.Count);

        return result;
    }

    /// <summary>
    /// Promote pending transactions that reference the newly assigned item
    /// </summary>
    private async Task PromotePendingTransactionsForItemAsync(string squareCatalogId, Guid itemId, Guid consignorId)
    {
        // Find pending transaction items that reference this Square item
        var pendingTransactionItems = await _unitOfWork.PendingSquareTransactionItems
            .GetAllAsync(pti => pti.SquareCatalogId == squareCatalogId && pti.PendingSquareImportId.HasValue);

        if (!pendingTransactionItems.Any())
        {
            return; // No pending transactions to promote
        }

        var consignor = await _unitOfWork.Consignors.GetByIdAsync(consignorId);
        if (consignor == null)
        {
            _logger.LogWarning("Consignor {ConsignorId} not found when promoting pending transactions", consignorId);
            return;
        }

        // Update the pending transaction items to reference the actual item
        foreach (var pendingItem in pendingTransactionItems)
        {
            pendingItem.ItemId = itemId;
            pendingItem.ConsignorId = consignorId;
            pendingItem.PendingSquareImportId = null;

            // Calculate split amounts
            pendingItem.ConsignorSplitPercentage = consignor.CommissionRate;
            pendingItem.ConsignorAmount = pendingItem.LineTotal * consignor.CommissionRate;
            pendingItem.StoreAmount = pendingItem.LineTotal * (1 - consignor.CommissionRate);

            await _unitOfWork.PendingSquareTransactionItems.UpdateAsync(pendingItem);
        }

        // Get unique transaction IDs that need to be checked for promotion
        var transactionIds = pendingTransactionItems.Select(pti => pti.PendingTransactionId).Distinct();

        foreach (var transactionId in transactionIds)
        {
            await CheckAndPromoteTransactionAsync(transactionId);
        }

        _logger.LogInformation("Updated {Count} pending transaction items for Square catalog ID {SquareCatalogId}",
            pendingTransactionItems.Count(), squareCatalogId);
    }

    /// <summary>
    /// Check if a pending transaction can be promoted (all items assigned) and promote it if so
    /// </summary>
    private async Task CheckAndPromoteTransactionAsync(Guid pendingTransactionId)
    {
        var pendingTransaction = await _unitOfWork.PendingSquareTransactions.GetByIdAsync(pendingTransactionId);
        if (pendingTransaction == null)
        {
            return;
        }

        // Get all items for this transaction
        var transactionItems = await _unitOfWork.PendingSquareTransactionItems
            .GetAllAsync(pti => pti.PendingTransactionId == pendingTransactionId);

        // Check if all items are now assigned (have ItemId)
        if (!transactionItems.All(pti => pti.ItemId.HasValue))
        {
            // Not all items assigned yet, update status and return
            pendingTransaction.Status = "Partial"; // Some items assigned, some not
            await _unitOfWork.PendingSquareTransactions.UpdateAsync(pendingTransaction);
            return;
        }

        // All items are assigned - promote to main Transaction table
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = pendingTransaction.OrganizationId,
            TransactionDate = pendingTransaction.TransactionDate,
            Source = pendingTransaction.Source,
            PaymentType = pendingTransaction.PaymentType,
            Subtotal = pendingTransaction.Subtotal,
            TaxAmount = pendingTransaction.TaxAmount,
            TaxRate = pendingTransaction.TaxRate,
            Total = pendingTransaction.Total,
            CustomerEmail = pendingTransaction.CustomerEmail,
            TaxCode = pendingTransaction.TaxCode,
            SquarePaymentId = pendingTransaction.SquarePaymentId,
            SquareLocationId = pendingTransaction.SquareLocationId,
            ImportedFromSquare = true,
            SquareCreatedAt = pendingTransaction.SquareCreatedAt,
            Notes = pendingTransaction.Notes,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Transactions.AddAsync(transaction);

        // Promote transaction items
        foreach (var pendingItem in transactionItems)
        {
            var transactionItem = new TransactionItem
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                OrganizationId = pendingItem.OrganizationId,
                ItemId = pendingItem.ItemId!.Value,
                ConsignorId = pendingItem.ConsignorId!.Value,
                Quantity = pendingItem.Quantity,
                UnitPrice = pendingItem.UnitPrice,
                ConsignorSplitPercentage = pendingItem.ConsignorSplitPercentage!.Value,
                ConsignorAmount = pendingItem.ConsignorAmount!.Value,
                StoreAmount = pendingItem.StoreAmount!.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TransactionItems.AddAsync(transactionItem);

            // Update item status to Sold
            var item = await _unitOfWork.Items.GetByIdAsync(pendingItem.ItemId.Value);
            if (item != null)
            {
                item.Status = ItemStatus.Sold;
                item.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Items.UpdateAsync(item);
            }
        }

        // Remove pending transaction and items
        foreach (var item in transactionItems)
        {
            await _unitOfWork.PendingSquareTransactionItems.DeleteAsync(item);
        }
        await _unitOfWork.PendingSquareTransactions.DeleteAsync(pendingTransaction);

        _logger.LogInformation("Promoted pending transaction {PendingTransactionId} to transaction {TransactionId}",
            pendingTransactionId, transaction.Id);
    }
}

/// <summary>
/// Result of bulk assignment operation
/// </summary>
public class BulkAssignResult
{
    public int Assigned { get; set; } = 0;
    public List<FailedAssignment> Failed { get; set; } = new List<FailedAssignment>();
}

/// <summary>
/// Details of a failed assignment
/// </summary>
public class FailedAssignment
{
    public Guid PendingImportId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Exception for not found resources
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Exception for forbidden operations
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}