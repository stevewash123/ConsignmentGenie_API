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
/// Owner manually pays consignors and marks payouts as paid in the system
/// Phase 5+ will add automated PayPal/Stripe Connect payouts
/// </summary>
public class ManualPayoutService : IPayoutService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<ManualPayoutService> _logger;
    private readonly IConsignorNotificationService _notificationService;

    public ManualPayoutService(
        ConsignmentGenieContext context,
        ILogger<ManualPayoutService> logger,
        IConsignorNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<PayoutReportDto> GeneratePayoutAsync(Guid consignorId, DateTime startDate, DateTime endDate)
    {
        var consignor = await _context.Consignors
            .FirstOrDefaultAsync(p => p.Id == consignorId);

        if (consignor == null)
            throw new ArgumentException($"Consignor {consignorId} not found");

        // Get all unpaid transactions for this consignor in the date range
        var transactions = await _context.Transactions
            .Include(t => t.Item)
            .Where(t => t.ConsignorId == consignorId
                     && t.SaleDate >= startDate
                     && t.SaleDate <= endDate
                     && !t.ConsignorPaidOut)
            .OrderBy(t => t.SaleDate)
            .ToListAsync();

        var totalAmount = transactions.Sum(t => t.ConsignorAmount);
        var transactionCount = transactions.Count;

        return new PayoutReportDto
        {
            ConsignorId = consignorId,
            ConsignorName = consignor.GetDisplayName(),
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
        var consignors = await _context.Consignors
            .Where(p => p.Status == Core.Enums.ConsignorStatus.Active)
            .ToListAsync();

        var payouts = new List<PayoutReportDto>();

        foreach (var consignor in consignors)
        {
            var payout = await GeneratePayoutAsync(consignor.Id, startDate, endDate);
            if (payout.TotalAmount > 0) // Only include consignors with amounts owed
            {
                payouts.Add(payout);
            }
        }

        return payouts;
    }

    public async Task MarkPayoutAsPaidAsync(Guid payoutId, string paymentMethod, string? notes = null)
    {
        // In MVP, payoutId represents the consignor ID for a date range
        // Mark all unpaid transactions for this consignor as paid
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

        // Send notification to consignor about the payout
        if (transactions.Any())
        {
            try
            {
                var consignor = await _context.Consignors
                    .FirstOrDefaultAsync(p => p.Id == payoutId);

                if (consignor != null && consignor.UserId.HasValue)
                {
                    var totalAmount = transactions.Sum(t => t.ConsignorAmount);

                    await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                    {
                        OrganizationId = consignor.OrganizationId,
                        UserId = consignor.UserId.Value,
                        ConsignorId = consignor.Id,
                        Type = NotificationType.PayoutProcessed,
                        Title = "Payout Processed 💰",
                        Message = $"A payout of {totalAmount:C} has been processed via {paymentMethod}.",
                        RelatedEntityType = "Payout",
                        RelatedEntityId = payoutId, // Using consignor ID as payout identifier for MVP
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
                _logger.LogError(ex, "Failed to send payout notification for consignor {ConsignorId}", payoutId);
            }
        }

        _logger.LogInformation(
            "[MANUAL PAYOUT] Marked {Count} transactions as paid for consignor {ConsignorId}\n" +
            "  Payment Method: {PaymentMethod}\n" +
            "  Notes: {Notes}",
            transactions.Count, payoutId, paymentMethod, notes ?? "None"
        );
    }

    public async Task<byte[]> ExportPayoutToCsvAsync(Guid payoutId)
    {
        // For MVP, this would need the consignor ID and date range
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

    public async Task<decimal> GetPendingPayoutAmountAsync(Guid consignorId)
    {
        var pendingAmount = await _context.Transactions
            .Where(t => t.ConsignorId == consignorId && !t.ConsignorPaidOut)
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