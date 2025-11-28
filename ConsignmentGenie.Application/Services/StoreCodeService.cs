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
        const int maxRetries = 5;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
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
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Collision occurred, retry with new code
                if (attempt == maxRetries - 1)
                    throw new Exception("Failed to generate unique store code after multiple attempts");
            }
        }

        throw new Exception("Failed to generate unique store code");
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

    private static readonly char[] AllowedLetters = "ABCDEFGHJKMNPQRTUVWXY".ToCharArray();
    private static readonly Random _random = new Random();

    public string GenerateStoreCode()
    {
        // Pattern: NNNLLN (e.g., 888TG4)
        var code = new char[6];

        // Positions 0-2: digits
        code[0] = (char)('0' + _random.Next(0, 10));
        code[1] = (char)('0' + _random.Next(0, 10));
        code[2] = (char)('0' + _random.Next(0, 10));

        // Positions 3-4: letters
        code[3] = AllowedLetters[_random.Next(AllowedLetters.Length)];
        code[4] = AllowedLetters[_random.Next(AllowedLetters.Length)];

        // Position 5: digit
        code[5] = (char)('0' + _random.Next(0, 10));

        return new string(code);
    }

    public bool IsValidStoreCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 6)
            return false;

        // Check positions 0-2 are digits
        if (!char.IsDigit(code[0]) || !char.IsDigit(code[1]) || !char.IsDigit(code[2]))
            return false;

        // Check positions 3-4 are allowed letters
        if (!AllowedLetters.Contains(code[3]) || !AllowedLetters.Contains(code[4]))
            return false;

        // Check position 5 is digit
        if (!char.IsDigit(code[5]))
            return false;

        return true;
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL unique constraint violation
        return ex.InnerException?.Message.Contains("unique constraint") == true
            || ex.InnerException?.Message.Contains("duplicate key") == true
            || ex.InnerException?.Message.Contains("23505") == true;
    }
}