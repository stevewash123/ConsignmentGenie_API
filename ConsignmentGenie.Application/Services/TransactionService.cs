using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Transaction;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IProviderNotificationService _notificationService;

    public TransactionService(
        ConsignmentGenieContext context,
        IProviderNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        Guid organizationId,
        TransactionQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.Item)
            .Include(t => t.Consignor)
            .Where(t => t.OrganizationId == organizationId);

        // Apply filters
        if (queryParams.StartDate.HasValue)
            query = query.Where(t => t.SaleDate >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(t => t.SaleDate <= queryParams.EndDate.Value);

        if (queryParams.ConsignorId.HasValue)
            query = query.Where(t => t.ConsignorId == queryParams.ConsignorId.Value);

        if (!string.IsNullOrEmpty(queryParams.PaymentMethod))
            query = query.Where(t => t.PaymentMethod == queryParams.PaymentMethod);

        // Source filtering removed for MVP - Phase 2+ feature

        // Apply sorting
        query = queryParams.SortBy?.ToLower() switch
        {
            "saleprice" => queryParams.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(t => t.SalePrice)
                : query.OrderByDescending(t => t.SalePrice),
            "provider" => queryParams.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(t => t.Consignor.GetDisplayName())
                : query.OrderByDescending(t => t.Consignor.GetDisplayName()),
            "item" => queryParams.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(t => t.Item.Title)
                : query.OrderByDescending(t => t.Item.Title),
            _ => queryParams.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(t => t.SaleDate)
                : query.OrderByDescending(t => t.SaleDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                SaleDate = t.SaleDate,
                SalePrice = t.SalePrice,
                SalesTaxAmount = t.SalesTaxAmount,
                PaymentMethod = t.PaymentMethod ?? string.Empty,
                // Source removed for MVP - Phase 2+ feature
                ConsignorSplitPercentage = t.ConsignorSplitPercentage,
                ConsignorAmount = t.ConsignorAmount,
                ShopAmount = t.ShopAmount,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt ?? t.CreatedAt,
                Item = new ItemSummaryDto
                {
                    Id = t.Item.Id,
                    Name = t.Item.Title,
                    Description = t.Item.Description,
                    OriginalPrice = t.Item.Price
                },
                Consignor = new ProviderSummaryDto
                {
                    Id = t.Consignor.Id,
                    Name = t.Consignor.GetDisplayName(),
                    Email = t.Consignor.Email
                }
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
            .Include(t => t.Item)
            .Include(t => t.Consignor)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == organizationId, cancellationToken);

        if (transaction == null)
            return null;

        return new TransactionDto
        {
            Id = transaction.Id,
            SaleDate = transaction.SaleDate,
            SalePrice = transaction.SalePrice,
            SalesTaxAmount = transaction.SalesTaxAmount,
            PaymentMethod = transaction.PaymentMethod ?? string.Empty,
            // Source removed for MVP - Phase 2+ feature
            ConsignorSplitPercentage = transaction.ConsignorSplitPercentage,
            ConsignorAmount = transaction.ConsignorAmount,
            ShopAmount = transaction.ShopAmount,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt,
            Item = new ItemSummaryDto
            {
                Id = transaction.Item.Id,
                Name = transaction.Item.Title,
                Description = transaction.Item.Description,
                OriginalPrice = transaction.Item.Price
            },
            Consignor = new ProviderSummaryDto
            {
                Id = transaction.Consignor.Id,
                Name = transaction.Consignor.GetDisplayName(),
                Email = transaction.Consignor.Email
            }
        };
    }

    public async Task<TransactionDto> CreateTransactionAsync(
        Guid organizationId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate item exists and is available
        var item = await _context.Items
            .Include(i => i.Consignor)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.OrganizationId == organizationId, cancellationToken);

        if (item == null)
            throw new InvalidOperationException("Item not found");

        if (item.Status != ItemStatus.Available)
            throw new InvalidOperationException($"Item is not available for sale. Current status: {item.Status}");

        // Get provider's commission rate
        var provider = item.Consignor;
        if (provider == null || provider.Status != ConsignorStatus.Active)
            throw new InvalidOperationException("Consignor not found or inactive");

        // Calculate commission split
        var providerAmount = request.SalePrice * (provider.CommissionRate / 100);
        var shopAmount = request.SalePrice - providerAmount;

        // Create transaction
        var transaction = new Core.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ItemId = request.ItemId,
            ConsignorId = provider.Id,
            SalePrice = request.SalePrice,
            SaleDate = request.SaleDate ?? DateTime.UtcNow,
            // Source removed for MVP - defaults to "Manual"
            PaymentMethod = request.PaymentMethod,
            SalesTaxAmount = request.SalesTaxAmount,
            ConsignorSplitPercentage = provider.CommissionRate,
            ConsignorAmount = providerAmount,
            ShopAmount = shopAmount,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);

        // Update item status to sold
        item.Status = ItemStatus.Sold;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Send notification to provider about the sale
        if (provider.UserId.HasValue)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                {
                    OrganizationId = organizationId,
                    UserId = provider.UserId.Value,
                ConsignorId = provider.Id,
                Type = NotificationType.ItemSold,
                Title = "Item Sold! 🎉",
                Message = $"Your item \"{item.Title}\" sold for {transaction.SalePrice:C}. Your cut: {transaction.ConsignorAmount:C}",
                RelatedEntityType = "Transaction",
                RelatedEntityId = transaction.Id,
                Metadata = new NotificationMetadata
                {
                    ItemTitle = item.Title,
                    ItemSku = item.Sku,
                    SalePrice = transaction.SalePrice,
                    EarningsAmount = transaction.ConsignorAmount
                }
            });
            }
            catch (Exception ex)
            {
                // Log but don't fail the transaction if notification fails
                // TODO: Add proper logging
            }
        }

        // Return the created transaction with navigation properties
        return new TransactionDto
        {
            Id = transaction.Id,
            SaleDate = transaction.SaleDate,
            SalePrice = transaction.SalePrice,
            SalesTaxAmount = transaction.SalesTaxAmount,
            PaymentMethod = transaction.PaymentMethod ?? string.Empty,
            // Source removed for MVP - Phase 2+ feature
            ConsignorSplitPercentage = transaction.ConsignorSplitPercentage,
            ConsignorAmount = transaction.ConsignorAmount,
            ShopAmount = transaction.ShopAmount,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt,
            Item = new ItemSummaryDto
            {
                Id = item.Id,
                Name = item.Title,
                Description = item.Description,
                OriginalPrice = item.Price
            },
            Consignor = new ProviderSummaryDto
            {
                Id = provider.Id,
                Name = provider.GetDisplayName(),
                Email = provider.Email
            }
        };
    }

    public async Task<TransactionDto?> UpdateTransactionAsync(
        Guid organizationId,
        Guid transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Item)
            .Include(t => t.Consignor)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == organizationId, cancellationToken);

        if (transaction == null)
            return null;

        // Update allowed fields
        if (!string.IsNullOrEmpty(request.PaymentMethod))
            transaction.PaymentMethod = request.PaymentMethod;

        if (request.Notes != null)
            transaction.Notes = request.Notes;

        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new TransactionDto
        {
            Id = transaction.Id,
            SaleDate = transaction.SaleDate,
            SalePrice = transaction.SalePrice,
            SalesTaxAmount = transaction.SalesTaxAmount,
            PaymentMethod = transaction.PaymentMethod ?? string.Empty,
            // Source removed for MVP - Phase 2+ feature
            ConsignorSplitPercentage = transaction.ConsignorSplitPercentage,
            ConsignorAmount = transaction.ConsignorAmount,
            ShopAmount = transaction.ShopAmount,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt,
            Item = new ItemSummaryDto
            {
                Id = transaction.Item.Id,
                Name = transaction.Item.Title,
                Description = transaction.Item.Description,
                OriginalPrice = transaction.Item.Price
            },
            Consignor = new ProviderSummaryDto
            {
                Id = transaction.Consignor.Id,
                Name = transaction.Consignor.GetDisplayName(),
                Email = transaction.Consignor.Email
            }
        };
    }

    public async Task<bool> DeleteTransactionAsync(
        Guid organizationId,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Item)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrganizationId == organizationId, cancellationToken);

        if (transaction == null)
            return false;

        // Revert item status back to Available
        transaction.Item.Status = ItemStatus.Available;
        transaction.Item.UpdatedAt = DateTime.UtcNow;

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
            .Include(t => t.Consignor)
            .Where(t => t.OrganizationId == organizationId);

        // Apply filters
        if (queryParams.StartDate.HasValue)
            query = query.Where(t => t.SaleDate >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(t => t.SaleDate <= queryParams.EndDate.Value);

        if (queryParams.ConsignorId.HasValue)
            query = query.Where(t => t.ConsignorId == queryParams.ConsignorId.Value);

        var transactions = await query.ToListAsync(cancellationToken);

        var totalSales = transactions.Sum(t => t.SalePrice);
        var totalShopAmount = transactions.Sum(t => t.ShopAmount);
        var totalProviderAmount = transactions.Sum(t => t.ConsignorAmount);
        var totalTax = transactions.Sum(t => t.SalesTaxAmount ?? 0);
        var transactionCount = transactions.Count;

        var topProviders = transactions
            .GroupBy(t => new { t.ConsignorId, ConsignorName = t.Consignor.FirstName + " " + t.Consignor.LastName })
            .Select(g => new ProviderSalesDto
            {
                ConsignorId = g.Key.ConsignorId,
                ConsignorName = g.Key.ConsignorName,
                TransactionCount = g.Count(),
                TotalSales = g.Sum(t => t.SalePrice),
                TotalProviderAmount = g.Sum(t => t.ConsignorAmount)
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(10)
            .ToList();

        var paymentMethodBreakdown = transactions
            .GroupBy(t => t.PaymentMethod ?? "Unknown")
            .Select(g => new PaymentMethodBreakdownDto
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                Total = g.Sum(t => t.SalePrice)
            })
            .OrderByDescending(p => p.Total)
            .ToList();

        return new SalesMetricsDto
        {
            TotalSales = totalSales,
            TotalShopAmount = totalShopAmount,
            TotalProviderAmount = totalProviderAmount,
            TotalTax = totalTax,
            TransactionCount = transactionCount,
            AverageTransactionValue = transactionCount > 0 ? totalSales / transactionCount : 0,
            TopProviders = topProviders,
            PaymentMethodBreakdown = paymentMethodBreakdown,
            PeriodStart = queryParams.StartDate,
            PeriodEnd = queryParams.EndDate
        };
    }
}