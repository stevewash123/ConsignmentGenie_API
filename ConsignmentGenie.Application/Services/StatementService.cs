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

    public async Task<StatementDto> GenerateStatementAsync(Guid consignorId, DateOnly periodStart, DateOnly periodEnd)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var consignor = await _context.Consignors
            .Include(p => p.Organization)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == consignorId);

        if (consignor == null)
            throw new ArgumentException("Consignor not found", nameof(consignorId));

        // Check if statement already exists for this period
        var existingStatement = await _context.Statements
            .FirstOrDefaultAsync(s => s.ConsignorId == consignorId
                && s.PeriodStart == periodStart
                && s.PeriodEnd == periodEnd);

        if (existingStatement != null)
        {
            return await MapToStatementDto(existingStatement);
        }

        // Calculate opening balance (unpaid earnings before this period)
        var openingBalance = await CalculateBalanceBeforePeriod(consignorId, periodStart);

        // Get transactions in this period
        var transactions = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ConsignorId == consignorId
                && t.TransactionDate >= periodStart.ToDateTime(TimeOnly.MinValue)
                && t.TransactionDate <= periodEnd.ToDateTime(TimeOnly.MaxValue)
                && t.Status == "Completed")
            .ToListAsync();

        // Get payouts in this period
        var payouts = await _context.Payouts
            .Where(p => p.ConsignorId == consignorId
                && p.PayoutDate >= periodStart.ToDateTime(TimeOnly.MinValue)
                && p.PayoutDate <= periodEnd.ToDateTime(TimeOnly.MaxValue)
                && p.Status == PayoutStatus.Paid)
            .ToListAsync();

        // Calculate totals
        var totalSales = transactions.Sum(t => t.SalePrice);
        var totalEarnings = transactions.Sum(t => t.ConsignorAmount);
        var totalPayouts = payouts.Sum(p => p.Amount);
        var closingBalance = openingBalance + totalEarnings - totalPayouts;

        // Generate statement number
        var statementNumber = GenerateStatementNumber(consignor.Organization, consignor, periodStart, periodEnd);

        // 🏗️ AGGREGATE ROOT PATTERN: Create statement aggregate root
        var statement = new Statement
        {
            OrganizationId = consignor.OrganizationId,
            ConsignorId = consignorId,
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

        _logger.LogInformation("Generated statement {StatementNumber} for provider {ConsignorId}", statementNumber, consignorId);

        return await MapToStatementDto(statement);
    }

    public async Task GenerateStatementsForMonthAsync(int year, int month)
    {
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        // Get all active providers
        var providers = await _context.Consignors
            .Where(p => p.Status == Core.Enums.ConsignorStatus.Approved)
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var consignorId in providers)
        {
            try
            {
                await GenerateStatementAsync(consignorId, periodStart, periodEnd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate statement for provider {ConsignorId} for period {Year}-{Month}", consignorId, year, month);
            }
        }

        _logger.LogInformation("Completed statement generation for {Year}-{Month}. Processed {ProviderCount} providers", year, month, providers.Count());
    }

    public async Task<List<StatementListDto>> GetStatementsAsync(Guid consignorId)
    {
        return await _context.Statements
            .Where(s => s.ConsignorId == consignorId)
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

    public async Task<StatementDto?> GetStatementAsync(Guid statementId, Guid consignorId)
    {
        var statement = await _context.Statements
            .FirstOrDefaultAsync(s => s.Id == statementId && s.ConsignorId == consignorId);

        return statement != null ? await MapToStatementDto(statement) : null;
    }

    public async Task<StatementDto?> GetStatementByPeriodAsync(Guid consignorId, DateOnly periodStart, DateOnly periodEnd)
    {
        var statement = await _context.Statements
            .FirstOrDefaultAsync(s => s.ConsignorId == consignorId
                && s.PeriodStart == periodStart
                && s.PeriodEnd == periodEnd);

        return statement != null ? await MapToStatementDto(statement) : null;
    }

    public async Task MarkAsViewedAsync(Guid statementId, Guid consignorId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var statement = await _context.Statements
            .FirstOrDefaultAsync(s => s.Id == statementId && s.ConsignorId == consignorId);

        if (statement != null && statement.ViewedAt == null)
        {
            statement.ViewedAt = DateTime.UtcNow;
            statement.Status = "Viewed";
            await _context.SaveChangesAsync();
        }
    }

    public async Task<byte[]> GeneratePdfAsync(Guid statementId, Guid consignorId)
    {
        // TODO: Implement PDF generation using QuestPDF
        var statement = await GetStatementAsync(statementId, consignorId);
        if (statement == null)
            throw new ArgumentException("Statement not found");

        // Placeholder - would implement with QuestPDF
        var pdfContent = System.Text.Encoding.UTF8.GetBytes($"Statement {statement.StatementNumber} PDF placeholder");

        _logger.LogWarning("PDF generation not yet implemented for statement {StatementId}", statementId);

        return pdfContent;
    }

    public async Task<StatementDto> RegenerateStatementAsync(Guid statementId, Guid consignorId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var existingStatement = await _context.Statements
            .FirstOrDefaultAsync(s => s.Id == statementId && s.ConsignorId == consignorId);

        if (existingStatement == null)
            throw new ArgumentException("Statement not found");

        // Remove existing statement
        _context.Statements.Remove(existingStatement);
        await _context.SaveChangesAsync();

        // Generate new statement for same period
        return await GenerateStatementAsync(consignorId, existingStatement.PeriodStart, existingStatement.PeriodEnd);
    }

    private async Task<decimal> CalculateBalanceBeforePeriod(Guid consignorId, DateOnly periodStart)
    {
        var periodStartDateTime = periodStart.ToDateTime(TimeOnly.MinValue);

        // Get all earnings before this period
        var totalEarnings = await _context.Transactions
            .Where(t => t.ConsignorId == consignorId
                && t.TransactionDate < periodStartDateTime
                && t.Status == "Completed")
            .SumAsync(t => t.ConsignorAmount);

        // Get all payouts before this period
        var totalPayouts = await _context.Payouts
            .Where(p => p.ConsignorId == consignorId
                && p.PayoutDate < periodStartDateTime
                && p.Status == PayoutStatus.Paid)
            .SumAsync(p => p.Amount);

        return totalEarnings - totalPayouts;
    }

    private string GenerateStatementNumber(Organization organization, Consignor consignor, DateOnly periodStart, DateOnly periodEnd)
    {
        // Format: STMT-2025-11-PRV00042
        return $"STMT-{periodStart.Year}-{periodStart.Month:D2}-PRV{consignor.Id.ToString()[..8].ToUpper()}";
    }

    private async Task<StatementDto> MapToStatementDto(Statement statement)
    {
        var consignor = await _context.Consignors
            .Include(p => p.Organization)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == statement.ConsignorId);

        // Get sales for this period
        var sales = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ConsignorId == statement.ConsignorId
                && t.TransactionDate >= statement.PeriodStart.ToDateTime(TimeOnly.MinValue)
                && t.TransactionDate <= statement.PeriodEnd.ToDateTime(TimeOnly.MaxValue)
                && t.Status == "Completed")
            .Select(t => new StatementSaleLineDto
            {
                Date = t.TransactionDate,
                ItemSku = t.Item.Sku ?? "",
                ItemTitle = t.Item.Title,
                SalePrice = t.SalePrice,
                CommissionRate = t.ConsignorSplitPercentage,
                EarningsAmount = t.ConsignorAmount
            })
            .ToListAsync();

        // Get payouts for this period
        var payouts = await _context.Payouts
            .Where(p => p.ConsignorId == statement.ConsignorId
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
            ConsignorName = consignor != null ? $"{consignor.FirstName} {consignor.LastName}" : "Unknown",
            ShopName = consignor?.Organization?.Name ?? "Unknown",
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