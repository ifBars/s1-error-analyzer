using ErrorAnalyzer.Core;
using ErrorAnalyzer.Core.Analysis;
using ErrorAnalyzer.Core.Models;
using ErrorAnalyzer.Core.Presentation;
using ErrorAnalyzer.Core.Rules;
using Xunit;

namespace ErrorAnalyzer.Core.Tests;

public sealed class DiagnosisAdviceTests
{
    private readonly LogAnalyzer _analyzer = new();

    [Fact]
    public void MissingDependencyRuleProvidesSpecificDependencyGuidance()
    {
        var result = Analyze("Latest (22).log");
        var diagnosis = result.Diagnoses.First(x =>
            x.RuleId == RuleIds.MissingDependency &&
            x.Evidence.Contains("SteamNetworkLib", StringComparison.Ordinal));
        var advice = diagnosis.Advice;

        Assert.Equal("missing_dependency:steamnetworklib", advice.GroupKey);
        Assert.Equal(3, advice.Priority);
        Assert.Equal("A mod is missing SteamNetworkLib", advice.Title);
        Assert.Contains("SteamNetworkLib", advice.PrimaryAction, StringComparison.Ordinal);
    }

    [Fact]
    public void FishNetMonoDependencyOnIl2CppUsesRuntimeMismatchAdvice()
    {
        var result = Analyze("Latest (22).log");
        var diagnosis = result.Diagnoses.First(x =>
            x.RuleId == RuleIds.RuntimeMismatchMonoModOnIl2Cpp &&
            string.Equals(x.ModName, "RainsCarMod", StringComparison.Ordinal) &&
            x.Evidence.Contains("FishNet.Runtime", StringComparison.Ordinal));
        var advice = diagnosis.Advice;

        Assert.Equal(RuleIds.RuntimeMismatchMonoModOnIl2Cpp, advice.GroupKey);
        Assert.Equal(2, advice.Priority);
        Assert.Equal("You have the wrong version of a mod installed", advice.Title);
        Assert.Contains("Il2Cpp", diagnosis.SuggestedAction, StringComparison.Ordinal);
        Assert.Contains("Il2CppFishNet.Runtime", diagnosis.SuggestedAction, StringComparison.Ordinal);
    }

    [Fact]
    public void OutdatedRuleProvidesSharedAdviceGroup()
    {
        var result = Analyze("Latest (22).log");
        var diagnosis = Assert.Single(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingMethod &&
            x.Evidence.Contains("ItemDefinition.get_LabelDisplayColor", StringComparison.Ordinal));
        var advice = diagnosis.Advice;

        Assert.Equal("outdated_mods", advice.GroupKey);
        Assert.Equal(4, advice.Priority);
        Assert.Equal("One or more mods are outdated after a game update", advice.Title);
    }

    [Fact]
    public void MapsDiagnosisDtosWithSharedAdvicePayload()
    {
        var diagnosis = new Diagnosis(
            RuleIds.ModInWrongFolder,
            "This mod is in the wrong folder",
            "This file was installed into the `Plugins` folder even though MelonLoader identifies it as a mod.",
            "Move this file from `Plugins` into the `Mods` folder, then launch the game again.",
            "S1API",
            "Failed to load Melon 'S1API' from '.\\Plugins\\S1API.dll': The given Melon is a Mod and cannot be loaded as a Plugin.",
            10,
            DiagnosisSeverity.Error,
            DiagnosisConfidence.High,
            new DiagnosisAdvice(
                RuleIds.ModInWrongFolder,
                1,
                "Quick fix",
                "A mod was installed into the wrong folder",
                "Move this file from Plugins into Mods, then try again.",
                "MelonLoader recognized this file as a mod, not a plugin."));

        var dto = LogAnalysisResultMapper.ToDto(new LogAnalysisResult(RuntimeKind.Il2Cpp, new[] { diagnosis }));
        var dtoDiagnosis = Assert.Single(dto.Diagnoses);

        Assert.Equal("mod_in_wrong_folder", dtoDiagnosis.Advice.GroupKey);
        Assert.Equal(1, dtoDiagnosis.Advice.Priority);
        Assert.Equal("A mod was installed into the wrong folder", dtoDiagnosis.Advice.Title);
    }

    [Fact]
    public void ResultDtoIncludesGroupedAdviceSummaries()
    {
        var result = Analyze("new.log");

        var dto = LogAnalysisResultMapper.ToDto(result);
        var outdatedGroup = Assert.Single(dto.AdviceGroups, group => group.GroupKey == "outdated_mods");

        Assert.Equal("One or more mods are outdated after a game update", outdatedGroup.Title);
        Assert.Contains("AdvancedDealing", outdatedGroup.AffectedMods, StringComparer.Ordinal);
        Assert.Contains("ContractCompassMarkersPatch", outdatedGroup.AffectedMods, StringComparer.Ordinal);
        Assert.Contains("Low End", outdatedGroup.AffectedMods, StringComparer.Ordinal);
        Assert.True(outdatedGroup.AffectedMods.Count >= 4);
        Assert.True(outdatedGroup.TotalOccurrences >= outdatedGroup.DiagnosisCount);
    }

    [Fact]
    public void ResultDtoSkipsUnknownModsInAdviceGroups()
    {
        var diagnosis = new Diagnosis(
            RuleIds.MissingDependency,
            "A required support file is missing",
            "This mod could not load one of the files it needs: `Some.Dependency`.",
            "Reinstall the mod and its required dependencies.",
            null,
            "Missing dependency `Some.Dependency`.",
            42,
            DiagnosisSeverity.Error,
            DiagnosisConfidence.High,
            DiagnosisAdviceFactory.Generic(
                "missing_dependency:generic",
                "A mod is missing a required file",
                "The mod cannot start because one of its required files is not present.",
                "Reinstall this mod and any required support mods."));

        var dto = LogAnalysisResultMapper.ToDto(new LogAnalysisResult(RuntimeKind.Unknown, new[] { diagnosis }));
        var group = Assert.Single(dto.AdviceGroups);

        Assert.Empty(group.AffectedMods);
    }

    private LogAnalysisResult Analyze(string fileName)
    {
        var path = Path.Combine(FindErrorLogsDirectory(), fileName);
        return _analyzer.AnalyzeFile(path);
    }

    private static string FindErrorLogsDirectory()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, "..", "..", "..", "..", "ErrorLogs"));
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new DirectoryNotFoundException("Could not find ErrorLogs directory.");
    }
}
