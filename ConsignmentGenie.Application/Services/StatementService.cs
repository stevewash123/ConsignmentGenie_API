using ConsignmentGenie.Core.DTOs.Statements;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class StatementService : IStatementService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<StatementService> _logger;

    public StatementService(
        ConsignmentGenieContext context,
        ILogger<StatementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StatementDto> GenerateStatementAsync(Guid providerId, DateOnly periodStart, DateOnly periodEnd)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var provider = await _context.Providers
            .Include(p => p.Organization)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null)
            throw new ArgumentException("Provider not found", nameof(providerId));

        // Check if statement already exists for this period
        var existingStatement = await _context.Statements
            .FirstOrDefaultAsync(s => s.ProviderId == providerId
                && s.PeriodStart == periodStart
                && s.PeriodEnd == periodEnd);

        if (existingStatement != null)
        {
            return await MapToStatementDto(existingStatement);
        }

        // Calculate opening balance (unpaid earnings before this period)
        var openingBalance = await CalculateBalanceBeforePeriod(providerId, periodStart);

        // Get transactions in this period
        var transactions = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ProviderId == providerId
                && t.TransactionDate >= periodStart.ToDateTime(TimeOnly.MinValue)
                && t.TransactionDate <= periodEnd.ToDateTime(TimeOnly.MaxValue)
                && t.Status == "Completed")
            .ToListAsync();

        // Get payouts in this period
        var payouts = await _context.Payouts
            .Where(p => p.ProviderId == providerId
                && p.PayoutDate >= periodStart.ToDateTime(TimeOnly.MinValue)
                && p.PayoutDate <= periodEnd.ToDateTime(TimeOnly.MaxValue)
                && p.Status == PayoutStatus.Paid)
            .ToListAsync();

        // Calculate totals
        var totalSales = transactions.Sum(t => t.SalePrice);
        var totalEarnings = transactions.Sum(t => t.ProviderAmount);
        var totalPayouts = payouts.Sum(p => p.Amount);
        var closingBalance = openingBalance + totalEarnings - totalPayouts;

        // Generate statement number
        var statementNumber = GenerateStatementNumber(provider.Organization, provider, periodStart, periodEnd);

        // 🏗️ AGGREGATE ROOT PATTERN: Create statement aggregate root
        var statement = new Statement
        {
            OrganizationId = provider.OrganizationId,
            ProviderId = providerId,
            StatementNumber = statementNumber,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            OpeningBalance = openingBalance,
            TotalSales = totalSales,
            TotalEarnings = totalEarnings,
            TotalPayouts = totalPayouts,
            ClosingBalance = closingBalance,
            ItemsSold = transactions.Count,
            PayoutCount = payouts.Count,
            GeneratedAt = DateTime.UtcNow,
            Status = "Generated"
        };

        _context.Statements.Add(statement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated statement {StatementNumber} for provider {ProviderId}", statementNumber, providerId);

        return await MapToStatementDto(statement);
    }

    public async Task GenerateStatementsForMonthAsync(int year, int month)
    {
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Get all active providers
        var providers = await _context.Providers
            .Where(p => p.Status == Core.Enums.ProviderStatus.Approved)
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var providerId in providers)
        {
            try
            {
                await GenerateStatementAsync(providerId, periodStart, periodEnd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate statement for provider {ProviderId} for period {Year}-{Month}", providerId, year, month);
            }
        }

        _logger.LogInformation("Completed statement generation for {Year}-{Month}. Processed {ProviderCount} providers", year, month, providers.Count());
    }

    public async Task<List<StatementListDto>> GetStatementsAsync(Guid providerId)
    {
        return await _context.Statements
            .Where(s => s.ProviderId == providerId)
            .OrderByDescending(s => s.PeriodStart)
            .Select(s => new StatementListDto
            {
                StatementId = s.Id,
                StatementNumber = s.StatementNumber,
                PeriodStart = s.PeriodStart.ToDateTime(TimeOnly.MinValue),
                PeriodEnd = s.PeriodEnd.ToDateTime(TimeOnly.MinValue),
                PeriodLabel = s.PeriodStart.ToString("MMMM yyyy"),
                ItemsSold = s.ItemsSold,
                TotalEarnings = s.TotalEarnings,
                ClosingBalance = s.ClosingBalance,
                Status = s.Status,
                HasPdf = !string.IsNullOrEmpty(s.PdfUrl),
                GeneratedAt = s.GeneratedAt
            })
            .ToListAsync();
    }

    public async Task<StatementDto?> GetStatementAsync(Guid statementId, Guid providerId)
    {
        var statement = await _context.Statements
            .FirstOrDefaultAsync(s => s.Id == statementId && s.ProviderId == providerId);

        return statement != null ? await MapToStatementDto(statement) : null;
    }

    public async Task<StatementDto?> GetStatementByPeriodAsync(Guid providerId, DateOnly periodStart, DateOnly periodEnd)
    {
        var statement = await _context.Statements
            .FirstOrDefaultAsync(s => s.ProviderId == providerId
                && s.PeriodStart == periodStart
                && s.PeriodEnd == periodEnd);

        return statement != null ? await MapToStatementDto(statement) : null;
    }

    public async Task MarkAsViewedAsync(Guid statementId, Guid providerId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var statement = await _context.Statements
            .FirstOrDefaultAsync(s => s.Id == statementId && s.ProviderId == providerId);

        if (statement != null && statement.ViewedAt == null)
        {
            statement.ViewedAt = DateTime.UtcNow;
            statement.Status = "Viewed";
            await _context.SaveChangesAsync();
        }
    }

    public async Task<byte[]> GeneratePdfAsync(Guid statementId, Guid providerId)
    {
        // TODO: Implement PDF generation using QuestPDF
        var statement = await GetStatementAsync(statementId, providerId);
        if (statement == null)
            throw new ArgumentException("Statement not found");

        // Placeholder - would implement with QuestPDF
        var pdfContent = System.Text.Encoding.UTF8.GetBytes($"Statement {statement.StatementNumber} PDF placeholder");

        _logger.LogWarning("PDF generation not yet implemented for statement {StatementId}", statementId);

        return pdfContent;
    }

    public async Task<StatementDto> RegenerateStatementAsync(Guid statementId, Guid providerId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var existingStatement = await _context.Statements
            .FirstOrDefaultAsync(s => s.Id == statementId && s.ProviderId == providerId);

        if (existingStatement == null)
            throw new ArgumentException("Statement not found");

        // Remove existing statement
        _context.Statements.Remove(existingStatement);
        await _context.SaveChangesAsync();

        // Generate new statement for same period
        return await GenerateStatementAsync(providerId, existingStatement.PeriodStart, existingStatement.PeriodEnd);
    }

    private async Task<decimal> CalculateBalanceBeforePeriod(Guid providerId, DateOnly periodStart)
    {
        var periodStartDateTime = periodStart.ToDateTime(TimeOnly.MinValue);

        // Get all earnings before this period
        var totalEarnings = await _context.Transactions
            .Where(t => t.ProviderId == providerId
                && t.TransactionDate < periodStartDateTime
                && t.Status == "Completed")
            .SumAsync(t => t.ProviderAmount);

        // Get all payouts before this period
        var totalPayouts = await _context.Payouts
            .Where(p => p.ProviderId == providerId
                && p.PayoutDate < periodStartDateTime
                && p.Status == PayoutStatus.Paid)
            .SumAsync(p => p.Amount);

        return totalEarnings - totalPayouts;
    }

    private string GenerateStatementNumber(Organization organization, Provider provider, DateOnly periodStart, DateOnly periodEnd)
    {
        // Format: STMT-2025-11-PRV00042
        return $"STMT-{periodStart.Year}-{periodStart.Month:D2}-PRV{provider.Id.ToString()[..8].ToUpper()}";
    }

    private async Task<StatementDto> MapToStatementDto(Statement statement)
    {
        var provider = await _context.Providers
            .Include(p => p.Organization)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == statement.ProviderId);

        // Get sales for this period
        var sales = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ProviderId == statement.ProviderId
                && t.TransactionDate >= statement.PeriodStart.ToDateTime(TimeOnly.MinValue)
                && t.TransactionDate <= statement.PeriodEnd.ToDateTime(TimeOnly.MaxValue)
                && t.Status == "Completed")
            .Select(t => new StatementSaleLineDto
            {
                Date = t.TransactionDate,
                ItemSku = t.Item.Sku ?? "",
                ItemTitle = t.Item.Title,
                SalePrice = t.SalePrice,
                CommissionRate = t.ProviderSplitPercentage,
                EarningsAmount = t.ProviderAmount
            })
            .ToListAsync();

        // Get payouts for this period
        var payouts = await _context.Payouts
            .Where(p => p.ProviderId == statement.ProviderId
                && p.PayoutDate >= statement.PeriodStart.ToDateTime(TimeOnly.MinValue)
                && p.PayoutDate <= statement.PeriodEnd.ToDateTime(TimeOnly.MaxValue)
                && p.Status == PayoutStatus.Paid)
            .Select(p => new StatementPayoutLineDto
            {
                Date = p.PayoutDate,
                PayoutNumber = p.PayoutNumber,
                PaymentMethod = p.PaymentMethod.ToString(),
                Amount = p.Amount
            })
            .ToListAsync();

        return new StatementDto
        {
            Id = statement.Id,
            StatementNumber = statement.StatementNumber,
            PeriodStart = statement.PeriodStart,
            PeriodEnd = statement.PeriodEnd,
            PeriodLabel = statement.PeriodStart.ToString("MMMM yyyy"),
            ProviderName = provider != null ? $"{provider.FirstName} {provider.LastName}" : "Unknown",
            ShopName = provider?.Organization?.Name ?? "Unknown",
            OpeningBalance = statement.OpeningBalance,
            TotalSales = statement.TotalSales,
            TotalEarnings = statement.TotalEarnings,
            TotalPayouts = statement.TotalPayouts,
            ClosingBalance = statement.ClosingBalance,
            ItemsSold = statement.ItemsSold,
            PayoutCount = statement.PayoutCount,
            Sales = sales,
            Payouts = payouts,
            Status = statement.Status,
            HasPdf = !string.IsNullOrEmpty(statement.PdfUrl),
            PdfUrl = statement.PdfUrl,
            ViewedAt = statement.ViewedAt,
            GeneratedAt = statement.GeneratedAt
        };
    }
}