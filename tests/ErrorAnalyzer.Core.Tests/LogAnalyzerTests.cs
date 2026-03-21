using ErrorAnalyzer.Core;
using Xunit;

namespace ErrorAnalyzer.Core.Tests;

public sealed class LogAnalyzerTests
{
    private readonly LogAnalyzer _analyzer = new();

    [Theory]
    [InlineData("Latest (22).log", RuntimeKind.Il2Cpp)]
    [InlineData("Latest (17).log", RuntimeKind.Mono)]
    public void DetectsRuntimeFromSupportModule(string fileName, RuntimeKind expectedRuntime)
    {
        var result = Analyze(fileName);
        Assert.Equal(expectedRuntime, result.Runtime);
    }

    [Fact]
    public void DetectsMissingPatchTargetFromS1ApiLog()
    {
        var result = Analyze("25-7-31_23-28-0.log");

        var diagnosis = Assert.Single(result.Diagnoses, x => x.RuleId == RuleIds.MissingPatchTarget);
        Assert.Equal("S1API", diagnosis.ModName);
        Assert.Contains("HarmonyLib.HarmonyException", diagnosis.Evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void DetectsMonoRuntimeMismatchAndMissingDependenciesInIl2CppLog()
    {
        var result = Analyze("Latest (22).log");

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.DualRuntimeInstall &&
            string.Equals(x.ModName, "CartelEnforcer", StringComparison.Ordinal) &&
            x.Evidence.Contains("CartelEnforcer.dll", StringComparison.Ordinal) &&
            x.Evidence.Contains("CartelEnforcer-IL2Cpp.dll", StringComparison.Ordinal) &&
            x.SuggestedAction.Contains("Remove `CartelEnforcer.dll`", StringComparison.Ordinal) &&
            x.SuggestedAction.Contains("keep `CartelEnforcer-IL2Cpp.dll`", StringComparison.Ordinal));

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.RuntimeMismatchMonoModOnIl2Cpp &&
            string.Equals(x.ModName, "RainsCarMod", StringComparison.Ordinal));

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingDependency &&
            x.Evidence.Contains("SteamNetworkLib", StringComparison.Ordinal));

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingMethod &&
            x.Evidence.Contains("ItemDefinition.get_LabelDisplayColor", StringComparison.Ordinal));
    }

    [Fact]
    public void DetectsModInstalledInPluginsFolder()
    {
        var result = Analyze("25-8-16_20-40-8.log");

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.ModInWrongFolder &&
            string.Equals(x.ModName, "S1API", StringComparison.Ordinal) &&
            x.Evidence.Contains("cannot be loaded as a Plugin", StringComparison.Ordinal));
    }

    [Fact]
    public void DetectsDeclaredMissingDependenciesFromStartupWarnings()
    {
        var result = Analyze("26-3-14_15-46-46.log");

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingDependency &&
            string.Equals(x.ModName, "ChangeMixerThrehold", StringComparison.Ordinal) &&
            x.Evidence.Contains("ModManager&PhoneApp", StringComparison.Ordinal) &&
            x.SuggestedAction.Contains("Reinstall the mod and its required dependencies", StringComparison.Ordinal));
    }

    [Fact]
    public void DetectsDualRuntimeInstallFromLoadedAssemblies()
    {
        var lines = File.ReadAllLines(Path.Combine(FindErrorLogsDirectory(), "Latest (22).log"));
        Assert.Contains(lines, line => line.Contains("CartelEnforcer-IL2Cpp.dll", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("CartelEnforcer.dll", StringComparison.Ordinal));

        var logText = string.Join(Environment.NewLine, lines);
        var document = new LogDocument("Latest (22).log", logText);
        var conflicts = RuntimeVariantDetector.FindConflicts(document);

        Assert.Contains(conflicts, x =>
            string.Equals(x.BaseName, "CartelEnforcer", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.MonoFileName, "CartelEnforcer.dll", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Il2CppFileName, "CartelEnforcer-IL2Cpp.dll", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AttributesReflectionLoaderFailuresToOwningModsInLatest22()
    {
        var result = Analyze("Latest (22).log");

        Assert.Contains(result.Diagnoses, x =>
            (x.RuleId == RuleIds.OutdatedTypeReference ||
             x.RuleId == RuleIds.RuntimeMismatchMonoModOnIl2Cpp) &&
            string.Equals(x.ModName, "CartelEnforcer", StringComparison.Ordinal) &&
            x.Evidence.Contains("ScheduleOne.Quests.Quest", StringComparison.Ordinal));

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingDependency &&
            (string.Equals(x.ModName, "AutoRestockIL2CPP", StringComparison.Ordinal) ||
             string.Equals(x.ModName, "AutoRestock", StringComparison.Ordinal)) &&
            x.Evidence.Contains("SteamNetworkLib", StringComparison.Ordinal));

        Assert.DoesNotContain(result.Diagnoses, x =>
            string.Equals(x.ModName, "Mod_Manager_&_Phone_App", StringComparison.Ordinal));
    }

    [Fact]
    public void PrefersStackOwnerOverNearbyUnrelatedModLogs()
    {
        var result = Analyze("Latest (22).log");

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingMethod &&
            string.Equals(x.ModName, "ContractCompassMarkersPatch", StringComparison.Ordinal) &&
            x.Evidence.Contains("Quest.get_QuestState", StringComparison.Ordinal));

        Assert.DoesNotContain(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingMethod &&
            x.Evidence.Contains("Quest.get_QuestState", StringComparison.Ordinal) &&
            (string.Equals(x.ModName, "Property Price Manager", StringComparison.Ordinal) ||
             string.Equals(x.ModName, "RainsCarMod", StringComparison.Ordinal) ||
             string.Equals(x.ModName, "Unicorns Custom Seeds", StringComparison.Ordinal)));
    }

    [Fact]
    public void DetectsRuntimeMismatchFromTrashBagLog()
    {
        var result = Analyze("26-1-10_21-45-34.log");

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.RuntimeMismatchMonoModOnIl2Cpp &&
            x.Evidence.Contains("TrashBag_Equippable", StringComparison.Ordinal));

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.OutdatedTypeReference &&
            x.Evidence.Contains("NPCSignal_HandleDeal", StringComparison.Ordinal));
    }

    [Fact]
    public void DetectsFieldAccessorPatchBreaksFromRealLogs()
    {
        var result = Analyze("Latest (22).log");

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.FieldAccessorPatchBreak &&
            x.Evidence.Contains("Property::get_Price", StringComparison.Ordinal));

        var minimapResult = Analyze("Latest (16).log");
        Assert.Contains(minimapResult.Diagnoses, x =>
            x.RuleId == RuleIds.FieldAccessorPatchBreak &&
            x.Evidence.Contains("GameInput::get_MouseWheelAxis", StringComparison.Ordinal));
    }

    [Fact]
    public void DoesNotUseGenericClassNamesAsModNames()
    {
        var result = Analyze("new.log");

        Assert.Contains(result.Diagnoses, x =>
            x.RuleId == RuleIds.MissingMethod &&
            string.Equals(x.ModName, "Low_End", StringComparison.Ordinal) &&
            x.Evidence.Contains("Resources.FindObjectsOfTypeAll", StringComparison.Ordinal));

        Assert.DoesNotContain(result.Diagnoses, x =>
            string.Equals(x.ModName, "Class1", StringComparison.Ordinal));
    }

    private LogAnalysisResult Analyze(string fileName)
    {
        var logDirectory = FindErrorLogsDirectory();
        var path = Path.Combine(logDirectory, fileName);
        return _analyzer.AnalyzeFile(path);
    }

    private static string FindErrorLogsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "ErrorLogs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the ErrorLogs directory for analyzer tests.");
    }
}
