using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ConsignmentGenie.Core.Extensions;

public static class EnumExtensions
{
    public static string ToDisplayName(this Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<DisplayAttribute>()
            ?.GetName() ?? SplitCamelCase(enumValue.ToString());
    }

    private static string SplitCamelCase(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
    }
}