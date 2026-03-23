using ErrorAnalyzer.Core.Parsing;

namespace ErrorAnalyzer.Core.Runtime;

internal static class RuntimeVariantDetector
{
    public static IReadOnlyList<RuntimeVariantConflict> FindConflicts(LogDocument document)
    {
        var variantsByBaseName = new Dictionary<string, RuntimeVariantState>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in document.Lines)
        {
            if (!TryExtractLoadedAssemblyFileName(line.Text, out var fileName))
            {
                continue;
            }

            if (!TryParseVariant(fileName, out var baseName, out var variant))
            {
                continue;
            }

            if (!variantsByBaseName.TryGetValue(baseName, out var state))
            {
                state = new RuntimeVariantState(baseName);
                variantsByBaseName[baseName] = state;
            }

            state.Register(variant, fileName, line.Number);
        }

        return variantsByBaseName.Values
            .Where(state => state.HasMono && state.HasIl2Cpp)
            .Select(state => state.ToConflict())
            .ToArray();
    }

    private static bool TryParseVariant(string fileName, out string baseName, out RuntimeVariant variant)
    {
        var extensionlessName = Path.GetFileNameWithoutExtension(fileName).Trim();

        if (extensionlessName.EndsWith("-IL2Cpp", StringComparison.OrdinalIgnoreCase))
        {
            baseName = extensionlessName[..^7];
            variant = RuntimeVariant.Il2Cpp;
            return true;
        }

        if (extensionlessName.EndsWith(".Il2Cpp", StringComparison.OrdinalIgnoreCase))
        {
            baseName = extensionlessName[..^7];
            variant = RuntimeVariant.Il2Cpp;
            return true;
        }

        if (extensionlessName.EndsWith("IL2CPP", StringComparison.OrdinalIgnoreCase) && extensionlessName.Length > "IL2CPP".Length)
        {
            baseName = extensionlessName[..^7].TrimEnd('-', '.', '_', ' ');
            variant = RuntimeVariant.Il2Cpp;
            return true;
        }

        baseName = extensionlessName;
        variant = RuntimeVariant.Mono;
        return true;
    }

    private static bool TryExtractLoadedAssemblyFileName(string text, out string fileName)
    {
        const string marker = "Melon Assembly loaded:";
        fileName = string.Empty;

        var markerIndex = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var firstQuoteIndex = text.IndexOf('\'', markerIndex + marker.Length);
        if (firstQuoteIndex < 0)
        {
            return false;
        }

        var secondQuoteIndex = text.IndexOf('\'', firstQuoteIndex + 1);
        if (secondQuoteIndex < 0)
        {
            return false;
        }

        var rawPath = text[(firstQuoteIndex + 1)..secondQuoteIndex].Trim();
        var lastSeparatorIndex = rawPath.LastIndexOfAny(new[] { '\\', '/' });
        fileName = lastSeparatorIndex >= 0 ? rawPath[(lastSeparatorIndex + 1)..] : rawPath;
        return fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
    }

    internal sealed class RuntimeVariantConflict
    {
        public RuntimeVariantConflict(string baseName, string monoFileName, string il2CppFileName, int firstLineNumber)
        {
            BaseName = baseName;
            MonoFileName = monoFileName;
            Il2CppFileName = il2CppFileName;
            FirstLineNumber = firstLineNumber;
        }

        public string BaseName { get; }

        public string MonoFileName { get; }

        public string Il2CppFileName { get; }

        public int FirstLineNumber { get; }
    }

    private sealed class RuntimeVariantState
    {
        public RuntimeVariantState(string baseName)
        {
            BaseName = baseName;
        }

        public string BaseName { get; }

        public bool HasMono => !string.IsNullOrWhiteSpace(MonoFileName);

        public bool HasIl2Cpp => !string.IsNullOrWhiteSpace(Il2CppFileName);

        public string? MonoFileName { get; private set; }

        public string? Il2CppFileName { get; private set; }

        public int FirstLineNumber { get; private set; } = int.MaxValue;

        public void Register(RuntimeVariant variant, string fileName, int lineNumber)
        {
            FirstLineNumber = Math.Min(FirstLineNumber, lineNumber);
            if (variant == RuntimeVariant.Il2Cpp)
            {
                Il2CppFileName ??= fileName;
                return;
            }

            MonoFileName ??= fileName;
        }

        public RuntimeVariantConflict ToConflict()
        {
            return new RuntimeVariantConflict(
                BaseName,
                MonoFileName ?? string.Empty,
                Il2CppFileName ?? string.Empty,
                FirstLineNumber);
        }
    }

    private enum RuntimeVariant
    {
        Mono,
        Il2Cpp,
    }
}
