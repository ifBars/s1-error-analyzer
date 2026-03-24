using System.Text.RegularExpressions;
using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Presentation;
using ErrorAnalyzer.Core.Parsing;

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

                var modName = document.ResolveModName(headerMatch.Groups["mod"].Value);
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
        var resolvedModName = document.ResolveModName(modName) ?? document.FindNearestModName(lineIndex);
        if (TryCreateRuntimeMismatchDiagnosis(document, resolvedModName, assemblyName, lineNumber, out var runtimeMismatchDiagnosis))
        {
            return runtimeMismatchDiagnosis;
        }

        return new Diagnosis(
            RuleIds.MissingDependency,
            "A required support file is missing",
            $"This mod could not load one of the files it needs: `{assemblyName}`.",
            GetSuggestedAction(assemblyName),
            resolvedModName,
            $"Missing dependency `{assemblyName}`.",
            lineNumber,
            DiagnosisSeverity.Error,
            DiagnosisConfidence.High,
            GetAdvice(assemblyName));
    }

    private static bool TryCreateRuntimeMismatchDiagnosis(
        LogDocument document,
        string? modName,
        string assemblyName,
        int lineNumber,
        out Diagnosis diagnosis)
    {
        if (document.Runtime == RuntimeKind.Il2Cpp &&
            assemblyName.StartsWith("FishNet.Runtime", StringComparison.OrdinalIgnoreCase))
        {
            diagnosis = new Diagnosis(
                RuleIds.RuntimeMismatchMonoModOnIl2Cpp,
                "Wrong version of the mod is installed",
                "This mod is trying to load the Mono FishNet runtime on an Il2Cpp game install.",
                "Install the Il2Cpp build of this mod. If it has a separate FishNet dependency, use `Il2CppFishNet.Runtime` instead of `FishNet.Runtime`.",
                modName,
                $"Missing dependency `{assemblyName}` on an Il2Cpp game install.",
                lineNumber,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High,
                DiagnosisAdviceFactory.WrongRuntimeBuild());
            return true;
        }

        diagnosis = null!;
        return false;
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
