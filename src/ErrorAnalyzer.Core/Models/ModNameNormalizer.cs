using System.Text;

namespace ErrorAnalyzer.Core.Models;

internal static class ModNameNormalizer
{
    public static string? Normalize(string? modName)
    {
        var trimmed = modName?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static string GetEquivalenceKey(string? modName)
    {
        var normalized = Normalize(modName);
        if (normalized is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;
        foreach (var character in normalized)
        {
            if (character == '_' || char.IsWhiteSpace(character))
            {
                if (previousWasSeparator)
                {
                    continue;
                }

                builder.Append(' ');
                previousWasSeparator = true;
                continue;
            }

            builder.Append(character);
            previousWasSeparator = false;
        }

        return builder.ToString();
    }

    public static bool ShouldPreferDisplayName(string candidate, string existing)
        => GetDisplayScore(candidate) > GetDisplayScore(existing);

    private static int GetDisplayScore(string modName)
    {
        var hasSpaces = modName.IndexOf(' ') >= 0;
        var hasUnderscores = modName.IndexOf('_') >= 0;

        if (hasSpaces && !hasUnderscores)
        {
            return 2;
        }

        if (!hasUnderscores)
        {
            return 1;
        }

        return 0;
    }
}
