using ErrorAnalyzer.Core.Presentation;

namespace ErrorAnalyzer.Core.Rules;

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
                DiagnosisConfidence.High,
                new DiagnosisAdvice(
                    RuleIds.DualRuntimeInstall,
                    0,
                    "Most likely fix",
                    "You installed both versions of the same mod",
                    suggestedAction.Replace("`", string.Empty, StringComparison.Ordinal).Trim(),
                    "Keep only the version that matches your current game type."));
        }
    }
}
