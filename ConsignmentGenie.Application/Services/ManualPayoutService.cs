using ConsignmentGenie.Application.DTOs.Payout;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ConsignmentGenie.Application.Services;

/// <summary>
/// MVP Payout Service - Manual tracking only (no automation)
/// Owner manually pays providers and marks payouts as paid in the system
/// Phase 5+ will add automated PayPal/Stripe Connect payouts
/// </summary>
public class ManualPayoutService : IPayoutService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ManualPayoutService> _logger;
    private readonly IProviderNotificationService _notificationService;

    public ManualPayoutService(
        ConsignmentGenieContext context,
        ILogger<ManualPayoutService> logger,
        IProviderNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<PayoutReportDto> GeneratePayoutAsync(Guid providerId, DateTime startDate, DateTime endDate)
    {
        var provider = await _context.Consignors
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null)
            throw new ArgumentException($"Consignor {providerId} not found");

        // Get all unpaid transactions for this provider in the date range
        var transactions = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ConsignorId == providerId
                     && t.SaleDate >= startDate
                     && t.SaleDate <= endDate
                     && !t.ConsignorPaidOut)
            .OrderBy(t => t.SaleDate)
            .ToListAsync();

        var totalAmount = transactions.Sum(t => t.ConsignorAmount);
        var transactionCount = transactions.Count;

        return new PayoutReportDto
        {
            ConsignorId = providerId,
            ConsignorName = provider.GetDisplayName(),
            StartDate = startDate,
            EndDate = endDate,
            TotalAmount = totalAmount,
            TransactionCount = transactionCount,
            Status = "Pending",
            GeneratedAt = DateTime.UtcNow,
            Transactions = transactions.Select(t => new PayoutTransactionDto
            {
                TransactionId = t.Id,
                ItemName = t.Item.Title,
                SaleDate = t.SaleDate,
                SalePrice = t.SalePrice,
                ConsignorAmount = t.ConsignorAmount,
                ShopAmount = t.ShopAmount
            }).ToList()
        };
    }

    public async Task<List<PayoutReportDto>> GenerateAllPayoutsAsync(DateTime startDate, DateTime endDate)
    {
        var providers = await _context.Consignors
            .Where(p => p.Status == Core.Enums.ConsignorStatus.Active)
            .ToListAsync();

        var payouts = new List<PayoutReportDto>();

        foreach (var provider in providers)
        {
            var payout = await GeneratePayoutAsync(provider.Id, startDate, endDate);
            if (payout.TotalAmount > 0) // Only include providers with amounts owed
            {
                payouts.Add(payout);
            }
        }

        return payouts;
    }

    public async Task MarkPayoutAsPaidAsync(Guid payoutId, string paymentMethod, string? notes = null)
    {
        // In MVP, payoutId represents the provider ID for a date range
        // Mark all unpaid transactions for this provider as paid
        var transactions = await _context.Transactions
            .Where(t => t.ConsignorId == payoutId && !t.ConsignorPaidOut)
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            transaction.ConsignorPaidOut = true;
            transaction.ConsignorPaidOutDate = DateTime.UtcNow;
            transaction.PayoutMethod = paymentMethod;
            transaction.PayoutNotes = notes;
        }

        await _context.SaveChangesAsync();

        // Send notification to provider about the payout
        if (transactions.Any())
        {
            try
            {
                var provider = await _context.Consignors
                    .FirstOrDefaultAsync(p => p.Id == payoutId);

                if (provider != null && provider.UserId.HasValue)
                {
                    var totalAmount = transactions.Sum(t => t.ConsignorAmount);

                    await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                    {
                        OrganizationId = provider.OrganizationId,
                        UserId = provider.UserId.Value,
                        ConsignorId = provider.Id,
                        Type = NotificationType.PayoutProcessed,
                        Title = "Payout Processed 💰",
                        Message = $"A payout of {totalAmount:C} has been processed via {paymentMethod}.",
                        RelatedEntityType = "Payout",
                        RelatedEntityId = payoutId, // Using provider ID as payout identifier for MVP
                        Metadata = new NotificationMetadata
                        {
                            PayoutAmount = totalAmount,
                            PayoutMethod = paymentMethod,
                            PayoutNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{payoutId.ToString()[..8].ToUpper()}"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the payout if notification fails
                _logger.LogError(ex, "Failed to send payout notification for provider {ConsignorId}", payoutId);
            }
        }

        _logger.LogInformation(
            "[MANUAL PAYOUT] Marked {Count} transactions as paid for provider {ConsignorId}\n" +
            "  Payment Method: {PaymentMethod}\n" +
            "  Notes: {Notes}",
            transactions.Count, payoutId, paymentMethod, notes ?? "None"
        );
    }

    public async Task<byte[]> ExportPayoutToCsvAsync(Guid payoutId)
    {
        // For MVP, this would need the provider ID and date range
        // This is a simplified implementation
        var csv = new StringBuilder();
        csv.AppendLine("Consignor,Item,Sale Date,Sale Price,Consignor Amount,Shop Amount");
        csv.AppendLine($"Sample Consignor,Sample Item,{DateTime.Now:yyyy-MM-dd},100.00,50.00,50.00");

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportPayoutsToCsvAsync(List<Guid> payoutIds)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Consignor,Item,Sale Date,Sale Price,Consignor Amount,Shop Amount");

        foreach (var payoutId in payoutIds)
        {
            csv.AppendLine($"Consignor {payoutId},Sample Item,{DateTime.Now:yyyy-MM-dd},100.00,50.00,50.00");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<decimal> GetPendingPayoutAmountAsync(Guid providerId)
    {
        var pendingAmount = await _context.Transactions
            .Where(t => t.ConsignorId == providerId && !t.ConsignorPaidOut)
            .SumAsync(t => t.ConsignorAmount);

        return pendingAmount;
    }

    public async Task<List<PayoutSummaryDto>> GetPendingPayoutsAsync()
    {
        var pendingPayouts = await _context.Transactions
            .Include(t => t.Consignor)
            .Where(t => !t.ConsignorPaidOut)
            .GroupBy(t => t.ConsignorId)
            .Select(g => new PayoutSummaryDto
            {
                ConsignorId = g.Key,
                ConsignorName = g.First().Consignor.GetDisplayName(),
                PendingAmount = g.Sum(t => t.ConsignorAmount),
                TransactionCount = g.Count(),
                OldestTransaction = g.Min(t => t.SaleDate)
            })
            .ToListAsync();

        return pendingPayouts;
    }

    public async Task<PayoutResultDto> ProcessAutomatedPayoutAsync(Guid payoutId)
    {
        // MVP doesn't support automated payouts
        _logger.LogWarning("Automated payouts not supported in MVP. Use MarkPayoutAsPaidAsync for manual tracking.");

        throw new NotImplementedException("Automated payouts will be available in Phase 5+. Use manual payout tracking for MVP.");
    }
}