using AutoMapper;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Provider;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public class ProvidersController : ControllerBase
{
    private readonly ConsignmentGenieContext _context;
    private readonly IMapper _mapper;

    public ProvidersController(ConsignmentGenieContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    private Guid GetOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(organizationIdClaim, out var orgId) ? orgId : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProviderDto>>>> GetProviders()
    {
        try
        {
            var organizationId = GetOrganizationId();

            var providers = await _context.Providers
                .Where(p => p.OrganizationId == organizationId)
                .Include(p => p.Items)
                .Include(p => p.Transactions)
                .ToListAsync();

            var providerDtos = providers.Select(p => new ProviderDto
            {
                Id = p.Id,
                DisplayName = p.DisplayName,
                Email = p.Email,
                Phone = p.Phone,
                DefaultSplitPercentage = p.DefaultSplitPercentage,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                ActiveItemsCount = p.Items.Count(i => i.Status == Core.Enums.ItemStatus.Available),
                TotalEarnings = p.Transactions.Sum(t => t.ProviderAmount)
            }).ToList();

            return Ok(ApiResponse<List<ProviderDto>>.SuccessResult(providerDtos));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<ProviderDto>>.ErrorResult($"Failed to retrieve providers: {ex.Message}"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> GetProvider(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var provider = await _context.Providers
                .Include(p => p.Items)
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDto>.ErrorResult("Provider not found"));
            }

            var providerDto = new ProviderDto
            {
                Id = provider.Id,
                DisplayName = provider.DisplayName,
                Email = provider.Email,
                Phone = provider.Phone,
                DefaultSplitPercentage = provider.DefaultSplitPercentage,
                PaymentMethod = provider.PaymentMethod,
                Status = provider.Status,
                Notes = provider.Notes,
                CreatedAt = provider.CreatedAt,
                ActiveItemsCount = provider.Items.Count(i => i.Status == Core.Enums.ItemStatus.Available),
                TotalEarnings = provider.Transactions.Sum(t => t.ProviderAmount)
            };

            return Ok(ApiResponse<ProviderDto>.SuccessResult(providerDto));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProviderDto>.ErrorResult($"Failed to retrieve provider: {ex.Message}"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> CreateProvider([FromBody] CreateProviderRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();

            // Check if email already exists for this organization
            if (await _context.Providers.AnyAsync(p => p.Email.ToLower() == request.Email.ToLower() && p.OrganizationId == organizationId))
            {
                return BadRequest(ApiResponse<ProviderDto>.ErrorResult("Provider with this email already exists"));
            }

            var provider = new Provider
            {
                OrganizationId = organizationId,
                DisplayName = request.DisplayName,
                Email = request.Email,
                Phone = request.Phone,
                DefaultSplitPercentage = request.DefaultSplitPercentage,
                PaymentMethod = request.PaymentMethod,
                PaymentDetails = request.PaymentDetails,
                Status = request.Status,
                Notes = request.Notes
            };

            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();

            var providerDto = new ProviderDto
            {
                Id = provider.Id,
                DisplayName = provider.DisplayName,
                Email = provider.Email,
                Phone = provider.Phone,
                DefaultSplitPercentage = provider.DefaultSplitPercentage,
                PaymentMethod = provider.PaymentMethod,
                Status = provider.Status,
                Notes = provider.Notes,
                CreatedAt = provider.CreatedAt,
                ActiveItemsCount = 0,
                TotalEarnings = 0
            };

            return CreatedAtAction(nameof(GetProvider), new { id = provider.Id }, ApiResponse<ProviderDto>.SuccessResult(providerDto, "Provider created successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProviderDto>.ErrorResult($"Failed to create provider: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProviderDto>>> UpdateProvider(Guid id, [FromBody] UpdateProviderRequest request)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

            if (provider == null)
            {
                return NotFound(ApiResponse<ProviderDto>.ErrorResult("Provider not found"));
            }

            // Check if email already exists for another provider in this organization
            if (await _context.Providers.AnyAsync(p => p.Id != id && p.Email.ToLower() == request.Email.ToLower() && p.OrganizationId == organizationId))
            {
                return BadRequest(ApiResponse<ProviderDto>.ErrorResult("Another provider with this email already exists"));
            }

            provider.DisplayName = request.DisplayName;
            provider.Email = request.Email;
            provider.Phone = request.Phone;
            provider.DefaultSplitPercentage = request.DefaultSplitPercentage;
            provider.PaymentMethod = request.PaymentMethod;
            provider.PaymentDetails = request.PaymentDetails;
            provider.Status = request.Status;
            provider.Notes = request.Notes;

            await _context.SaveChangesAsync();

            var providerDto = new ProviderDto
            {
                Id = provider.Id,
                DisplayName = provider.DisplayName,
                Email = provider.Email,
                Phone = provider.Phone,
                DefaultSplitPercentage = provider.DefaultSplitPercentage,
                PaymentMethod = provider.PaymentMethod,
                Status = provider.Status,
                Notes = provider.Notes,
                CreatedAt = provider.CreatedAt,
                ActiveItemsCount = await _context.Items.CountAsync(i => i.ProviderId == id && i.Status == Core.Enums.ItemStatus.Available),
                TotalEarnings = await _context.Transactions.Where(t => t.ProviderId == id).SumAsync(t => t.ProviderAmount)
            };

            return Ok(ApiResponse<ProviderDto>.SuccessResult(providerDto, "Provider updated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProviderDto>.ErrorResult($"Failed to update provider: {ex.Message}"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProvider(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();

            var provider = await _context.Providers
                .Include(p => p.Items)
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId);

            if (provider == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Provider not found"));
            }

            // Check if provider has items or transactions
            if (provider.Items.Any() || provider.Transactions.Any())
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete provider with existing items or transactions"));
            }

            _context.Providers.Remove(provider);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null, "Provider deleted successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult($"Failed to delete provider: {ex.Message}"));
        }
    }
}