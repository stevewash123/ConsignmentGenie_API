using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Transaction;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ConsignmentGenieContext _context;

    public TransactionService(ConsignmentGenieContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        Guid organizationId,
        TransactionQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.Item)
            .Include(t => t.Provider)
            .Where(t => t.OrganizationId == organizationId);

        // Apply filters
        if (queryParams.StartDate.HasValue)
            query = query.Where(t => t.SaleDate >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(t => t.SaleDate <= queryParams.EndDate.Value);

        if (queryParams.ProviderId.HasValue)
            query = query.Where(t => t.ProviderId == queryParams.ProviderId.Value);

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
                ? query.OrderBy(t => t.Provider.GetDisplayName())
                : query.OrderByDescending(t => t.Provider.GetDisplayName()),
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
                ProviderSplitPercentage = t.ProviderSplitPercentage,
                ProviderAmount = t.ProviderAmount,
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
                Provider = new ProviderSummaryDto
                {
                    Id = t.Provider.Id,
                    Name = t.Provider.GetDisplayName(),
                    Email = t.Provider.Email
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
            .Include(t => t.Provider)
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
            ProviderSplitPercentage = transaction.ProviderSplitPercentage,
            ProviderAmount = transaction.ProviderAmount,
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
            Provider = new ProviderSummaryDto
            {
                Id = transaction.Provider.Id,
                Name = transaction.Provider.GetDisplayName(),
                Email = transaction.Provider.Email
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
            .Include(i => i.Provider)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.OrganizationId == organizationId, cancellationToken);

        if (item == null)
            throw new InvalidOperationException("Item not found");

        if (item.Status != ItemStatus.Available)
            throw new InvalidOperationException($"Item is not available for sale. Current status: {item.Status}");

        // Get provider's commission rate
        var provider = item.Provider;
        if (provider == null || provider.Status != ProviderStatus.Active)
            throw new InvalidOperationException("Provider not found or inactive");

        // Calculate commission split
        var providerAmount = request.SalePrice * (provider.CommissionRate / 100);
        var shopAmount = request.SalePrice - providerAmount;

        // Create transaction
        var transaction = new Core.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ItemId = request.ItemId,
            ProviderId = provider.Id,
            SalePrice = request.SalePrice,
            SaleDate = request.SaleDate ?? DateTime.UtcNow,
            // Source removed for MVP - defaults to "Manual"
            PaymentMethod = request.PaymentMethod,
            SalesTaxAmount = request.SalesTaxAmount,
            ProviderSplitPercentage = provider.CommissionRate,
            ProviderAmount = providerAmount,
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

        // Return the created transaction with navigation properties
        return new TransactionDto
        {
            Id = transaction.Id,
            SaleDate = transaction.SaleDate,
            SalePrice = transaction.SalePrice,
            SalesTaxAmount = transaction.SalesTaxAmount,
            PaymentMethod = transaction.PaymentMethod ?? string.Empty,
            // Source removed for MVP - Phase 2+ feature
            ProviderSplitPercentage = transaction.ProviderSplitPercentage,
            ProviderAmount = transaction.ProviderAmount,
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
            Provider = new ProviderSummaryDto
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
            .Include(t => t.Provider)
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
            ProviderSplitPercentage = transaction.ProviderSplitPercentage,
            ProviderAmount = transaction.ProviderAmount,
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
            Provider = new ProviderSummaryDto
            {
                Id = transaction.Provider.Id,
                Name = transaction.Provider.GetDisplayName(),
                Email = transaction.Provider.Email
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
            .Include(t => t.Provider)
            .Where(t => t.OrganizationId == organizationId);

        // Apply filters
        if (queryParams.StartDate.HasValue)
            query = query.Where(t => t.SaleDate >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(t => t.SaleDate <= queryParams.EndDate.Value);

        if (queryParams.ProviderId.HasValue)
            query = query.Where(t => t.ProviderId == queryParams.ProviderId.Value);

        var transactions = await query.ToListAsync(cancellationToken);

        var totalSales = transactions.Sum(t => t.SalePrice);
        var totalShopAmount = transactions.Sum(t => t.ShopAmount);
        var totalProviderAmount = transactions.Sum(t => t.ProviderAmount);
        var totalTax = transactions.Sum(t => t.SalesTaxAmount ?? 0);
        var transactionCount = transactions.Count;

        var topProviders = transactions
            .GroupBy(t => new { t.ProviderId, ProviderName = t.Provider.FirstName + " " + t.Provider.LastName })
            .Select(g => new ProviderSalesDto
            {
                ProviderId = g.Key.ProviderId,
                ProviderName = g.Key.ProviderName,
                TransactionCount = g.Count(),
                TotalSales = g.Sum(t => t.SalePrice),
                TotalProviderAmount = g.Sum(t => t.ProviderAmount)
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