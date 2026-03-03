using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ConsignmentGenie.Application.Services;

public interface ICategoryValidationService
{
    Task<CategoryValidationResult> ValidateCategoryNameAsync(Guid organizationId, string name);
    Task<List<CategoryValidationResult>> ValidateCategoryNamesBulkAsync(Guid organizationId, List<string> names);
}

public class CategoryValidationResult
{
    public ItemCategory? ExactMatch { get; set; }
    public List<CategorySuggestion> SuggestedMatches { get; set; } = new();
    public bool IsExactMatch { get; set; }
}

public class CategorySuggestion
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Similarity { get; set; }
}

public class CategoryValidationService : ICategoryValidationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    // Configurable thresholds from appsettings
    private readonly double _exactMatchThreshold;
    private readonly double _closeMatchThreshold;
    private readonly double _levenshteinWeight;
    private readonly double _tokenOverlapWeight;
    private readonly double _containsWeight;

    public CategoryValidationService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;

        // Load configuration with defaults
        _exactMatchThreshold = _configuration.GetValue<double>("CategoryValidation:ExactMatchThreshold", 1.0);
        _closeMatchThreshold = _configuration.GetValue<double>("CategoryValidation:CloseMatchThreshold", 0.70);
        _levenshteinWeight = _configuration.GetValue<double>("CategoryValidation:LevenshteinWeight", 0.40);
        _tokenOverlapWeight = _configuration.GetValue<double>("CategoryValidation:TokenOverlapWeight", 0.35);
        _containsWeight = _configuration.GetValue<double>("CategoryValidation:ContainsWeight", 0.25);
    }

    public async Task<CategoryValidationResult> ValidateCategoryNameAsync(Guid organizationId, string name)
    {
        var results = await ValidateCategoryNamesBulkAsync(organizationId, new List<string> { name });
        return results.First();
    }

    public async Task<List<CategoryValidationResult>> ValidateCategoryNamesBulkAsync(Guid organizationId, List<string> names)
    {
        // Amendment 1: Load both active and pending categories for validation
        // Active categories (both CG and Square sources) are available for exact/fuzzy matching
        // Pending categories are also checked to prevent duplicate pending entries
        var existingCategories = await _unitOfWork.ItemCategories.GetAllAsync(
            c => c.OrganizationId == organizationId &&
                 c.IsActive &&
                 (c.Status == CategoryStatus.Active || c.Status == CategoryStatus.Pending)
        );

        var results = new List<CategoryValidationResult>();

        foreach (var name in names)
        {
            var result = await ValidateSingleCategoryAsync(name, existingCategories);
            results.Add(result);
        }

        return results;
    }

    private Task<CategoryValidationResult> ValidateSingleCategoryAsync(string inputName, IEnumerable<ItemCategory> existingCategories)
    {
        var normalizedInput = NormalizeForComparison(inputName);
        var suggestions = new List<CategorySuggestion>();

        foreach (var category in existingCategories)
        {
            var normalizedCategory = NormalizeForComparison(category.Name);
            var similarity = CalculateSimilarity(normalizedInput, normalizedCategory);

            // Check for exact match first
            if (similarity >= _exactMatchThreshold)
            {
                return Task.FromResult(new CategoryValidationResult
                {
                    ExactMatch = category,
                    IsExactMatch = true,
                    SuggestedMatches = new List<CategorySuggestion>()
                });
            }

            // Collect close matches
            if (similarity >= _closeMatchThreshold)
            {
                suggestions.Add(new CategorySuggestion
                {
                    CategoryId = category.Id,
                    Name = category.Name,
                    Similarity = similarity
                });
            }
        }

        // Sort suggestions by similarity (highest first) and take top 3
        var topSuggestions = suggestions
            .OrderByDescending(s => s.Similarity)
            .Take(3)
            .ToList();

        return Task.FromResult(new CategoryValidationResult
        {
            ExactMatch = null,
            IsExactMatch = false,
            SuggestedMatches = topSuggestions
        });
    }

    private double CalculateSimilarity(string input, string category)
    {
        // Combined scoring strategy
        var levenshteinScore = CalculateNormalizedLevenshtein(input, category);
        var tokenOverlapScore = CalculateJaccardSimilarity(input, category);
        var containsScore = CalculateContainsSimilarity(input, category);

        return (_levenshteinWeight * levenshteinScore) +
               (_tokenOverlapWeight * tokenOverlapScore) +
               (_containsWeight * containsScore);
    }

    private string NormalizeForComparison(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Apply normalization rules from specification
        var normalized = input.Trim();

        // Collapse multiple spaces to single space
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        // Normalize apostrophes (smart quotes to straight apostrophe)
        normalized = normalized.Replace('\u2018', '\'').Replace('\u2019', '\'').Replace('`', '\'');

        // For comparison: lowercase and strip apostrophes entirely
        normalized = normalized.ToLowerInvariant();
        normalized = normalized.Replace("'", "");

        // Strip leading/trailing "the"
        if (normalized.StartsWith("the "))
            normalized = normalized.Substring(4);
        if (normalized.EndsWith(" the"))
            normalized = normalized.Substring(0, normalized.Length - 4);

        // Strip trailing "s" for simple plural handling (only for words 4+ chars)
        var words = normalized.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length >= 5 && words[i].EndsWith("s") && !words[i].EndsWith("ss"))
            {
                words[i] = words[i].Substring(0, words[i].Length - 1);
            }
        }
        normalized = string.Join(" ", words);

        return normalized;
    }

    private double CalculateNormalizedLevenshtein(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            return 1.0;

        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        var distance = CalculateLevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    private int CalculateLevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        // Initialize first row and column
        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        // Fill the matrix
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private double CalculateJaccardSimilarity(string s1, string s2)
    {
        var tokens1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tokens2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (tokens1.Count == 0 && tokens2.Count == 0)
            return 1.0;

        if (tokens1.Count == 0 || tokens2.Count == 0)
            return 0.0;

        var intersection = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();

        return (double)intersection / union;
    }

    private double CalculateContainsSimilarity(string s1, string s2)
    {
        var shorter = s1.Length <= s2.Length ? s1 : s2;
        var longer = s1.Length > s2.Length ? s1 : s2;

        if (string.IsNullOrEmpty(shorter))
            return string.IsNullOrEmpty(longer) ? 1.0 : 0.0;

        // Check if shorter string is contained in longer string
        if (longer.Contains(shorter))
        {
            // Return score based on how much of the longer string the shorter represents
            return (double)shorter.Length / longer.Length;
        }

        // Check prefix match
        if (longer.StartsWith(shorter))
        {
            return (double)shorter.Length / longer.Length * 0.8; // Slightly lower than contains
        }

        return 0.0;
    }
}