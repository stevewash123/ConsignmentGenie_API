namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ISlugService
{
    /// <summary>
    /// Generates a URL-friendly slug from the given input string
    /// </summary>
    /// <param name="input">The input string to convert to a slug</param>
    /// <returns>A URL-friendly slug</returns>
    string GenerateSlug(string input);

    /// <summary>
    /// Generates a unique slug for an organization by checking existing slugs in the database
    /// </summary>
    /// <param name="shopName">The shop name to generate a slug from</param>
    /// <param name="excludeOrganizationId">Optional organization ID to exclude from uniqueness check (for updates)</param>
    /// <returns>A unique slug for the organization</returns>
    Task<string> GenerateUniqueOrganizationSlugAsync(string shopName, Guid? excludeOrganizationId = null);

    /// <summary>
    /// Validates if a slug is properly formatted
    /// </summary>
    /// <param name="slug">The slug to validate</param>
    /// <returns>True if the slug is valid, false otherwise</returns>
    bool IsValidSlug(string slug);
}