using ConsignmentGenie.Application.DTOs.Payout;
using ConsignmentGenie.Application.DTOs.Transaction;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class PayoutsController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;

    public PayoutsController(ConsignmentGenieContext context)
    {
        _context = context;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
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
                .ThenInclude(t => t.Item)
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
                Consignor = new ProviderSummaryDto
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
                .ThenInclude(t => t.Item)
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
                Consignor = new ProviderSummaryDto
                {
                    Id = p.Consignor.Id,
                    Name = p.Consignor.DisplayName,
                    Email = p.Consignor.Email
                },
                Transactions = p.Transactions.Select(t => new PayoutTransactionDto
                {
                    TransactionId = t.Id,
                    ItemName = t.Item.Title,
                    SaleDate = t.SaleDate,
                    SalePrice = t.SalePrice,
                    ConsignorAmount = t.ConsignorAmount,
                    ShopAmount = t.ShopAmount
                }).ToList()
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

        var query = _context.Transactions
            .Include(t => t.Consignor)
            .Include(t => t.Item)
            .Where(t => t.OrganizationId == organizationId &&
                       t.PayoutStatus == "Pending" &&
                       t.PayoutId == null);

        // Apply filters
        if (request.ConsignorId.HasValue)
            query = query.Where(t => t.ConsignorId == request.ConsignorId.Value);

        if (request.PeriodEndBefore.HasValue)
            query = query.Where(t => t.SaleDate <= request.PeriodEndBefore.Value);

        // Group by provider and calculate pending amounts
        var pendingPayouts = await query
            .GroupBy(t => new { t.ConsignorId, t.Consignor.DisplayName, t.Consignor.Email })
            .Select(g => new
            {
                ConsignorId = g.Key.ConsignorId,
                ConsignorName = g.Key.DisplayName,
                ProviderEmail = g.Key.Email,
                PendingAmount = g.Sum(t => t.ConsignorAmount),
                TransactionCount = g.Count(),
                EarliestSale = g.Min(t => t.SaleDate),
                LatestSale = g.Max(t => t.SaleDate),
                Transactions = g.Select(t => new PayoutTransactionDto
                {
                    TransactionId = t.Id,
                    ItemName = t.Item.Title,
                    SaleDate = t.SaleDate,
                    SalePrice = t.SalePrice,
                    ConsignorAmount = t.ConsignorAmount,
                    ShopAmount = t.ShopAmount
                }).ToList()
            })
            .ToListAsync();

        // Apply minimum amount filter if specified
        if (request.MinimumAmount.HasValue)
        {
            pendingPayouts = pendingPayouts
                .Where(p => p.PendingAmount >= request.MinimumAmount.Value)
                .ToList();
        }

        return Ok(new { success = true, data = pendingPayouts });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayout([FromBody] CreatePayoutRequestDto request)
    {
        var organizationId = GetOrganizationId();

        // Validate provider exists
        var provider = await _context.Consignors
            .FirstOrDefaultAsync(p => p.Id == request.ConsignorId && p.OrganizationId == organizationId);

        if (provider == null)
            return BadRequest("Consignor not found");

        // Validate transactions exist and are pending
        var transactions = await _context.Transactions
            .Where(t => request.TransactionIds.Contains(t.Id) &&
                       t.OrganizationId == organizationId &&
                       t.ConsignorId == request.ConsignorId &&
                       t.PayoutStatus == "Pending" &&
                       t.PayoutId == null)
            .ToListAsync();

        if (transactions.Count != request.TransactionIds.Count)
            return BadRequest("Some transactions are invalid or already paid out");

        // Calculate total amount
        var totalAmount = transactions.Sum(t => t.ConsignorAmount);

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
            transaction.ConsignorPaidOutDate = request.PayoutDate;
            transaction.PayoutMethod = request.PaymentMethod;
        }

        await _context.SaveChangesAsync();

        // Return the created payout
        var createdPayout = await _context.Payouts
            .Include(p => p.Consignor)
            .Include(p => p.Transactions)
                .ThenInclude(t => t.Item)
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
                Consignor = new ProviderSummaryDto
                {
                    Id = p.Consignor.Id,
                    Name = p.Consignor.DisplayName,
                    Email = p.Consignor.Email
                },
                Transactions = p.Transactions.Select(t => new PayoutTransactionDto
                {
                    TransactionId = t.Id,
                    ItemName = t.Item.Title,
                    SaleDate = t.SaleDate,
                    SalePrice = t.SalePrice,
                    ConsignorAmount = t.ConsignorAmount,
                    ShopAmount = t.ShopAmount
                }).ToList()
            })
            .FirstAsync();

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
                .ThenInclude(t => t.Item)
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
                .ThenInclude(t => t.Item)
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
            $"Consignor,{payout.Consignor.DisplayName}",
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
            lines.Add($"{transaction.Item.Title},{transaction.SaleDate:yyyy-MM-dd},${transaction.SalePrice:F2},${transaction.ConsignorAmount:F2},${transaction.ShopAmount:F2}");
        }

        lines.Add("");
        lines.Add($"Total Transactions,{payout.Transactions.Count}");
        lines.Add($"Total Consignor Amount,${payout.Transactions.Sum(t => t.ConsignorAmount):F2}");

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
            <div class='info-item'><span class='label'>Consignor:</span> {payout.Consignor.DisplayName}</div>
            <div class='info-item'><span class='label'>Email:</span> {payout.Consignor.Email}</div>
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
                <td>{t.Item.Title}</td>
                <td>{t.SaleDate:MMM dd, yyyy}</td>
                <td class='amount'>${t.SalePrice:F2}</td>
                <td class='amount'>${t.ConsignorAmount:F2}</td>
                <td class='amount'>${t.ShopAmount:F2}</td>
            </tr>"))}
        </tbody>
    </table>

    <div class='summary'>
        <p>Total Consignor Payout: ${payout.Transactions.Sum(t => t.ConsignorAmount):F2}</p>
        <p>Total Shop Revenue: ${payout.Transactions.Sum(t => t.ShopAmount):F2}</p>
        <p>Total Sales: ${payout.Transactions.Sum(t => t.SalePrice):F2}</p>
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
}