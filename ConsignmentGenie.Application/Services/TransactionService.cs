using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Transaction;
using ConsignmentGenie.Application.Helpers;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Constants;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IConsignorNotificationService _notificationService;
    private readonly IClearDateCalculator _clearDateCalculator;

    public TransactionService(
        ConsignmentGenieContext context,
        IConsignorNotificationService notificationService,
        IClearDateCalculator clearDateCalculator)
    {
        _context = context;
        _notificationService = notificationService;
        _clearDateCalculator = clearDateCalculator;
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        Guid organizationId,
        TransactionQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Consignor)
            .Where(t => t.OrganizationId == organizationId);

        // Apply filters
        if (queryParams.StartDate.HasValue)
            query = query.Where(t => t.TransactionDate >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(t => t.TransactionDate <= queryParams.EndDate.Value);

        if (queryParams.ConsignorId.HasValue)
            query = query.Where(t => t.Items.Any(ti => ti.ConsignorId == queryParams.ConsignorId.Value));

        if (!string.IsNullOrEmpty(queryParams.PaymentMethod))
            query = query.Where(t => t.PaymentType == queryParams.PaymentMethod);

        // Item search - searches item title and description
        if (!string.IsNullOrEmpty(queryParams.ItemSearch))
            query = query.Where(t => t.Items.Any(ti =>
                EF.Functions.ILike(ti.Item.Title, $"%{queryParams.ItemSearch}%") ||
                EF.Functions.ILike(ti.Item.Description ?? "", $"%{queryParams.ItemSearch}%")));

        // Consignor search - searches consignor first/last name
        if (!string.IsNullOrEmpty(queryParams.ConsignorSearch))
            query = query.Where(t => t.Items.Any(ti =>
                EF.Functions.ILike(ti.Consignor.Name, $"%{queryParams.ConsignorSearch}%") ||
                EF.Functions.ILike(ti.Consignor.PreferredName ?? "", $"%{queryParams.ConsignorSearch}%")));

        // Source filtering removed for MVP - Phase 2+ feature

        // Apply sorting
        query = queryParams.SortBy?.ToLower() switch
        {
            "total" => queryParams.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(t => t.Total)
                : query.OrderByDescending(t => t.Total),
            "subtotal" => queryParams.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(t => t.Subtotal)
                : query.OrderByDescending(t => t.Subtotal),
            _ => queryParams.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(t => t.TransactionDate)
                : query.OrderByDescending(t => t.TransactionDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                PaymentType = t.PaymentType,
                Source = t.Source,
                Subtotal = t.Subtotal,
                TaxAmount = t.TaxAmount,
                TaxRate = t.TaxRate,
                Total = t.Total,
                CustomerEmail = t.CustomerEmail,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt ?? t.CreatedAt,
                Items = t.Items.Select(ti => new TransactionItemDto
                {
                    Id = ti.Id,
                    Quantity = ti.Quantity,
                    UnitPrice = ti.UnitPrice,
                    LineTotal = ti.UnitPrice * ti.Quantity,
                    ConsignorSplitPercentage = ti.ConsignorSplitPercentage,
                    ConsignorAmount = ti.ConsignorAmount,
                    StoreAmount = ti.StoreAmount,
                    Item = new ItemSummaryDto
                    {
                        Id = ti.Item.Id,
                        Title = ti.Item.Title,
                        Sku = ti.Item.Sku,
                        Description = ti.Item.Description,
                        Price = ti.Item.Price
                    },
                    Consignor = new ConsignorSummaryDto
                    {
                        Id = ti.Consignor.Id,
                        Name = ti.Consignor.GetDisplayName(),
                        Email = ti.Consignor.Email
                    }
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionDto>
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            OrganizationId = organizationId
        };
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(
        Guid organizationId,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Consignor)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == organizationId, cancellationToken);

        if (transaction == null)
            return null;

        return new TransactionDto
        {
            Id = transaction.Id,
            TransactionDate = transaction.TransactionDate,
            PaymentType = transaction.PaymentType,
            Source = transaction.Source,
            Subtotal = transaction.Subtotal,
            TaxAmount = transaction.TaxAmount,
            TaxRate = transaction.TaxRate,
            Total = transaction.Total,
            CustomerEmail = transaction.CustomerEmail,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt,
            Items = transaction.Items.Select(ti => new TransactionItemDto
            {
                Id = ti.Id,
                Quantity = ti.Quantity,
                UnitPrice = ti.UnitPrice,
                LineTotal = ti.UnitPrice * ti.Quantity,
                ConsignorSplitPercentage = ti.ConsignorSplitPercentage,
                ConsignorAmount = ti.ConsignorAmount,
                StoreAmount = ti.StoreAmount,
                Item = new ItemSummaryDto
                {
                    Id = ti.Item.Id,
                    Title = ti.Item.Title,
                    Sku = ti.Item.Sku,
                    Description = ti.Item.Description,
                    Price = ti.Item.Price
                },
                Consignor = new ConsignorSummaryDto
                {
                    Id = ti.Consignor.Id,
                    Name = ti.Consignor.GetDisplayName(),
                    Email = ti.Consignor.Email
                }
            }).ToList()
        };
    }

    public async Task<TransactionDto> CreateTransactionAsync(
        Guid organizationId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate all items exist and are available
        var itemIds = request.Items.Select(i => i.ItemId).ToList();
        var items = await _context.Items
            .Include(i => i.Consignor)
            .Where(i => itemIds.Contains(i.Id) && i.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        if (items.Count != request.Items.Count)
        {
            var missingItemIds = itemIds.Except(items.Select(i => i.Id));
            throw new InvalidOperationException($"Items not found: {string.Join(", ", missingItemIds)}");
        }

        // Validate all items are available
        var unavailableItems = items.Where(i => i.Status != ItemStatus.Available).ToList();
        if (unavailableItems.Any())
        {
            var unavailableInfo = unavailableItems.Select(i => $"{i.Sku} ({i.Status})");
            throw new InvalidOperationException($"Items not available for sale: {string.Join(", ", unavailableInfo)}");
        }

        // Calculate transaction totals
        decimal subtotal = 0;
        var transactionItems = new List<TransactionItem>();

        foreach (var requestItem in request.Items)
        {
            var item = items.First(i => i.Id == requestItem.ItemId);
            var consignor = item.Consignor;

            if (consignor == null || consignor.Status != ConsignorStatus.Active)
                throw new InvalidOperationException($"Consignor for item {item.Sku} not found or inactive");

            var lineTotal = requestItem.UnitPrice * requestItem.Quantity;
            subtotal += lineTotal;

            // Calculate commission split for this item
            var consignorAmount = lineTotal * consignor.CommissionRate;
            var storeAmount = lineTotal - consignorAmount;

            var transactionItem = new TransactionItem
            {
                Id = Guid.NewGuid(),
                ItemId = requestItem.ItemId,
                OrganizationId = organizationId,
                ConsignorId = consignor.Id,
                Quantity = requestItem.Quantity,
                UnitPrice = requestItem.UnitPrice,
                ConsignorSplitPercentage = consignor.CommissionRate,
                ConsignorAmount = consignorAmount,
                StoreAmount = storeAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            transactionItems.Add(transactionItem);
        }

        var taxAmount = subtotal * request.TaxRate;
        var total = subtotal + taxAmount;

        // Get payout settings to calculate clear date
        var payoutSettings = await _context.PayoutSettings
            .FirstOrDefaultAsync(ps => ps.OrganizationId == organizationId, cancellationToken);

        if (payoutSettings == null)
        {
            throw new InvalidOperationException("Payout settings not found for organization");
        }

        var transactionDate = request.TransactionDate ?? DateTime.UtcNow;
        var clearDate = _clearDateCalculator.CalculateClearDate(transactionDate, request.PaymentType, payoutSettings);

        // Create transaction
        var transaction = new Core.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TransactionDate = transactionDate,
            PaymentType = request.PaymentType,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TaxRate = request.TaxRate,
            Total = total,
            CustomerEmail = request.CustomerEmail,
            Notes = request.Notes,
            Source = "Manual",
            ClearDate = clearDate,
            ProcessedByUserId = request.ProcessedByUserId,
            ProcessedByName = request.ProcessedByName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set the transaction ID for all transaction items
        foreach (var transactionItem in transactionItems)
        {
            transactionItem.TransactionId = transaction.Id;
        }

        _context.Transactions.Add(transaction);
        _context.TransactionItems.AddRange(transactionItems);

        // Update all item statuses to sold
        foreach (var item in items)
        {
            item.Status = ItemStatus.Sold;
            item.SoldDate = DateOnly.FromDateTime(transaction.TransactionDate);
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Send notifications to consignors about the sale
        foreach (var transactionItem in transactionItems)
        {
            var item = items.First(i => i.Id == transactionItem.ItemId);
            var consignor = item.Consignor;

            if (consignor?.UserId.HasValue == true)
            {
                try
                {
                    var notification = NotificationHelper.CreateItemSoldNotification(
                        transaction, item, consignor, transactionItem);

                    await _notificationService.CreateNotificationAsync(notification);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the transaction if notification fails
                    // TODO: Add proper logging
                }
            }
        }

        // Return the created transaction with navigation properties
        return new TransactionDto
        {
            Id = transaction.Id,
            TransactionDate = transaction.TransactionDate,
            PaymentType = transaction.PaymentType,
            Source = transaction.Source,
            Subtotal = transaction.Subtotal,
            TaxAmount = transaction.TaxAmount,
            TaxRate = transaction.TaxRate,
            Total = transaction.Total,
            CustomerEmail = transaction.CustomerEmail,
            Notes = transaction.Notes,
            Items = transactionItems.Select(ti =>
            {
                var item = items.First(i => i.Id == ti.ItemId);
                var consignor = item.Consignor!;
                return new TransactionItemDto
                {
                    Id = ti.Id,
                    Quantity = ti.Quantity,
                    UnitPrice = ti.UnitPrice,
                    LineTotal = ti.LineTotal,
                    ConsignorSplitPercentage = ti.ConsignorSplitPercentage,
                    ConsignorAmount = ti.ConsignorAmount,
                    StoreAmount = ti.StoreAmount,
                    Item = new ItemSummaryDto
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Sku = item.Sku,
                        Description = item.Description,
                        Price = item.Price
                    },
                    Consignor = new ConsignorSummaryDto
                    {
                        Id = consignor.Id,
                        Name = consignor.GetDisplayName(),
                        Email = consignor.Email
                    }
                };
            }).ToList(),
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt
        };
    }

    public async Task<TransactionDto?> UpdateTransactionAsync(
        Guid organizationId,
        Guid transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Consignor)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == organizationId, cancellationToken);

        if (transaction == null)
            return null;

        // Update allowed fields
        if (!string.IsNullOrEmpty(request.PaymentMethod))
            transaction.PaymentType = request.PaymentMethod;

        if (request.Notes != null)
            transaction.Notes = request.Notes;

        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new TransactionDto
        {
            Id = transaction.Id,
            TransactionDate = transaction.TransactionDate,
            PaymentType = transaction.PaymentType,
            Source = transaction.Source,
            Subtotal = transaction.Subtotal,
            TaxAmount = transaction.TaxAmount,
            TaxRate = transaction.TaxRate,
            Total = transaction.Total,
            CustomerEmail = transaction.CustomerEmail,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt,
            Items = transaction.Items.Select(ti => new TransactionItemDto
            {
                Id = ti.Id,
                Quantity = ti.Quantity,
                UnitPrice = ti.UnitPrice,
                LineTotal = ti.UnitPrice * ti.Quantity,
                ConsignorSplitPercentage = ti.ConsignorSplitPercentage,
                ConsignorAmount = ti.ConsignorAmount,
                StoreAmount = ti.StoreAmount,
                Item = new ItemSummaryDto
                {
                    Id = ti.Item.Id,
                    Title = ti.Item.Title,
                    Sku = ti.Item.Sku,
                    Description = ti.Item.Description,
                    Price = ti.Item.Price
                },
                Consignor = new ConsignorSummaryDto
                {
                    Id = ti.Consignor.Id,
                    Name = ti.Consignor.GetDisplayName(),
                    Email = ti.Consignor.Email
                }
            }).ToList()
        };
    }

    public async Task<bool> DeleteTransactionAsync(
        Guid organizationId,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == organizationId, cancellationToken);

        if (transaction == null)
            return false;

        // Revert all item statuses back to Available
        foreach (var transactionItem in transaction.Items)
        {
            transactionItem.Item.Status = ItemStatus.Available;
            transactionItem.Item.SoldDate = null;
            transactionItem.Item.UpdatedAt = DateTime.UtcNow;
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<SalesMetricsDto> GetSalesMetricsAsync(
        Guid organizationId,
        MetricsQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Consignor)
            .Where(t => t.OrganizationId == organizationId);

        // Apply filters
        if (queryParams.StartDate.HasValue)
            query = query.Where(t => t.TransactionDate >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(t => t.TransactionDate <= queryParams.EndDate.Value);

        if (queryParams.ConsignorId.HasValue)
            query = query.Where(t => t.Items.Any(ti => ti.ConsignorId == queryParams.ConsignorId.Value));

        var transactions = await query.ToListAsync(cancellationToken);

        var totalSales = transactions.Sum(t => t.Total);
        var totalShopAmount = transactions.SelectMany(t => t.Items).Sum(ti => ti.StoreAmount);
        var totalConsignorAmount = transactions.SelectMany(t => t.Items).Sum(ti => ti.ConsignorAmount);
        var totalTax = transactions.Sum(t => t.TaxAmount);
        var transactionCount = transactions.Count;

        var topConsignors = transactions
            .SelectMany(t => t.Items)
            .GroupBy(ti => new { ti.ConsignorId, ConsignorName = ti.Consignor.Name + " " + ti.Consignor })
            .Select(g => new ConsignorSalesDto
            {
                ConsignorId = g.Key.ConsignorId,
                ConsignorName = g.Key.ConsignorName,
                TransactionCount = g.Count(), // Count of items sold for this consignor
                TotalSales = g.Sum(ti => ti.UnitPrice * ti.Quantity),
                TotalConsignorAmount = g.Sum(ti => ti.ConsignorAmount)
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(10)
            .ToList();

        var paymentMethodBreakdown = transactions
            .GroupBy(t => t.PaymentType ?? "Unknown")
            .Select(g => new PaymentMethodBreakdownDto
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                Total = g.Sum(t => t.Total)
            })
            .OrderByDescending(p => p.Total)
            .ToList();

        return new SalesMetricsDto
        {
            TotalSales = totalSales,
            TotalShopAmount = totalShopAmount,
            TotalConsignorAmount = totalConsignorAmount,
            TotalTax = totalTax,
            TransactionCount = transactionCount,
            AverageTransactionValue = transactionCount > 0 ? totalSales / transactionCount : 0,
            TopConsignors = topConsignors,
            PaymentMethodBreakdown = paymentMethodBreakdown,
            PeriodStart = queryParams.StartDate,
            PeriodEnd = queryParams.EndDate
        };
    }
}