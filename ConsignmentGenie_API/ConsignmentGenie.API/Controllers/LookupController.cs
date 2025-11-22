using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;

    public LookupController(ConsignmentGenieContext context)
    {
        _context = context;
    }
    [HttpGet("payment-methods")]
    public ActionResult<List<LookupDto>> GetPaymentMethods()
    {
        return Enum.GetValues<PaymentMethod>()
            .Select(e => new LookupDto
            {
                Value = e.ToString(),
                Label = e.ToDisplayName(),
                SortOrder = (int)e
            }).ToList();
    }

    [HttpGet("item-statuses")]
    public ActionResult<List<LookupDto>> GetItemStatuses()
    {
        return Enum.GetValues<ItemStatus>()
            .Select(e => new LookupDto
            {
                Value = e.ToString(),
                Label = e.ToDisplayName(),
                SortOrder = (int)e
            }).ToList();
    }

    [HttpGet("item-conditions")]
    public ActionResult<List<LookupDto>> GetItemConditions()
    {
        return Enum.GetValues<ItemCondition>()
            .Select(e => new LookupDto
            {
                Value = e.ToString(),
                Label = e.ToDisplayName(),
                SortOrder = (int)e
            }).ToList();
    }

    [HttpGet("payout-methods")]
    public ActionResult<List<LookupDto>> GetPayoutMethods()
    {
        return Enum.GetValues<PayoutMethod>()
            .Select(e => new LookupDto
            {
                Value = e.ToString(),
                Label = e.ToDisplayName(),
                SortOrder = (int)e
            }).ToList();
    }

    [HttpGet("provider-statuses")]
    public ActionResult<List<LookupDto>> GetProviderStatuses()
    {
        return Enum.GetValues<ProviderStatus>()
            .Select(e => new LookupDto
            {
                Value = e.ToString(),
                Label = e.ToDisplayName(),
                SortOrder = (int)e
            }).ToList();
    }

    [HttpGet("categories")]
    public ActionResult<List<LookupDto>> GetCategories()
    {
        // MVP: Hard-coded categories
        // Phase 2+: Database table with organization-specific categories
        var categories = new[]
        {
            "Clothing",
            "Accessories",
            "Furniture",
            "Art",
            "Collectibles",
            "Electronics",
            "Books",
            "Jewelry",
            "Home Decor",
            "Toys",
            "Sporting Goods",
            "Musical Instruments"
        };

        return categories
            .Select((cat, index) => new LookupDto
            {
                Value = cat,
                Label = cat,
                SortOrder = index + 1
            }).ToList();
    }
}