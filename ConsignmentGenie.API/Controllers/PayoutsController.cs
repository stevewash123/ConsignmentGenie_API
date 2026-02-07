using ConsignmentGenie.Application.DTOs.Payout;
using ConsignmentGenie.Application.DTOs.Transaction;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Application.Helpers;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "owner")]
public class PayoutsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<PayoutsController> _logger;
    private readonly IConsignorNotificationService _notificationService;

    public PayoutsController(
        ConsignmentGenieContext context,
        ILogger<PayoutsController> logger,
        IConsignorNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("organizationId")?.Value;
        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            throw new UnauthorizedAccessException("Invalid organization");
        }
        return organizationId;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayouts([FromQuery] PayoutSearchRequestDto request)
    {
        var organizationId = GetOrganizationId();

        var query = _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
                .ThenInclude(t => t.Items)
                    .ThenInclude(ti => ti.Item)
            .Where(p => p.OrganizationId == organizationId);

        // Apply filters
        if (request.ConsignorId.HasValue)
            query = query.Where(p => p.ConsignorId == request.ConsignorId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (request.PayoutDateFrom.HasValue)
            query = query.Where(p => p.PayoutDate >= request.PayoutDateFrom.Value);

        if (request.PayoutDateTo.HasValue)
            query = query.Where(p => p.PayoutDate <= request.PayoutDateTo.Value);

        if (request.PeriodStart.HasValue)
            query = query.Where(p => p.PeriodStart >= request.PeriodStart.Value);

        if (request.PeriodEnd.HasValue)
            query = query.Where(p => p.PeriodEnd <= request.PeriodEnd.Value);

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "payoutdate" => request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.PayoutDate)
                : query.OrderByDescending(p => p.PayoutDate),
            "amount" => request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.Amount)
                : query.OrderByDescending(p => p.Amount),
            "provider" => request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.Consignor.DisplayName)
                : query.OrderByDescending(p => p.Consignor.DisplayName),
            "status" => request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.Status)
                : query.OrderByDescending(p => p.Status),
            _ => query.OrderByDescending(p => p.PayoutDate)
        };

        // Pagination
        var totalCount = await query.CountAsync();
        var payouts = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PayoutListDto
            {
                Id = p.Id,
                PayoutNumber = p.PayoutNumber,
                PayoutDate = p.PayoutDate,
                Amount = p.Amount,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                TransactionCount = p.TransactionCount,
                Consignor = new ConsignorSummaryDto
                {
                    Id = p.Consignor.Id,
                    Name = p.Consignor.DisplayName,
                    Email = p.Consignor.Email
                }
            })
            .ToListAsync();

        return Ok(new {
            success = true,
            data = payouts,
            totalCount,
            page = request.Page,
            pageSize = request.PageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayoutById(Guid id)
    {
        var organizationId = GetOrganizationId();

        var payout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
                .ThenInclude(t => t.Items)
                    .ThenInclude(ti => ti.Item)
            .Where(p => p.Id == id && p.OrganizationId == organizationId)
            .Select(p => new PayoutDto
            {
                Id = p.Id,
                PayoutNumber = p.PayoutNumber,
                PayoutDate = p.PayoutDate,
                Amount = p.Amount,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                PaymentReference = p.PaymentReference,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                TransactionCount = p.TransactionCount,
                Notes = p.Notes,
                SyncedToQuickBooks = p.SyncedToQuickBooks,
                QuickBooksBillId = p.QuickBooksBillId,
                CreatedAt = p.CreatedAt,
                Consignor = new ConsignorSummaryDto
                {
                    Id = p.Consignor.Id,
                    Name = p.Consignor.DisplayName,
                    Email = p.Consignor.Email
                },
                Transactions = p.Transactions
                    .SelectMany(t => t.Items.Select(ti => new PayoutTransactionDto
                    {
                        TransactionId = t.Id,
                        ItemName = ti.Item.Title,
                        SaleDate = t.TransactionDate,
                        SalePrice = ti.UnitPrice * ti.Quantity,
                        ConsignorAmount = ti.ConsignorAmount,
                        ShopAmount = ti.StoreAmount
                    })).ToList()
            })
            .FirstOrDefaultAsync();

        if (payout == null)
            return NotFound("Payout not found");

        return Ok(new { success = true, data = payout });
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingPayouts([FromQuery] PendingPayoutsRequestDto request)
    {
        var organizationId = GetOrganizationId();

        // Use cached PayoutSummaries data instead of real-time calculation
        var query = _context.PayoutSummaries
            .Include(s => s.Consignor)
            .Where(s => s.OrganizationId == organizationId && s.TotalPendingAmount > 0);

        // Apply filters
        if (request.ConsignorId.HasValue)
            query = query.Where(s => s.ConsignorId == request.ConsignorId.Value);

        if (request.MinimumAmount.HasValue)
            query = query.Where(s => s.TotalPendingAmount >= request.MinimumAmount.Value);

        var summaries = await query.OrderByDescending(s => s.TotalPendingAmount).ToListAsync();

        // Get payout settings for dynamic clear date calculation
        var payoutSettings = await _context.PayoutSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);

        var holdPeriodDays = GetHoldPeriodDays(payoutSettings);
        var today = DateTime.UtcNow.Date;

        // Get the actual transaction details for each consignor (both cleared and uncleared)
        var consignorIds = summaries.Select(s => s.ConsignorId).ToList();
        var transactions = await _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Where(t => t.OrganizationId == organizationId && t.PayoutId == null)
            .SelectMany(t => t.Items.Select(ti => new
            {
                Transaction = t,
                TransactionItem = ti,
                ConsignorId = ti.ConsignorId
            }))
            .Where(x => consignorIds.Contains(x.ConsignorId))
            .ToListAsync();

        // Group transactions by consignor
        var transactionsByConsignor = transactions
            .GroupBy(x => x.ConsignorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Convert to the expected format with actual transaction data
        var pendingPayouts = summaries.Select(s => new
        {
            ConsignorId = s.ConsignorId,
            ConsignorName = !string.IsNullOrWhiteSpace($"{s.Consignor.FirstName} {s.Consignor.LastName}".Trim()) ?
                $"{s.Consignor.FirstName} {s.Consignor.LastName}".Trim() :
                (s.Consignor.DisplayName ?? "Unknown Consignor"),
            ProviderEmail = s.Consignor.Email,
            PendingAmount = s.TotalPendingAmount,
            ClearedAmount = s.ClearedAmount,
            UnclearedAmount = s.UnclearedAmount,
            TransactionCount = s.TotalTransactionCount,
            EarliestSale = s.EarliestSaleDate,
            LatestSale = s.LatestSaleDate,
            EarliestClearDate = s.NextClearDate,
            MeetsMinimumThreshold = s.MeetsMinimumThreshold,
            Transactions = transactionsByConsignor.TryGetValue(s.ConsignorId, out var consignorTransactions) ?
                consignorTransactions.Select(x => {
                    var calculatedClearDate = x.Transaction.TransactionDate.Date.AddDays(holdPeriodDays);
                    return new PayoutTransactionDto
                    {
                        TransactionId = x.Transaction.Id,
                        ItemName = x.TransactionItem.Item?.Title,
                        SaleDate = x.Transaction.TransactionDate,
                        ClearDate = calculatedClearDate,
                        IsCleared = calculatedClearDate <= today,
                        SalePrice = x.TransactionItem.UnitPrice * x.TransactionItem.Quantity,
                        ConsignorAmount = x.TransactionItem.ConsignorAmount,
                        ShopAmount = x.TransactionItem.StoreAmount
                    };
                }).ToList() :
                new List<PayoutTransactionDto>()
        }).ToList();

        return Ok(new { success = true, data = pendingPayouts });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayout([FromBody] CreatePayoutRequestDto request)
    {
        var organizationId = GetOrganizationId();

        // Validate consignor exists
        var consignor = await _context.Consignors
            .FirstOrDefaultAsync(p => p.Id == request.ConsignorId && p.OrganizationId == organizationId);

        if (consignor == null)
            return BadRequest("Consignor not found");

        // Validate transactions exist and are pending
        var transactions = await _context.Transactions
            .Where(t => request.TransactionIds.Contains(t.Id) &&
                       t.OrganizationId == organizationId &&
                       t.Items.Any(ti => ti.ConsignorId == request.ConsignorId) &&
                       !t.ConsignorPaidOut)
            .ToListAsync();

        if (transactions.Count != request.TransactionIds.Count)
            return BadRequest("Some transactions are invalid or already paid out");

        // Calculate total amount
        var totalAmount = transactions.Sum(t => t.ConsignorAmount());

        // Generate payout number
        var payoutNumber = await GeneratePayoutNumber(organizationId);

        // Create payout
        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ConsignorId = request.ConsignorId,
            PayoutNumber = payoutNumber,
            PayoutDate = request.PayoutDate,
            Amount = totalAmount,
            Status = PayoutStatus.Paid,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = request.PaymentReference,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            TransactionCount = transactions.Count,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payouts.Add(payout);

        // Update transactions
        foreach (var transaction in transactions)
        {
            transaction.PayoutId = payout.Id;
            transaction.PayoutStatus = "Paid";
            transaction.ConsignorPaidOut = true;
            transaction.ConsignorPaidOutDate = DateTime.UtcNow;
            transaction.PayoutMethod = request.PaymentMethod;
        }

        await _context.SaveChangesAsync();

        // Send notification to consignor since payout is immediately marked as paid
        if (consignor.UserId.HasValue)
        {
            try
            {
                var notification = NotificationHelper.CreateManualPayoutProcessedNotification(
                    consignor, payout.Amount, payout.PaymentMethod, payout.PayoutNumber);

                await _notificationService.CreateNotificationAsync(notification);

                _logger.LogInformation("Payout paid notification sent to consignor {ConsignorId} for payout {PayoutId}",
                    consignor.Id, payout.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payout notification for payout {PayoutId}", payout.Id);
                // Don't fail the entire operation if notification fails
            }
        }

        // Return the created payout
        var createdPayout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
                .ThenInclude(t => t.Items)
                    .ThenInclude(ti => ti.Item)
            .Where(p => p.Id == payout.Id)
            .Select(p => new PayoutDto
            {
                Id = p.Id,
                PayoutNumber = p.PayoutNumber,
                PayoutDate = p.PayoutDate,
                Amount = p.Amount,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                PaymentReference = p.PaymentReference,
                PeriodStart = p.PeriodStart,
                PeriodEnd = p.PeriodEnd,
                TransactionCount = p.TransactionCount,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                Consignor = new ConsignorSummaryDto
                {
                    Id = p.Consignor.Id,
                    Name = p.Consignor.DisplayName,
                    Email = p.Consignor.Email
                },
                Transactions = p.Transactions
                    .SelectMany(t => t.Items.Select(ti => new PayoutTransactionDto
                    {
                        TransactionId = t.Id,
                        ItemName = ti.Item.Title,
                        SaleDate = t.TransactionDate,
                        SalePrice = ti.UnitPrice * ti.Quantity,
                        ConsignorAmount = ti.ConsignorAmount,
                        ShopAmount = ti.StoreAmount
                    })).ToList()
            })
            .FirstAsync();

        // Note: Notification will be sent when payout status is changed to 'Paid' via UpdatePayoutStatus

        return Ok(new { success = true, data = createdPayout });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayout(Guid id, [FromBody] UpdatePayoutRequestDto request)
    {
        var organizationId = GetOrganizationId();

        var payout = await _context.Payouts
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (payout == null)
            return NotFound("Payout not found");

        // Update fields if provided
        if (request.PayoutDate.HasValue)
            payout.PayoutDate = request.PayoutDate.Value;

        if (request.Status.HasValue)
            payout.Status = request.Status.Value;

        if (!string.IsNullOrEmpty(request.PaymentMethod))
            payout.PaymentMethod = request.PaymentMethod;

        if (request.PaymentReference != null)
            payout.PaymentReference = request.PaymentReference;

        if (request.Notes != null)
            payout.Notes = request.Notes;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Payout updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayout(Guid id)
    {
        var organizationId = GetOrganizationId();

        var payout = await _context.Payouts
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (payout == null)
            return NotFound("Payout not found");

        // Reset transaction payout status
        foreach (var transaction in payout.Transactions)
        {
            transaction.PayoutId = null;
            transaction.PayoutStatus = "Pending";
            transaction.ConsignorPaidOut = false;
            transaction.ConsignorPaidOutDate = null;
            transaction.PayoutMethod = null;
        }

        _context.Payouts.Remove(payout);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Payout deleted successfully" });
    }

    [HttpGet("{id}/export")]
    public async Task<IActionResult> ExportPayoutToCsv(Guid id)
    {
        var organizationId = GetOrganizationId();

        var payout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
                .ThenInclude(t => t.Items)
                    .ThenInclude(ti => ti.Item)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (payout == null)
            return NotFound("Payout not found");

        var csv = GenerateCsv(payout);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

        return File(bytes, "text/csv", $"payout-{payout.PayoutNumber}.csv");
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> ExportPayoutToPdf(Guid id)
    {
        var organizationId = GetOrganizationId();

        var payout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
                .ThenInclude(t => t.Items)
                    .ThenInclude(ti => ti.Item)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (payout == null)
            return NotFound("Payout not found");

        var html = GeneratePayoutHtml(payout);
        var pdfBytes = GeneratePdfFromHtml(html);

        return File(pdfBytes, "application/pdf", $"payout-{payout.PayoutNumber}.pdf");
    }

    private string GenerateCsv(Payout payout)
    {
        var lines = new List<string>
        {
            "Payout Information",
            $"Payout Number,{payout.PayoutNumber}",
            $"Consignor,{payout.Consignor?.DisplayName}",
            $"Payout Date,{payout.PayoutDate:yyyy-MM-dd}",
            $"Total Amount,${payout.Amount:F2}",
            $"Payment Method,{payout.PaymentMethod}",
            $"Period,{payout.PeriodStart:yyyy-MM-dd} to {payout.PeriodEnd:yyyy-MM-dd}",
            "",
            "Transactions",
            "Item,Sale Date,Sale Price,Consignor Amount,Shop Amount"
        };

        foreach (var transaction in payout.Transactions)
        {
            lines.Add($"{transaction.ItemName()},{transaction.SaleDate():yyyy-MM-dd},${transaction.SalePrice():F2},${transaction.ConsignorAmount():F2},${transaction.ShopAmount():F2}");
        }

        lines.Add("");
        lines.Add($"Total Transactions,{payout.Transactions.Count}");
        lines.Add($"Total Consignor Amount,${payout.Transactions.Sum(t => t.ConsignorAmount()):F2}");

        return string.Join("\n", lines);
    }

    private string GeneratePayoutHtml(Payout payout)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ border-bottom: 2px solid #333; margin-bottom: 20px; padding-bottom: 10px; }}
        .info-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 20px; }}
        .info-item {{ margin-bottom: 10px; }}
        .label {{ font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .amount {{ text-align: right; }}
        .summary {{ margin-top: 20px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Payout Report</h1>
        <h2>#{payout.PayoutNumber}</h2>
    </div>

    <div class='info-grid'>
        <div>
            <div class='info-item'><span class='label'>Consignor:</span> {payout.Consignor?.DisplayName}</div>
            <div class='info-item'><span class='label'>Email:</span> {payout.Consignor?.Email}</div>
            <div class='info-item'><span class='label'>Payout Date:</span> {payout.PayoutDate:MMMM dd, yyyy}</div>
            <div class='info-item'><span class='label'>Payment Method:</span> {payout.PaymentMethod}</div>
        </div>
        <div>
            <div class='info-item'><span class='label'>Total Amount:</span> ${payout.Amount:F2}</div>
            <div class='info-item'><span class='label'>Transaction Count:</span> {payout.TransactionCount}</div>
            <div class='info-item'><span class='label'>Period:</span> {payout.PeriodStart:MMM dd} - {payout.PeriodEnd:MMM dd, yyyy}</div>
            <div class='info-item'><span class='label'>Status:</span> {payout.Status}</div>
        </div>
    </div>

    {(string.IsNullOrEmpty(payout.PaymentReference) ? "" : $"<div class='info-item'><span class='label'>Payment Reference:</span> {payout.PaymentReference}</div>")}
    {(string.IsNullOrEmpty(payout.Notes) ? "" : $"<div class='info-item'><span class='label'>Notes:</span> {payout.Notes}</div>")}

    <h3>Transaction Details</h3>
    <table>
        <thead>
            <tr>
                <th>Item</th>
                <th>Sale Date</th>
                <th class='amount'>Sale Price</th>
                <th class='amount'>Consignor Amount</th>
                <th class='amount'>Shop Amount</th>
            </tr>
        </thead>
        <tbody>
            {string.Join("", payout.Transactions.Select(t => $@"
            <tr>
                <td>{t.Item()?.Title}</td>
                <td>{t.SaleDate():MMM dd, yyyy}</td>
                <td class='amount'>${t.SalePrice():F2}</td>
                <td class='amount'>${t.ConsignorAmount():F2}</td>
                <td class='amount'>${t.ShopAmount():F2}</td>
            </tr>"))}
        </tbody>
    </table>

    <div class='summary'>
        <p>Total Consignor Payout: ${payout.Transactions.Sum(t => t.ConsignorAmount()):F2}</p>
        <p>Total Shop Revenue: ${payout.Transactions.Sum(t => t.ShopAmount()):F2}</p>
        <p>Total Sales: ${payout.Transactions.Sum(t => t.SalePrice()):F2}</p>
    </div>

    <div style='margin-top: 40px; text-align: center; color: #666; font-size: 12px;'>
        Generated on {DateTime.UtcNow:MMMM dd, yyyy} by ConsignmentGenie
    </div>
</body>
</html>";
    }

    private byte[] GeneratePdfFromHtml(string html)
    {
        // For MVP, return simple HTML as text in a basic PDF format
        // In production, you'd use a library like iTextSharp, PuppeteerSharp, or similar

        // Simple fallback: return HTML as bytes (browsers can handle this)
        // Note: This is a placeholder - in production you'd use a proper PDF generation library
        var htmlBytes = System.Text.Encoding.UTF8.GetBytes(html);
        return htmlBytes;
    }

    [HttpPost("batch/preview")]
    public async Task<IActionResult> GetBatchPayoutPreview([FromBody] BatchPayoutPreviewRequestDto request)
    {
        var organizationId = GetOrganizationId();

        var query = _context.Transactions
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Consignor)
            .Include(t => t.Items)
                .ThenInclude(ti => ti.Item)
            .Where(t => t.OrganizationId == organizationId &&
                       !t.ConsignorPaidOut);

        // Materialize the data first
        var transactions = await query.ToListAsync();

        // Filter by specific consignors if provided
        if (request.ConsignorIds != null && request.ConsignorIds.Any())
        {
            transactions = transactions.Where(t => t.ConsignorId().HasValue && request.ConsignorIds.Contains(t.ConsignorId().Value)).ToList();
        }

        // Group by consignor and calculate available balances
        var eligibleConsignors = transactions
            .GroupBy(t => new {
                ConsignorId = t.ConsignorId(),
                ConsignorName = t.Consignor()?.DisplayName,
                ConsignorEmail = t.Consignor()?.Email
            })
            .Where(g => g.Sum(t => t.ConsignorAmount()) > 0) // Only include consignors with positive balance
            .Select(g => new
            {
                ConsignorId = g.Key.ConsignorId.ToString(),
                Name = g.Key.ConsignorName,
                AvailableBalance = g.Sum(t => t.ConsignorAmount()),
                PreferredPaymentMethod = g.First().Consignor()?.PreferredPaymentMethod
            })
            .OrderBy(c => c.Name)
            .ToList();

        var totalAmount = eligibleConsignors.Sum(c => c.AvailableBalance);

        return Ok(new
        {
            success = true,
            data = new
            {
                eligibleConsignors,
                totalAmount,
                count = eligibleConsignors.Count
            }
        });
    }

    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatchPayout([FromBody] BatchPayoutRequestDto request)
    {
        var organizationId = GetOrganizationId();

        if (request.ConsignorIds == null || !request.ConsignorIds.Any())
        {
            return BadRequest("At least one consignor must be selected");
        }

        // Generate batch ID for this manual Pay All operation
        var batchId = $"MANUAL-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow.Ticks % 1000:D3}";
        var batchCreatedAt = DateTime.UtcNow;

        var createdPayouts = new List<object>();

        // Process each consignor separately
        foreach (var consignorIdString in request.ConsignorIds)
        {
            if (!Guid.TryParse(consignorIdString, out var consignorId))
            {
                continue; // Skip invalid GUIDs
            }

            // Get all unpaid transactions for this consignor
            var consignorTransactions = await _context.Transactions
                .Include(t => t.Items)
                    .ThenInclude(ti => ti.Consignor)
                .Where(t => t.OrganizationId == organizationId &&
                           !t.ConsignorPaidOut &&
                           t.Items.Any(ti => ti.ConsignorId == consignorId))
                .ToListAsync();

            if (!consignorTransactions.Any())
                continue;

            // Get consignor info
            var consignor = await _context.Consignors
                .FirstOrDefaultAsync(p => p.Id == consignorId && p.OrganizationId == organizationId);

            if (consignor == null)
                continue;

            // Calculate total amount for this consignor
            var totalAmount = consignorTransactions.Sum(t => t.ConsignorAmount());

            if (totalAmount <= 0)
                continue;

            // Generate payout number
            var payoutNumber = await GeneratePayoutNumber(organizationId);

            // Create payout
            var payout = new Payout
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ConsignorId = consignorId,
                PayoutNumber = payoutNumber,
                PayoutDate = DateTime.UtcNow,
                Amount = totalAmount,
                Status = PayoutStatus.Paid,
                PaymentMethod = request.Method,
                Notes = request.Notes,
                PeriodStart = consignorTransactions.Min(t => t.SaleDate()),
                PeriodEnd = consignorTransactions.Max(t => t.SaleDate()),
                TransactionCount = consignorTransactions.Count,
                BatchId = batchId,
                BatchCreatedAt = batchCreatedAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payouts.Add(payout);

            // Update transactions
            foreach (var transaction in consignorTransactions)
            {
                transaction.PayoutId = payout.Id;
                transaction.PayoutStatus = "Paid";
                transaction.ConsignorPaidOut = true;
                transaction.ConsignorPaidOutDate = DateTime.UtcNow;
                transaction.PayoutMethod = request.Method;
            }

            createdPayouts.Add(new
            {
                ConsignorId = consignorId.ToString(),
                ConsignorName = consignor.DisplayName,
                Amount = totalAmount,
                Method = request.Method,
                PayoutNumber = payoutNumber
            });
        }

        await _context.SaveChangesAsync();

        var totalBatchAmount = createdPayouts.Sum(p => (decimal)p.GetType().GetProperty("Amount")?.GetValue(p, null));

        return Ok(new
        {
            success = true,
            data = new
            {
                payouts = createdPayouts,
                totalAmount = totalBatchAmount,
                count = createdPayouts.Count,
                batchId = batchId,
                batchCreatedAt = batchCreatedAt
            }
        });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdatePayoutStatus(Guid id, [FromBody] UpdatePayoutStatusRequest request)
    {
        var organizationId = GetOrganizationId();

        var payout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

        if (payout == null)
            return NotFound("Payout not found");

        var oldStatus = payout.Status;
        payout.Status = request.Status;

        // If transitioning from Pending to Paid, update transaction statuses
        if (oldStatus == PayoutStatus.Pending && request.Status == PayoutStatus.Paid)
        {
            foreach (var transaction in payout.Transactions)
            {
                transaction.PayoutStatus = "Paid";
                transaction.ConsignorPaidOut = true;
                transaction.ConsignorPaidOutDate = DateTime.UtcNow;
            }
        }
        // If transitioning from Paid back to Pending, revert transaction statuses
        else if (oldStatus == PayoutStatus.Paid && request.Status == PayoutStatus.Pending)
        {
            foreach (var transaction in payout.Transactions)
            {
                transaction.PayoutStatus = "Pending";
                transaction.ConsignorPaidOut = false;
                transaction.ConsignorPaidOutDate = null;
            }
        }
        // If cancelling a payout, reset transactions
        else if (request.Status == PayoutStatus.Cancelled)
        {
            foreach (var transaction in payout.Transactions)
            {
                transaction.PayoutId = null;
                transaction.PayoutStatus = "Pending";
                transaction.ConsignorPaidOut = false;
                transaction.ConsignorPaidOutDate = null;
                transaction.PayoutMethod = null;
            }
        }

        await _context.SaveChangesAsync();

        // Log the status change
        _logger.LogInformation(
            "Payout {PayoutId} status changed from {OldStatus} to {NewStatus} for organization {OrganizationId}",
            id, oldStatus, request.Status, organizationId
        );

        // If changing to Paid status, send notification to consignor
        if (request.Status == PayoutStatus.Paid && payout.Consignor != null && payout.Consignor.UserId.HasValue)
        {
            try
            {
                var payoutReference = payout.PayoutNumber;
                var notification = NotificationHelper.CreateManualPayoutProcessedNotification(
                    payout.Consignor, payout.Amount, payout.PaymentMethod ?? "Unknown", payoutReference);

                await _notificationService.CreateNotificationAsync(notification);

                _logger.LogInformation("Payout paid notification sent to consignor {ConsignorId} for payout {PayoutId}",
                    payout.ConsignorId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payout status notification for payout {PayoutId}", id);
                // Don't fail the entire operation if notification fails
            }
        }

        return Ok(new { success = true, message = "Payout status updated successfully" });
    }

    private async Task<string> GeneratePayoutNumber(Guid organizationId)
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"PO{today:yyyyMMdd}";

        var lastPayoutNumber = await _context.Payouts
            .Where(p => p.OrganizationId == organizationId &&
                       p.PayoutNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PayoutNumber)
            .Select(p => p.PayoutNumber)
            .FirstOrDefaultAsync();

        if (lastPayoutNumber == null)
        {
            return $"{prefix}001";
        }

        var lastSequence = lastPayoutNumber.Substring(prefix.Length);
        if (int.TryParse(lastSequence, out var sequence))
        {
            return $"{prefix}{(sequence + 1):D3}";
        }

        return $"{prefix}001";
    }

    [HttpGet("summaries")]
    public async Task<ActionResult<List<PayoutSummaryDto>>> GetPayoutSummaries([FromQuery] bool readyOnly = false)
    {
        var organizationId = GetOrganizationId();

        var query = _context.PayoutSummaries
            .Include(s => s.Consignor)
            .Where(s => s.OrganizationId == organizationId && s.TotalTransactionCount > 0);

        if (readyOnly)
            query = query.Where(s => s.MeetsMinimumThreshold && s.ClearedAmount > 0);

        var summaries = await query.OrderByDescending(s => s.ClearedAmount).ToListAsync();
        return Ok(summaries.Select(MapToSummaryDto).ToList());
    }

    [HttpPost("summaries/refresh")]
    public async Task<IActionResult> RefreshSummaries([FromServices] Core.Services.IPayoutSummaryComputationService service)
    {
        var organizationId = GetOrganizationId();
        var startTime = DateTime.UtcNow;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        _logger.LogInformation("API ENDPOINT: RefreshSummaries called for organization {OrganizationId} by user {UserId}",
            organizationId, userId);

        try
        {
            await service.ComputeOrganizationSummariesAsync(organizationId);
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("API ENDPOINT: RefreshSummaries completed successfully in {Duration:F2}s for organization {OrganizationId}",
                duration.TotalSeconds, organizationId);
            return Ok(new { success = true, message = "Summaries refreshed", processingTimeSeconds = duration.TotalSeconds });
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "API ENDPOINT: RefreshSummaries FAILED after {Duration:F2}s for organization {OrganizationId}",
                duration.TotalSeconds, organizationId);
            return StatusCode(500, new { success = false, error = "Failed to refresh summaries", message = ex.Message });
        }
    }

    private int GetHoldPeriodDays(PayoutSettings? payoutSettings)
    {
        if (payoutSettings != null)
        {
            return payoutSettings.HoldPeriodDays;
        }

        // Default hold period if no settings found
        return 7;
    }

    [HttpGet("batch-report/{batchId}")]
    public async Task<ActionResult<PayoutBatchReportDto>> GetBatchReport(string batchId)
    {
        var organizationId = GetOrganizationId();

        var payouts = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
                .ThenInclude(t => t.Items)
            .Where(p => p.OrganizationId == organizationId && p.BatchId == batchId)
            .OrderBy(p => p.PayoutNumber)
            .ToListAsync();

        if (!payouts.Any())
            return NotFound("Batch not found");

        var batchReport = new PayoutBatchReportDto
        {
            BatchId = batchId,
            TotalPayouts = payouts.Count,
            TotalAmount = payouts.Sum(p => p.Amount),
            CreatedAt = payouts.First().BatchCreatedAt ?? payouts.First().CreatedAt,
            Payouts = payouts.Select(p => new PayoutBatchItemDto
            {
                PayoutNumber = p.PayoutNumber,
                ConsignorName = $"{p.Consignor.FirstName} {p.Consignor.LastName}",
                ItemCount = p.Transactions.SelectMany(t => t.Items).Count(),
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod
            }).ToList()
        };

        return Ok(batchReport);
    }

    [HttpPost("batch-export/{batchId}")]
    public async Task<IActionResult> ExportBatchReport(string batchId, [FromQuery] string format = "csv")
    {
        var organizationId = GetOrganizationId();

        var batchReportResult = await GetBatchReport(batchId);
        if (batchReportResult.Result is NotFoundResult)
            return NotFound("Batch not found");

        var batchReport = ((OkObjectResult)batchReportResult.Result!).Value as PayoutBatchReportDto;

        if (format.ToLower() == "csv")
        {
            var csv = GenerateBatchCsv(batchReport!);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"payout-batch-{batchId}.csv");
        }
        else if (format.ToLower() == "pdf")
        {
            // TODO: Implement PDF generation
            return BadRequest("PDF export not yet implemented");
        }

        return BadRequest("Invalid format. Use 'csv' or 'pdf'");
    }

    private string GenerateBatchCsv(PayoutBatchReportDto report)
    {
        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine($"Payout Batch Report - {report.BatchId}");
        csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        csv.AppendLine($"Total Payouts: {report.TotalPayouts}");
        csv.AppendLine($"Total Amount: ${report.TotalAmount:F2}");
        csv.AppendLine();

        // Column headers
        csv.AppendLine("Payout Number,Consignor Name,Item Count,Amount,Payment Method");

        // Data rows
        foreach (var payout in report.Payouts)
        {
            csv.AppendLine($"\"{payout.PayoutNumber}\",\"{payout.ConsignorName}\",{payout.ItemCount},${payout.Amount:F2},\"{payout.PaymentMethod}\"");
        }

        return csv.ToString();
    }

    private PayoutSummaryDto MapToSummaryDto(PayoutSummary summary)
    {
        return new PayoutSummaryDto
        {
            ConsignorId = summary.ConsignorId,
            ConsignorName = summary.Consignor.FirstName + " " + summary.Consignor.LastName,
            ConsignorNumber = summary.Consignor.ConsignorNumber,
            ClearedAmount = summary.ClearedAmount,
            UnclearedAmount = summary.UnclearedAmount,
            TotalPendingAmount = summary.TotalPendingAmount,
            ClearedTransactionCount = summary.ClearedTransactionCount,
            UnclearedTransactionCount = summary.UnclearedTransactionCount,
            TotalTransactionCount = summary.TotalTransactionCount,
            NextClearDate = summary.NextClearDate,
            MeetsMinimumThreshold = summary.MeetsMinimumThreshold,
            LastComputedAt = summary.LastComputedAt,
            // Legacy fields for backwards compatibility
            PendingAmount = summary.TotalPendingAmount,
            TransactionCount = summary.TotalTransactionCount,
            EarliestSale = summary.EarliestSaleDate,
            LatestSale = summary.LatestSaleDate,
            EarliestClearDate = summary.NextClearDate
        };
    }
}