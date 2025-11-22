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

    [HttpGet]
    public async Task<IActionResult> GetPayouts([FromQuery] string? status = null)
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return Unauthorized("Invalid organization");
        }

        var query = _context.Payouts
            .Include(p => p.Provider)
            .Where(p => p.OrganizationId == organizationId);

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PayoutStatus>(status, true, out var payoutStatus))
        {
            query = query.Where(p => p.Status == payoutStatus);
        }

        var payouts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                id = p.Id,
                providerId = p.ProviderId,
                providerName = p.Provider.DisplayName,
                amount = p.TotalAmount,
                status = p.Status.ToString(),
                createdDate = p.CreatedAt,
                periodStart = p.PeriodStart,
                periodEnd = p.PeriodEnd,
                paidAt = p.PaidAt,
                paymentMethod = p.PaymentMethod,
                notes = p.Notes
            })
            .ToListAsync();

        return Ok(new { success = true, data = payouts });
    }
}