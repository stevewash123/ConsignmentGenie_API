using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ConsignmentGenie.Core.Extensions;

/// <summary>
/// Generic enum extensions for converting enums to/from their Display attribute string values
/// This ensures consistent enum-to-string conversion across the entire API
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Legacy method - kept for backward compatibility
    /// </summary>
    public static string ToDisplayName(this Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<DisplayAttribute>()
            ?.GetName() ?? SplitCamelCase(enumValue.ToString());
    }

    /// <summary>
    /// Gets the string representation of any enum value for API serialization
    /// Uses the Display attribute Name if present, otherwise falls back to lowercase enum name
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    /// <param name="enumValue">The enum value to convert</param>
    /// <returns>The string representation for API/frontend consumption</returns>
    public static string ToStringValue<T>(this T enumValue) where T : struct, Enum
    {
        var field = typeof(T).GetField(enumValue.ToString());
        var displayAttribute = field?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? enumValue.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Parses a string value back to the specified enum type
    /// Used for processing incoming API requests and database values
    /// </summary>
    /// <typeparam name="T">The enum type to parse to</typeparam>
    /// <param name="value">The string value to parse</param>
    /// <returns>The corresponding enum value</returns>
    /// <exception cref="ArgumentException">Thrown when the string value doesn't match any enum value</exception>
    public static T FromStringValue<T>(string value) where T : struct, Enum
    {
        foreach (T enumValue in Enum.GetValues<T>())
        {
            if (enumValue.ToStringValue().Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return enumValue;
            }
        }

        throw new ArgumentException($"Unknown {typeof(T).Name} value: {value}", nameof(value));
    }

    /// <summary>
    /// Tries to parse a string value to the specified enum type without throwing
    /// </summary>
    /// <typeparam name="T">The enum type to parse to</typeparam>
    /// <param name="value">The string value to parse</param>
    /// <param name="enumValue">The parsed enum value if successful</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    public static bool TryFromStringValue<T>(string value, out T enumValue) where T : struct, Enum
    {
        try
        {
            enumValue = FromStringValue<T>(value);
            return true;
        }
        catch
        {
            enumValue = default;
            return false;
        }
    }

    /// <summary>
    /// Gets all possible string values for an enum type
    /// Useful for validation and frontend dropdown generation
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    /// <returns>Array of all possible string values</returns>
    public static string[] GetAllStringValues<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>().Select(e => e.ToStringValue()).ToArray();
    }

    /// <summary>
    /// Gets a dictionary mapping enum values to their string representations
    /// Useful for lookups and validation
    /// </summary>
    /// <typeparam name="T">The enum type</typeparam>
    /// <returns>Dictionary with enum values as keys and string values as values</returns>
    public static Dictionary<T, string> GetStringValueDictionary<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>().ToDictionary(e => e, e => e.ToStringValue());
    }

    private static string SplitCamelCase(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
    }
}