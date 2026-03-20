using System.Text.RegularExpressions;

namespace ErrorAnalyzer.Core;

public static class RuleIds
{
    public const string DualRuntimeInstall = "dual_runtime_install";
    public const string MissingPatchTarget = "missing_patch_target";
    public const string MissingMethod = "missing_method";
    public const string MissingDependency = "missing_dependency";
    public const string RuntimeMismatchMonoModOnIl2Cpp = "runtime_mismatch_mono_mod_on_il2cpp";
    public const string OutdatedTypeReference = "outdated_type_reference";
    public const string FieldAccessorPatchBreak = "field_accessor_patch_break";
}

internal sealed class DualRuntimeInstallRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        foreach (var conflict in RuntimeVariantDetector.FindConflicts(document))
        {
            var suggestedAction = document.Runtime switch
            {
                RuntimeKind.Il2Cpp => $"Remove `{conflict.MonoFileName}` and keep `{conflict.Il2CppFileName}`.",
                RuntimeKind.Mono => $"Remove `{conflict.Il2CppFileName}` and keep `{conflict.MonoFileName}`.",
                _ => $"Keep only one version of this mod installed: `{conflict.MonoFileName}` or `{conflict.Il2CppFileName}`.",
            };

            yield return new Diagnosis(
                RuleIds.DualRuntimeInstall,
                "Both versions of this mod are installed",
                "You have both the normal and Il2Cpp versions of the same mod installed at the same time.",
                suggestedAction,
                conflict.BaseName,
                $"Loaded both `{conflict.MonoFileName}` and `{conflict.Il2CppFileName}`.",
                conflict.FirstLineNumber,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High);
        }
    }
}

internal interface IDetectionRule
{
    IEnumerable<Diagnosis> Analyze(LogDocument document);
}

internal sealed class MissingPatchTargetRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        for (var index = 0; index < document.Lines.Count; index++)
        {
            var line = document.Lines[index];
            if (!line.Text.Contains("HarmonyLib.HarmonyException: Patching exception in method null", StringComparison.Ordinal))
            {
                continue;
            }

            var hasPatchTargetEvidence = document.ContainsNearby(index, 3, text =>
                text.Contains("Undefined target method", StringComparison.Ordinal) ||
                text.Contains("Could not find method", StringComparison.Ordinal));

            if (!hasPatchTargetEvidence)
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.MissingPatchTarget,
                "This mod is outdated",
                "This mod is trying to hook into game code that changed after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(index),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High);
        }
    }
}

internal sealed class MissingMethodRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("System.MissingMethodException: Method not found", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.MissingMethod,
                "This mod is outdated",
                "This mod is looking for game code that is no longer there after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(line.Number - 1),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High);
        }
    }
}

internal sealed class MissingDependencyRule : IDetectionRule
{
    private static readonly Regex AssemblyRegex = new("'(?<assembly>[^']+)'", RegexOptions.Compiled);

    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("System.IO.FileNotFoundException: Could not load file or assembly", StringComparison.Ordinal))
            {
                continue;
            }

            var match = AssemblyRegex.Match(line.Text);
            var assemblyName = match.Success ? match.Groups["assembly"].Value : "a required assembly";
            yield return new Diagnosis(
                RuleIds.MissingDependency,
                "A required support file is missing",
                $"This mod could not load one of the files it needs: `{assemblyName}`.",
                "Reinstall the mod and its required dependencies, or remove it if you are unsure which extra file it needs.",
                document.FindNearestModName(line.Number - 1),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High);
        }
    }
}

internal sealed class RuntimeMismatchMonoModOnIl2CppRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        if (document.Runtime != RuntimeKind.Il2Cpp)
        {
            yield break;
        }

        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("Could not load type 'ScheduleOne.", StringComparison.Ordinal) ||
                !line.Text.Contains("from assembly 'Assembly-CSharp", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.RuntimeMismatchMonoModOnIl2Cpp,
                "Wrong version of the mod is installed",
                "This looks like the wrong build of the mod for the current version of the game.",
                "Look for a version of this mod labeled `Il2Cpp`. If you cannot find one, remove this mod.",
                document.FindNearestModName(line.Number - 1),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High);
        }
    }
}

internal sealed class OutdatedTypeReferenceRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        foreach (var line in document.Lines)
        {
            var text = line.Text;
            var isTypeLoad = text.Contains("System.TypeLoadException: Could not load type '", StringComparison.Ordinal) ||
                             text.StartsWith("Could not load type '", StringComparison.Ordinal);
            if (!isTypeLoad)
            {
                continue;
            }

            if (document.Runtime == RuntimeKind.Il2Cpp &&
                text.Contains("Could not load type 'ScheduleOne.", StringComparison.Ordinal) &&
                text.Contains("from assembly 'Assembly-CSharp", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.OutdatedTypeReference,
                "This mod is outdated",
                "This mod is trying to use game code that changed after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(line.Number - 1),
                text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High);
        }
    }
}

internal sealed class FieldAccessorPatchBreakRule : IDetectionRule
{
    public IEnumerable<Diagnosis> Analyze(LogDocument document)
    {
        if (document.Runtime != RuntimeKind.Il2Cpp)
        {
            yield break;
        }

        foreach (var line in document.Lines)
        {
            if (!line.Text.Contains("Failed to init IL2CPP patch backend", StringComparison.Ordinal) ||
                !line.Text.Contains("field accessor, it can't be patched", StringComparison.Ordinal))
            {
                continue;
            }

            yield return new Diagnosis(
                RuleIds.FieldAccessorPatchBreak,
                "This mod is outdated",
                "This mod is patching game code that changed shape after an update.",
                "Update this mod if there is a newer version. If not, remove it for now.",
                document.FindNearestModName(line.Number - 1, allowForwardSearch: false),
                line.Text.Trim(),
                line.Number,
                DiagnosisSeverity.Error,
                DiagnosisConfidence.High);
        }
    }
}
