using System.Text.RegularExpressions;
using ErrorAnalyzer.Core.Presentation;

namespace ErrorAnalyzer.Core.Rules;

internal sealed class MissingDependencyRule : IDetectionRule
{
    private static readonly Regex AssemblyRegex = new("'(?<assembly>[^']+)'", RegexOptions.Compiled);
    private static readonly Regex MissingDependencyHeaderRegex = new(
        @"-\s+'(?<mod>[^']+)'\s+is missing the following dependencies:",
        RegexOptions.Compiled);
    private static readonly Regex MissingDependencyItemRegex = new(
        @"-\s+'(?<assembly>[^']+)'\s+v(?<version>\S+)",
        RegexOptions.Compiled);

    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        for (var index = 0; index < document.Lines.Count; index++)
        {
            var line = document.Lines[index];
            if (!line.Text.Contains("System.IO.FileNotFoundException: Could not load file or assembly", StringComparison.Ordinal))
            {
                var headerMatch = MissingDependencyHeaderRegex.Match(line.Text);
                if (!headerMatch.Success)
                {
                    continue;
                }

                var modName = headerMatch.Groups["mod"].Value;
                for (var dependencyIndex = index + 1; dependencyIndex < document.Lines.Count; dependencyIndex++)
                {
                    var dependencyLine = document.Lines[dependencyIndex];
                    var dependencyMatch = MissingDependencyItemRegex.Match(dependencyLine.Text);
                    if (!dependencyMatch.Success)
                    {
                        break;
                    }

                    var dependencyAssemblyName = dependencyMatch.Groups["assembly"].Value;
                    yield return CreateDiagnosis(
                        document,
                        dependencyLine.Number - 1,
                        modName,
                        dependencyAssemblyName,
                        dependencyLine.Number);
                }

                continue;
            }

            var match = AssemblyRegex.Match(line.Text);
            var assemblyName = match.Success ? match.Groups["assembly"].Value : "a required assembly";
            yield return CreateDiagnosis(
                document,
                line.Number - 1,
                document.FindNearestModName(line.Number - 1),
                assemblyName,
                line.Number);
        }
    }

    private static Diagnosis CreateDiagnosis(LogDocument document, int lineIndex, string? modName, string assemblyName, int lineNumber)
    {
        return new Diagnosis(
            RuleIds.MissingDependency,
            "A required support file is missing",
            $"This mod could not load one of the files it needs: `{assemblyName}`.",
            GetSuggestedAction(assemblyName),
            modName ?? document.FindNearestModName(lineIndex),
            $"Missing dependency `{assemblyName}`.",
            lineNumber,
            DiagnosisSeverity.Error,
            DiagnosisConfidence.High,
            GetAdvice(assemblyName));
    }

    private static string GetSuggestedAction(string assemblyName)
    {
        if (assemblyName.Contains("SteamNetworkLib", StringComparison.OrdinalIgnoreCase))
        {
            return "Install or update `SteamNetworkLib`, then try again. If this mod still fails after that, remove or update the mod that depends on it.";
        }

        if (assemblyName.Contains("UnhollowerBaseLib", StringComparison.OrdinalIgnoreCase))
        {
            return "Install a newer `UnityExplorer` build that matches current Il2CppInterop-based MelonLoader. Older Unhollower-based builds will not work here.";
        }

        return "Reinstall the mod and its required dependencies, or remove it if you are unsure which extra file it needs.";
    }

    private static DiagnosisAdvice GetAdvice(string assemblyName)
    {
        if (assemblyName.Contains("SteamNetworkLib", StringComparison.OrdinalIgnoreCase))
        {
            return new DiagnosisAdvice(
                "missing_dependency:steamnetworklib",
                3,
                "Needs reinstall",
                "A mod is missing SteamNetworkLib",
                "Install or update SteamNetworkLib, then try again.",
                "This mod cannot start because SteamNetworkLib is missing.");
        }

        if (assemblyName.Contains("UnhollowerBaseLib", StringComparison.OrdinalIgnoreCase))
        {
            return new DiagnosisAdvice(
                "missing_dependency:unhollowerbaselib",
                3,
                "Wrong build",
                "UnityExplorer is using an old dependency",
                "Install a newer UnityExplorer build that matches current Il2CppInterop-based MelonLoader.",
                "This copy still expects UnhollowerBaseLib, which belongs to older loader setups.");
        }

        return new DiagnosisAdvice(
            "missing_dependency:generic",
            3,
            "Needs reinstall",
            "A mod is missing a required file",
            "Reinstall this mod and any required support mods. If you are unsure which file is missing, remove the mod.",
            "The mod cannot start because one of its required files is not present.");
    }
}
