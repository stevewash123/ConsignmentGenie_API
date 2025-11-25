using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Application.Services;

public class StoreCodeService : IStoreCodeService
{
    private readonly ConsignmentGenieContext _context;

    public StoreCodeService(ConsignmentGenieContext context)
    {
        _context = context;
    }

    public async Task<StoreCodeDto> GetStoreCodeAsync(Guid organizationId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found");

        return new StoreCodeDto
        {
            StoreCode = organization.StoreCode ?? string.Empty,
            IsEnabled = organization.StoreCodeEnabled,
            LastRegenerated = organization.UpdatedAt
        };
    }

    public async Task<StoreCodeDto> RegenerateStoreCodeAsync(Guid organizationId)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found");

        organization.StoreCode = GenerateStoreCode();
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new StoreCodeDto
        {
            StoreCode = organization.StoreCode,
            IsEnabled = organization.StoreCodeEnabled,
            LastRegenerated = organization.UpdatedAt
        };
    }

    public async Task ToggleStoreCodeAsync(Guid organizationId, bool enabled)
    {
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new ArgumentException("Organization not found");

        organization.StoreCodeEnabled = enabled;
        await _context.SaveChangesAsync();
    }

    public string GenerateStoreCode()
    {
        // Generate random 4-digit number
        var random = new Random();
        string code;
        int attempts = 0;
        const int maxAttempts = 100;

        do
        {
            code = random.Next(1000, 9999).ToString();
            attempts++;

            if (attempts >= maxAttempts)
            {
                // Fallback to 5-digit if too many collisions
                code = random.Next(10000, 99999).ToString();
                break;
            }
        }
        while (_context.Organizations.Any(o => o.StoreCode == code));

        return code;
    }
}