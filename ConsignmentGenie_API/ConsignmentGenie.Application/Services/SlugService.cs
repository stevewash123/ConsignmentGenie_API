using System.Text.RegularExpressions;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Application.Services;

public class SlugService : ISlugService
{
    private readonly ConsignmentGenieContext _context;

    public SlugService(ConsignmentGenieContext context)
    {
        _context = context;
    }

    public string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var slug = input
            .ToLowerInvariant()
            .Trim();

        // Replace common words and characters
        slug = slug.Replace("&", "and")
                   .Replace("'", "")
                   .Replace("\"", "");

        // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"\s+", "-");

        // Remove all non-alphanumeric characters except hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Replace multiple consecutive hyphens with a single hyphen
        slug = Regex.Replace(slug, @"-+", "-");

        // Remove leading and trailing hyphens
        slug = slug.Trim('-');

        // Ensure slug is not empty
        if (string.IsNullOrEmpty(slug))
            return "shop";

        return slug;
    }

    public async Task<string> GenerateUniqueOrganizationSlugAsync(string shopName, Guid? excludeOrganizationId = null)
    {
        var baseSlug = GenerateSlug(shopName);
        var slug = baseSlug;
        var counter = 1;

        // Check for uniqueness and append counter if needed
        while (await SlugExistsAsync(slug, excludeOrganizationId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;

            // Prevent infinite loops - if we reach 1000, append a GUID
            if (counter > 1000)
            {
                slug = $"{baseSlug}-{Guid.NewGuid():N}";
                break;
            }
        }

        return slug;
    }

    public bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        // Must contain only lowercase letters, numbers, and hyphens
        // Cannot start or end with a hyphen
        // Cannot have consecutive hyphens
        var pattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
        return Regex.IsMatch(slug, pattern);
    }

    private async Task<bool> SlugExistsAsync(string slug, Guid? excludeOrganizationId = null)
    {
        var query = _context.Organizations.Where(o => o.Slug == slug);

        if (excludeOrganizationId.HasValue)
        {
            query = query.Where(o => o.Id != excludeOrganizationId.Value);
        }

        return await query.AnyAsync();
    }
}