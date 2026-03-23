using System.Text.RegularExpressions;

namespace ErrorAnalyzer.Core;

internal sealed class LogLine
{
    public LogLine()
    {
        Text = string.Empty;
    }

    public LogLine(int number, string text)
    {
        Number = number;
        Text = text;
    }

    public int Number { get; set; }

    public string Text { get; set; }
}

internal sealed class LogDocument
{
    private static readonly Regex TimestampOnlyRegex = new(@"^\d{1,2}:\d{1,2}:\d{1,2}(?:\.\d+)?$", RegexOptions.Compiled);
    private static readonly Regex TimestampedModRegex = new(@"^\[[^\]]+\]\s+\[(?<mod>[^\]]+)\]", RegexOptions.Compiled);
    private static readonly Regex UntimestampedModRegex = new(@"^\[(?<mod>[^\]]+)\]", RegexOptions.Compiled);
    private static readonly Regex StackFrameRegex = new(@"\bat\s+(?<symbol>[A-Za-z0-9_`]+(?:\.[A-Za-z0-9_`]+)+)", RegexOptions.Compiled);
    private static readonly Regex AssemblyRegex = new(@"assembly\s+(?<assembly>[^,]+),\s+Version=", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> InfrastructurePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Il2CppInterop",
        "UnityExceptionTrace",
        "HarmonyLib",
        "AccessTools",
        "MelonLoader",
        "System",
        "Unity",
    };
    private static readonly HashSet<string> IgnoredAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Assembly-CSharp",
        "UnityEngine.CoreModule",
        "System.Private.CoreLib",
    };

    public LogDocument(string sourceName, string text)
    {
        SourceName = sourceName;
        Text = text;
        Lines = text
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select((line, index) => new LogLine(index + 1, line))
            .ToArray();
        Runtime = DetectRuntime(Lines);
    }

    public string SourceName { get; }

    public string Text { get; }

    public IReadOnlyList<LogLine> Lines { get; }

    public RuntimeKind Runtime { get; }

    public string? FindNearestModName(int lineIndex, int searchRadius = 8, bool allowForwardSearch = true)
    {
        var currentLineModName = TryExtractModName(Lines[lineIndex].Text);
        if (!string.IsNullOrWhiteSpace(currentLineModName))
        {
            return currentLineModName;
        }

        foreach (var candidate in EnumerateNearbyLines(lineIndex, searchRadius, allowForwardSearch))
        {
            var stackOwner = TryExtractStackOwner(candidate.Text);
            if (string.IsNullOrWhiteSpace(stackOwner))
            {
                continue;
            }

            return stackOwner;
        }

        foreach (var candidate in EnumerateNearbyLines(lineIndex, searchRadius, allowForwardSearch))
        {
            var modName = TryExtractModName(candidate.Text);
            if (!string.IsNullOrWhiteSpace(modName))
            {
                return modName;
            }
        }

        foreach (var candidate in EnumerateNearbyLines(lineIndex, searchRadius, allowForwardSearch))
        {
            var match = AssemblyRegex.Match(candidate.Text);
            if (match.Success)
            {
                var assemblyName = NormalizeAssemblyName(match.Groups["assembly"].Value);
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    return assemblyName;
                }
            }
        }

        foreach (var candidate in EnumerateBackwardLines(lineIndex, 40))
        {
            if (TryExtractModName(candidate.Text) is not null)
            {
                break;
            }

            var match = AssemblyRegex.Match(candidate.Text);
            if (match.Success)
            {
                var assemblyName = NormalizeAssemblyName(match.Groups["assembly"].Value);
                if (!string.IsNullOrWhiteSpace(assemblyName) && !InfrastructurePrefixes.Contains(assemblyName))
                {
                    return assemblyName;
                }
            }
        }

        return null;
    }

    public IEnumerable<LogLine> EnumerateNearbyLines(int lineIndex, int searchRadius, bool allowForwardSearch = true)
    {
        var indexes = new List<int> { lineIndex };
        for (var distance = 1; distance <= searchRadius; distance++)
        {
            indexes.Add(lineIndex - distance);
            if (allowForwardSearch)
            {
                indexes.Add(lineIndex + distance);
            }
        }

        foreach (var index in indexes)
        {
            if (index >= 0 && index < Lines.Count)
            {
                yield return Lines[index];
            }
        }
    }

    public bool ContainsNearby(int lineIndex, int searchRadius, Func<string, bool> predicate)
        => EnumerateNearbyLines(lineIndex, searchRadius).Any(line => predicate(line.Text));

    private IEnumerable<LogLine> EnumerateBackwardLines(int lineIndex, int searchRadius)
    {
        for (var index = lineIndex; index >= 0 && index >= lineIndex - searchRadius; index--)
        {
            yield return Lines[index];
        }
    }

    private static string? TryExtractModName(string text)
    {
        var match = TimestampedModRegex.Match(text);
        if (match.Success)
        {
            return NormalizeBracketName(match.Groups["mod"].Value);
        }

        match = UntimestampedModRegex.Match(text);
        if (match.Success)
        {
            return NormalizeBracketName(match.Groups["mod"].Value);
        }

        return null;
    }

    private static string? NormalizeBracketName(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) ||
            InfrastructurePrefixes.Contains(trimmed) ||
            TimestampOnlyRegex.IsMatch(trimmed))
        {
            return null;
        }

        return trimmed;
    }

    private static string? TryExtractStackOwner(string text)
    {
        var match = StackFrameRegex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var symbol = match.Groups["symbol"].Value;
        var segments = symbol.Split('.');
        if (segments.Length < 3)
        {
            return null;
        }

        var owner = segments[0];
        return InfrastructurePrefixes.Contains(owner) ? null : owner;
    }

    private static string? NormalizeAssemblyName(string value)
    {
        var normalized = value.Trim().Trim('\'', '"');
        if (string.IsNullOrWhiteSpace(normalized) || IgnoredAssemblies.Contains(normalized))
        {
            return null;
        }

        return normalized;
    }

    private static RuntimeKind DetectRuntime(IEnumerable<LogLine> lines)
    {
        foreach (var line in lines)
        {
            if (line.Text.Contains("Game Type:", StringComparison.OrdinalIgnoreCase) &&
                line.Text.Contains("Il2Cpp", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeKind.Il2Cpp;
            }

            if (line.Text.Contains("Game Type:", StringComparison.OrdinalIgnoreCase) &&
                line.Text.Contains("Mono", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeKind.Mono;
            }

            if (!line.Text.Contains("Support Module Loaded:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.Text.Contains("Il2Cpp.dll", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeKind.Il2Cpp;
            }

            if (line.Text.Contains("Mono.dll", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeKind.Mono;
            }
        }

        return RuntimeKind.Unknown;
    }
}
